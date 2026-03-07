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
    /// <summary>
    /// Handles any OpenAI-compatible endpoint.
    /// GitHub Models uses a different path (inference/chat/completions)
    /// and requires publisher-prefixed model IDs (e.g. openai/gpt-4o).
    /// All other providers use v1/chat/completions.
    /// </summary>
    internal sealed class OpenAiClient : ILlmClient
    {
        private readonly HttpClient    _http;
        private readonly string        _model;
        private readonly string        _apiKey;
        private readonly LlmProvider   _provider;

        public OpenAiClient(HttpClient http, string model, string apiKey, string baseUrl, LlmProvider provider)
        {
            _http     = http     ?? throw new ArgumentNullException(nameof(http));
            _model    = model    ?? throw new ArgumentNullException(nameof(model));
            _apiKey   = apiKey   ?? throw new ArgumentNullException(nameof(apiKey));
            _provider = provider;

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("BaseUrl must be set.", nameof(baseUrl));

            _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
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
                max_tokens = 1000
            };

            // GitHub Models uses a different path than standard OpenAI
            var path = _provider == LlmProvider.GitHubModels
                ? "inference/chat/completions"
                : "v1/chat/completions";

            using var req = new HttpRequestMessage(HttpMethod.Post, path);

            if (!string.IsNullOrWhiteSpace(_apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            req.Content = new StringContent(
                JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[{_model}@{_http.BaseAddress}] HTTP {(int)resp.StatusCode}: {json}");

            var obj     = JObject.Parse(json);
            var content = obj["choices"]?[0]?["message"]?["content"]?.ToString();
            return string.IsNullOrWhiteSpace(content) ? json : content!;
        }
    }
}
