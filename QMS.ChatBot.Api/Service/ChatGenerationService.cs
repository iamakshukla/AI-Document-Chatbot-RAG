
using System.Net.Http.Json;
using System.Text.Json;

namespace QMS.Chatbot.Api.Services
{
    public class ChatGenerationService
    {
        private readonly HttpClient _httpClient;

        public ChatGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateAnswerAsync(
            string question,
            string context)
        {
            string prompt = $"""
                You are a QMS insurance chatbot.

                Answer the user's question using ONLY
                the document context provided below.

                Keep answers clear, accurate and concise.

                If the answer is not available in the context,
                respond with:
                "Information is not available in the document."

                Document Context:
                {context}

                User Question:
                {question}

                Answer:
                """;

            var request = new
            {
                model = "llama3.2:1b",
                prompt = prompt,
                stream = false,
                keep_alive = "10m",
                options = new
                {
                    temperature = 0,
                    num_predict = 150
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"Ollama error {(int)response.StatusCode}: {error}");
            }

            using var json =
                await response.Content
                    .ReadFromJsonAsync<JsonDocument>();

            return json!.RootElement
                .GetProperty("response")
                .GetString() ?? "";
        }
    }
}
