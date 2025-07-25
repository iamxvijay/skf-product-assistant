using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace assistant_api.Services
{
    public class LLMSpectro
    {
        private readonly string _systemInstruction;
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public LLMSpectro(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("openai");
            _systemInstruction = File.ReadAllText("src/assistant/spectro_system_instruction.txt");
            _model = config["OpenAI:SpectroModel"] ?? "gpt-4o-mini";
        }

        public async Task<string> CallSpectroAsync(string userQuestion, string jsonPathQuery, string linkResult)
        {
            var messages = new[]
            {
                new { role = "system", content = _systemInstruction },
                new { role = "user", content = $"User question: {userQuestion}\nJSONPath query: {jsonPathQuery}\nQuery result: {linkResult}" }
            };

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
