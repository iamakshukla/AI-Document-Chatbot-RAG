
using System.Net.Http.Json;
using System.Text.Json;

namespace QMS.Chatbot.Api.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;

        public EmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<float>> GenerateEmbeddingAsync(
            string text)
        {
            var request = new
            {
                model = "nomic-embed-text",
                input = text
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/embed", request);

            response.EnsureSuccessStatusCode();

            using var json = await response.Content
                .ReadFromJsonAsync<JsonDocument>();

            var embedding = json!.RootElement
                .GetProperty("embeddings")[0];

            return embedding.EnumerateArray()
                .Select(x => x.GetSingle())
                .ToList();
        }
    }
}
