using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;

namespace assistant_api.Services
{
    public class LLMSpectro
    {
        private readonly string _systemInstruction;
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly string _endpoint;

        public LLMSpectro(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("openai");

            var instructionFilePath = config["OpenAI:SpectroSystemInstructionPath"] ?? "src/assistant/spectro_system_instruction.txt";
            _systemInstruction = File.ReadAllText(instructionFilePath);

            _model = config["OpenAI:SpectroModel"] ?? "gpt-4o-mini";

            var endpointTemplate = config["OpenAI:Endpoint"];
            
            _endpoint = endpointTemplate.Replace("{MODEL}", _model);
        }

        public async Task<string> CallSpectroAsync(string userQuestion, string jsonPathQuery, string linkResult)
        {
            var messages = new[]
            {
                new { role = "system", content = _systemInstruction },
                new { role = "user", content = $"User question: {userQuestion}\nJSONPath query: {jsonPathQuery}\nQuery result: {linkResult}" }
            };

            var payload = new { messages = messages };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return reply;
        }

        // Streaming version
        public async IAsyncEnumerable<string> StreamSpectroAsync(string userQuestion, string jsonPathQuery, string linkResult)
        {
            var messages = new[]
            {
                new { role = "system", content = _systemInstruction },
                new { role = "user", content = $"User question: {userQuestion}\nJSONPath query: {jsonPathQuery}\nQuery result: {linkResult}" }
            };

            var payload = new
            {
                messages = messages,
                stream = true
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = requestContent
            };
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None
            );

            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data:")) continue;

                var jsonStr = line.Substring(5).Trim();
                if (jsonStr == "[DONE]") break;

                string? content = null;
                try
                {
                    using var doc = JsonDocument.Parse(jsonStr);
                    var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var c))
                        content = c.GetString();
                }
                catch
                {
                    // Ignore parse errors
                }
                if (!string.IsNullOrEmpty(content))
                    yield return content;
            }
        }
    }
}
