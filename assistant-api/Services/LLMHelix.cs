using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace assistant_api.Services
{
    public class LLMHelix
    {
        private readonly string _systemInstruction;
        private readonly HttpClient _httpClient;
        private readonly string _model;
        public string SystemInstruction => _systemInstruction;

        public LLMHelix(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("openai");
            _systemInstruction = File.ReadAllText("src/assistant/helix_system_instruction.txt");
            _model = config["OpenAI:HelixModel"] ?? "gpt-4o-mini";
        }

        public async Task<string> CallHelixAsync(List<Dictionary<string, string>> messages)
        {
            var payload = new { model = _model, messages = messages };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
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

    }
}
