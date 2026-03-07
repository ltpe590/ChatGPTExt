namespace ChatGptVsix.Services
{
    /// <summary>
    /// Holds all configuration for one LLM provider.
    /// Populated from the VS Options page; consumed by LlmClientFactory.
    /// </summary>
    public sealed class ProviderSettings
    {
        // ── Defaults per provider (used when user has not overridden) ─────────
        public static readonly ProviderSettings GitHubModels = new ProviderSettings
        {
            Provider    = LlmProvider.GitHubModels,
            BaseUrl     = "https://models.github.ai",
            Model       = "openai/gpt-4o",
            ApiKeyHint  = "GitHub PAT with models:read scope (github.com > Settings > Developer settings > PAT > Fine-grained)"
        };

        public static readonly ProviderSettings OpenAI = new ProviderSettings
        {
            Provider    = LlmProvider.OpenAI,
            BaseUrl     = "https://api.openai.com",
            Model       = "gpt-4o",
            ApiKeyHint  = "OpenAI API key from platform.openai.com"
        };

        public static readonly ProviderSettings AzureOpenAI = new ProviderSettings
        {
            Provider    = LlmProvider.AzureOpenAI,
            BaseUrl     = "https://<your-resource>.openai.azure.com/openai/deployments/<deployment>",
            Model       = "gpt-4o",
            ApiKeyHint  = "Azure OpenAI key from Azure Portal"
        };

        public static readonly ProviderSettings Groq = new ProviderSettings
        {
            Provider    = LlmProvider.Groq,
            BaseUrl     = "https://api.groq.com/openai",
            Model       = "llama-3.3-70b-versatile",
            ApiKeyHint  = "Groq API key from console.groq.com"
        };

        public static readonly ProviderSettings OpenRouter = new ProviderSettings
        {
            Provider    = LlmProvider.OpenRouter,
            BaseUrl     = "https://openrouter.ai/api",
            Model       = "meta-llama/llama-3.3-70b-instruct:free",
            ApiKeyHint  = "OpenRouter API key from openrouter.ai/keys"
        };

        public static readonly ProviderSettings Mistral = new ProviderSettings
        {
            Provider    = LlmProvider.Mistral,
            BaseUrl     = "https://api.mistral.ai",
            Model       = "mistral-small-latest",
            ApiKeyHint  = "Mistral API key from console.mistral.ai"
        };

        public static readonly ProviderSettings Ollama = new ProviderSettings
        {
            Provider    = LlmProvider.Ollama,
            BaseUrl     = "http://localhost:11434",
            Model       = "qwen2.5-coder:3b",
            ApiKeyHint  = "No key needed for local Ollama"
        };

        public static readonly ProviderSettings LmStudio = new ProviderSettings
        {
            Provider    = LlmProvider.LmStudio,
            BaseUrl     = "http://localhost:1234",
            Model       = "local-model",
            ApiKeyHint  = "No key needed for local LM Studio"
        };

        // ── Instance fields (set by Options page) ─────────────────────────────
        public LlmProvider Provider    { get; set; }
        public string       BaseUrl    { get; set; } = "";
        public string       Model      { get; set; } = "";
        public string       ApiKey     { get; set; } = "";  // stored by Options page
        public string       ApiKeyHint { get; private set; } = "";

        /// <summary>Returns the defaults for a given provider enum value.</summary>
        public static ProviderSettings DefaultsFor(LlmProvider provider)
        {
            switch (provider)
            {
                case LlmProvider.GitHubModels: return GitHubModels;
                case LlmProvider.OpenAI:       return OpenAI;
                case LlmProvider.AzureOpenAI:  return AzureOpenAI;
                case LlmProvider.Groq:         return Groq;
                case LlmProvider.OpenRouter:   return OpenRouter;
                case LlmProvider.Mistral:      return Mistral;
                case LlmProvider.Ollama:       return Ollama;
                case LlmProvider.LmStudio:     return LmStudio;
                default:                       return GitHubModels;
            }
        }
    }
}
