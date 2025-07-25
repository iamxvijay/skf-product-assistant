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
        private readonly string _endpoint;

        public string SystemInstruction => _systemInstruction;

        public LLMHelix(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("openai");

            var instructionFilePath = config["OpenAI:HelixSystemInstructionPath"] ?? "src/assistant/helix_system_instruction.txt";
            _systemInstruction = File.ReadAllText(instructionFilePath);

            _model = config["OpenAI:HelixModel"] ?? "gpt-4o-mini";

            var endpointTemplate = config["OpenAI:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpointTemplate))
                endpointTemplate = "https://skf-openai-dev-eval.openai.azure.com/openai/deployments/{MODEL}/chat/completions?api-version=2024-08-01-preview";
            _endpoint = endpointTemplate.Replace("{MODEL}", _model);
        }

        public async Task<string> CallHelixAsync(List<Dictionary<string, string>> messages)
        {
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
    }
}
