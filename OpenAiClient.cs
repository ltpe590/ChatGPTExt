using ChatGptVsix.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix
{
    internal sealed class OpenAiClient : ILlmClient
    {
        private const string DefaultModel = "gpt-4o";

        private readonly HttpClient _http;
        private readonly string _model;
        private readonly string _apiKey;

        public OpenAiClient(HttpClient http, string model, string apiKey)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _http.BaseAddress ??= new Uri("https://api.openai.com/");

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            var payload = new
            {
                model = DefaultModel,
                input = new object[]
                {
                    new {
                        role = "system",
                        content = new object[] { new { type = "input_text", text = systemPrompt } }
                    },
                    new {
                        role = "user",
                        content = new object[] { new { type = "input_text", text = userPrompt } }
                    }
                },
                max_output_tokens = 800
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI error ({(int)resp.StatusCode}): {json}");

            // Prefer output_text when present
            var obj = JObject.Parse(json);

            var token = obj["output_text"];
            if (token != null)
            {
                var outputText = token.ToString();
                if (!string.IsNullOrWhiteSpace(outputText))
                    return outputText;
            }

            // Fallback: try to extract any text from output blocks (if output_text absent)
            // This keeps you resilient to shape differences.
            var output = obj["output"] as JArray;
            if (output != null)
            {
                var sb = new StringBuilder();
                foreach (var item in output)
                {
                    var content = item?["content"] as JArray;
                    if (content == null) continue;

                    foreach (var c in content)
                    {
                        var t = (string?)c?["text"];
                        if (!string.IsNullOrEmpty(t))
                            sb.Append(t);
                    }
                }

                var combined = sb.ToString();
                if (!string.IsNullOrWhiteSpace(combined))
                    return combined;
            }

            // Last resort: return raw json
            return json;
        }
    }
}