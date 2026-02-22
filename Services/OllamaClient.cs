using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services;

internal sealed class OllamaClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaClient(HttpClient http, string model)
    {
        _http = http;
        _model = model;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        // OpenAI-compatible Chat Completions endpoint on Ollama. :contentReference[oaicite:4]{index=4}
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = false
        };

        using var resp = await _http.PostAsJsonAsync("v1/chat/completions", payload, JsonOptions, ct);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama error ({(int)resp.StatusCode}): {body}");

        // Minimal parse: choices[0].message.content
        using var doc = JsonDocument.Parse(body);
        var content =
            doc.RootElement
               .GetProperty("choices")[0]
               .GetProperty("message")
               .GetProperty("content")
               .GetString();

        return content ?? string.Empty;
    }
}