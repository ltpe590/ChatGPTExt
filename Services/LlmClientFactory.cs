using System;
using System.Net.Http;

namespace ChatGptVsix.Services
{
    internal static class LlmClientFactory
    {
        public static ILlmClient Create(ProviderSettings settings, HttpClient http)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (http    == null) throw new ArgumentNullException(nameof(http));

            switch (settings.Provider)
            {
                // ── All OpenAI-compatible providers ───────────────────────────
                case LlmProvider.GitHubModels:
                case LlmProvider.OpenAI:
                case LlmProvider.AzureOpenAI:
                case LlmProvider.Groq:
                case LlmProvider.OpenRouter:
                case LlmProvider.Mistral:
                case LlmProvider.LmStudio:
                    return new OpenAiClient(http, settings.Model, settings.ApiKey,
                                            settings.BaseUrl, settings.Provider);

                // ── Ollama — own API shape ────────────────────────────────────
                case LlmProvider.Ollama:
                    return new OllamaClient(http, settings.Model, settings.BaseUrl);

                default:
                    throw new NotSupportedException($"Unknown provider: {settings.Provider}");
            }
        }
    }
}
