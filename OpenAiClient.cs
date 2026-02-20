using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatGptVsix
{
    internal static class OpenAiClient
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };

        public static async Task<string> SendPromptAsync(string prompt, string model, int maxOutputTokens)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Missing OPENAI_API_KEY environment variable.");

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = model,
                input = new object[]
                {
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = prompt }
                        }
                    }
                },
                max_output_tokens = maxOutputTokens
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI error ({(int)resp.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);

            // "output_text" exists in Responses API examples.
            if (doc.RootElement.TryGetProperty("output_text", out var outText))
                return outText.GetString() ?? "";

            // Fallback: show raw JSON if shape differs.
            return json;
        }
    }
}
