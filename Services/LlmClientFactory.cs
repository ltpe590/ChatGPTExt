using System;

namespace ChatGptVsix.Services
{
    internal static class LlmClientFactory
    {
        public static ILlmClient Create(ProviderSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            switch (settings.Provider)
            {
                case LlmProvider.GitHubModels:
                case LlmProvider.OpenAI:
                case LlmProvider.AzureOpenAI:
                case LlmProvider.Groq:
                case LlmProvider.OpenRouter:
                case LlmProvider.Mistral:
                case LlmProvider.LmStudio:
                    return new OpenAiClient(settings.Model, settings.ApiKey,
                                            settings.BaseUrl, settings.Provider);

                case LlmProvider.Ollama:
                    return new OllamaClient(settings.Model, settings.BaseUrl);

                default:
                    throw new NotSupportedException($"Unknown provider: {settings.Provider}");
            }
        }
    }
}
