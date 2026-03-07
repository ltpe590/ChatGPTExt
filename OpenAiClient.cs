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
        private readonly string      _baseUrl;
        private readonly string      _model;
        private readonly string      _apiKey;
        private readonly LlmProvider _provider;

        public OpenAiClient(string model, string apiKey, string baseUrl, LlmProvider provider)
        {
            _model    = model    ?? throw new ArgumentNullException(nameof(model));
            _apiKey   = apiKey   ?? throw new ArgumentNullException(nameof(apiKey));
            _provider = provider;

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("BaseUrl must be set.", nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/') + "/";
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

            var path = _provider == LlmProvider.GitHubModels
                ? "inference/chat/completions"
                : "v1/chat/completions";

            // New HttpClient per call — avoids BaseAddress mutation on shared instance
            using var http = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(120) };
            using var req  = new HttpRequestMessage(HttpMethod.Post, path);

            if (!string.IsNullOrWhiteSpace(_apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            req.Content = new StringContent(
                JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[{_model}@{_baseUrl}] HTTP {(int)resp.StatusCode}: {json}");

            var obj     = JObject.Parse(json);
            var content = obj["choices"]?[0]?["message"]?["content"]?.ToString();
            return string.IsNullOrWhiteSpace(content) ? json : content!;
        }
    }
}
