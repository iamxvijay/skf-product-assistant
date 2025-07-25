using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using assistant_api.Services;
using Newtonsoft.Json.Linq;

namespace assistant_api.Helpers
{
    public static class ChatApiResponseHelper
    {
        public static async Task SendJsonResponse(HttpResponse response, ChatHistoryService chatHistory, string sessionId, List<Dictionary<string, string>> chatHistoryList, string assistantMsg)
        {
            response.ContentType = "application/json";
            string responseText = TryUnwrapResponse(assistantMsg);

            // Save history as plain text (not double-JSON)
            if (!string.IsNullOrWhiteSpace(responseText) && chatHistoryList != null)
            {
                chatHistoryList.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", responseText } });
                if (chatHistory != null)
                {
                    try { await chatHistory.SaveHistoryAsync(sessionId, chatHistoryList); } catch { /* Log if needed */ }
                }
            }
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { response = responseText });
            await response.WriteAsync(json);
        }

        // Helper to unwrap {"response": "..."} if already JSON, else returns string as-is
        private static string TryUnwrapResponse(string maybeJson)
        {
            if (string.IsNullOrWhiteSpace(maybeJson)) return "";
            try
            {
                var token = JToken.Parse(maybeJson);
                if (token.Type == JTokenType.Object && token["response"] != null)
                {
                    return token["response"]?.ToString() ?? maybeJson;
                }
            }
            catch { /* Not a JSON, just text */ }
            return maybeJson;
        }
    }

    public static class ChatApiMessages
    {
        public const string GenericError = "Sorry, something went wrong. Please try again.";
    }
}
