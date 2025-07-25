using assistant_api.Models;
using assistant_api.Services;
using JObjectListSearch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace assistant_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly JsonDataLoader _dataLoader;
    private readonly LLMHelix _helix;
    private readonly LLMSpectro _spectro;
    private readonly ChatHistoryService _chatHistory;

    public ChatController(JsonDataLoader dataLoader, LLMHelix helix, LLMSpectro spectro, ChatHistoryService chatHistory)
    {
        _dataLoader = dataLoader;
        _helix = helix;
        _spectro = spectro;
        _chatHistory = chatHistory;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest req)
    {
        // 1. Generate or use sessionId
        string sessionId = string.IsNullOrWhiteSpace(req.SessionId) ? Guid.NewGuid().ToString() : req.SessionId;

        // 2. Load history, or create new with system instruction
        var chatHistory = await _chatHistory.GetHistoryAsync(sessionId);
        if (chatHistory.Count == 0)
        {
            // Add the system prompt as first message
            chatHistory.Add(new Dictionary<string, string> { { "role", "system" }, { "content", _helix.SystemInstruction } });
        }

        // 3. Add the new user message
        chatHistory.Add(new Dictionary<string, string> { { "role", "user" }, { "content", req.UserMessage } });

        // 4. Call Helix with full chat history
        var helixReply = await _helix.CallHelixAsync(chatHistory);
        Console.WriteLine("Helix raw reply: " + helixReply);

        if (string.IsNullOrWhiteSpace(helixReply))
        {
            // Save history and return
            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
            return Ok(new ChatResponse { SessionId = sessionId, Response = "Helix did not return a response." });
        }

        JObject helixJson;
        try { helixJson = ParseJsonObjectFromLLM(helixReply); }
        catch (Exception ex)
        {
            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
            return Ok(new ChatResponse { SessionId = sessionId, Response = $"Helix returned invalid JSON: {ex.Message}\nRaw: {helixReply}" });
        }

        // If just a direct response
        if (helixJson.ContainsKey("response"))
        {
            chatHistory.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", helixJson["response"]?.ToString() ?? "(empty response)" } });
            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
            return Ok(new ChatResponse { SessionId = sessionId, Response = helixJson["response"]?.ToString() ?? "(empty response)" });
        }

        // If a query is returned
        if (helixJson.ContainsKey("query"))
        {
            var allObjects = await _dataLoader.LoadDataAsync();
            var queryToken = helixJson["query"];
            if (queryToken.Type == JTokenType.Object)
            {
                Query? queryObj;
                try { queryObj = queryToken.ToObject<Query>(); }
                catch (Exception ex)
                {
                    await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                    return Ok(new ChatResponse { SessionId = sessionId, Response = $"Helix returned a query that couldn't be parsed: {ex.Message}\nRaw: {queryToken}" });
                }

                var letVars = JsonQueryEngine.EvaluateLets(queryObj, allObjects);
                var results = new List<string>();
                foreach (var obj in allObjects)
                {
                    if (JsonQueryEngine.EvaluateFilter(obj, queryObj.Filter, letVars))
                    {
                        foreach (var extract in queryObj.Extracts)
                        {
                            var val = JsonQueryEngine.ExtractValue(obj, extract, letVars);
                            results.Add(val?.ToString() ?? "(not found)");
                        }
                    }
                }

                if (results.Count == 0)
                {
                    await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                    return Ok(new ChatResponse { SessionId = sessionId, Response = "(No matching results found in data for this query)" });
                }

                var queryResultText = string.Join("\n", results);

                // Call Spectro LLM
                var spectroReply = await _spectro.CallSpectroAsync(req.UserMessage, queryToken.ToString(), queryResultText);
                Console.WriteLine("Spectro raw reply: " + spectroReply);

                if (!string.IsNullOrWhiteSpace(spectroReply))
                {
                    try
                    {
                        var spectroJson = ParseJsonObjectFromLLM(spectroReply);
                        if (spectroJson.ContainsKey("response"))
                        {
                            chatHistory.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", spectroJson["response"]?.ToString() ?? "(empty Spectro response)" } });
                            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                            return Ok(new ChatResponse { SessionId = sessionId, Response = spectroJson["response"]?.ToString() ?? "(empty Spectro response)" });
                        }
                        else
                        {
                            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                            return Ok(new ChatResponse { SessionId = sessionId, Response = $"Spectro replied, but no 'response' field found: {spectroReply}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                        return Ok(new ChatResponse { SessionId = sessionId, Response = $"Spectro returned invalid JSON: {ex.Message}\nRaw: {spectroReply}" });
                    }
                }
                await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
                return Ok(new ChatResponse { SessionId = sessionId, Response = "Spectro did not return any reply." });
            }
            await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
            return Ok(new ChatResponse { SessionId = sessionId, Response = $"Helix returned a query in an unsupported format: {queryToken}" });
        }

        await _chatHistory.SaveHistoryAsync(sessionId, chatHistory);
        return Ok(new ChatResponse { SessionId = sessionId, Response = $"Helix returned an unrecognized structure: {helixReply}" });
    }

    private static JObject ParseJsonObjectFromLLM(string llmReply)
    {
        int jsonStart = llmReply.IndexOf('{');
        if (jsonStart == -1)
            throw new Exception("No JSON object found in response.");
        string jsonPart = llmReply.Substring(jsonStart);
        return JObject.Parse(jsonPart);
    }
}
