using ChatGptVsix.Services;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services
{
    internal sealed class OllamaClient : ILlmClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly string _baseUrl;
        private readonly string _model;

        public OllamaClient(string model, string baseUrl = "http://localhost:11434/")
        {
            _model   = model ?? throw new ArgumentNullException(nameof(model));
            _baseUrl = (baseUrl ?? "http://localhost:11434/").TrimEnd('/') + "/";
        }

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            var payload = new
            {
                model    = _model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                },
                stream = false
            };

            using var http = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(120) };
            using var resp = await http.PostAsJsonAsync("v1/chat/completions", payload, JsonOptions, ct)
                                       .ConfigureAwait(false);

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ollama error ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }
    }
}
