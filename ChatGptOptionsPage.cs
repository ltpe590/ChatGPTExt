using ChatGptVsix.Services;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace ChatGptVsix
{
    /// <summary>
    /// VS Options page: Tools > Options > AI Assistant
    /// All settings persist automatically via the VS settings store.
    /// </summary>
    public sealed class ChatGptOptionsPage : DialogPage
    {
        // ── Provider selection ────────────────────────────────────────────────

        [Category("Provider")]
        [DisplayName("Active Provider")]
        [Description("Select which LLM backend to use. Changing this updates Base URL and Model to defaults for that provider (your API key is preserved).")]
        [DefaultValue(LlmProvider.GitHubModels)]
        public LlmProvider ActiveProvider { get; set; } = LlmProvider.GitHubModels;

        // ── Connection ────────────────────────────────────────────────────────

        [Category("Connection")]
        [DisplayName("Base URL")]
        [Description(
            "API base URL for the selected provider.\n" +
            "GitHub Models : https://models.inference.ai.azure.com\n" +
            "OpenAI        : https://api.openai.com\n" +
            "Groq          : https://api.groq.com/openai\n" +
            "OpenRouter    : https://openrouter.ai/api\n" +
            "Mistral       : https://api.mistral.ai\n" +
            "Ollama        : http://localhost:11434\n" +
            "LM Studio     : http://localhost:1234\n" +
            "Azure OpenAI  : https://<resource>.openai.azure.com/openai/deployments/<deploy>")]
        public string BaseUrl { get; set; } = ProviderSettings.GitHubModels.BaseUrl;

        [Category("Connection")]
        [DisplayName("Model")]
        [Description(
            "Model name to request.\n" +
            "GitHub Models : gpt-4o, gpt-4o-mini, Llama-3.3-70B-Instruct\n" +
            "OpenAI        : gpt-4o, gpt-4o-mini, gpt-4.1\n" +
            "Groq          : llama-3.3-70b-versatile, gemma2-9b-it\n" +
            "OpenRouter    : meta-llama/llama-3.3-70b-instruct:free\n" +
            "Mistral       : mistral-small-latest, codestral-latest\n" +
            "Ollama        : qwen2.5-coder:3b (or any pulled model)\n" +
            "LM Studio     : match the model loaded in LM Studio")]
        public string Model { get; set; } = ProviderSettings.GitHubModels.Model;

        [Category("Connection")]
        [DisplayName("API Key")]
        [Description("API key or token for the selected provider. Not needed for local Ollama / LM Studio.")]
        public string ApiKey { get; set; } = "";

        [Category("Connection")]
        [DisplayName("Max Tokens")]
        [Description("Maximum tokens in the response. Higher = longer answers, more cost.")]
        [DefaultValue(1000)]
        public int MaxTokens { get; set; } = 1000;

        // ── Behaviour ─────────────────────────────────────────────────────────

        [Category("Behaviour")]
        [DisplayName("System Prompt")]
        [Description("System prompt sent on every request. Defines the assistant persona.")]
        public string SystemPrompt { get; set; } =
            "You are a senior C# and .NET developer assistant embedded in Visual Studio. " +
            "Be concise and precise. Provide actionable suggestions and working code snippets. " +
            "Prefer modern C# idioms (.NET 8+).";

        [Category("Behaviour")]
        [DisplayName("Surrounding Line Count")]
        [Description("How many lines of context around the selection to include in the prompt (0 = selection only).")]
        [DefaultValue(80)]
        public int SurroundingLineCount { get; set; } = 80;

        // ── Helpers used by LlmClientFactory ──────────────────────────────────

        /// <summary>
        /// Fills BaseUrl and Model from provider defaults when the user
        /// switches ActiveProvider without overriding those fields.
        /// Call this when the Options page is saved.
        /// </summary>
        public void ApplyProviderDefaults()
        {
            var defaults = ProviderSettings.DefaultsFor(ActiveProvider);
            if (string.IsNullOrWhiteSpace(BaseUrl) || BaseUrl == GetPreviousProviderDefault())
                BaseUrl = defaults.BaseUrl;
            if (string.IsNullOrWhiteSpace(Model) || Model == GetPreviousProviderDefault(isModel: true))
                Model = defaults.Model;
            _lastProvider = ActiveProvider;
        }

        private LlmProvider _lastProvider = LlmProvider.GitHubModels;
        private string GetPreviousProviderDefault(bool isModel = false)
        {
            var d = ProviderSettings.DefaultsFor(_lastProvider);
            return isModel ? d.Model : d.BaseUrl;
        }

        /// <summary>Builds a ProviderSettings snapshot from current page values.</summary>
        public ProviderSettings ToProviderSettings() => new ProviderSettings
        {
            Provider = ActiveProvider,
            BaseUrl  = BaseUrl,
            Model    = Model,
            ApiKey   = ApiKey,
        };
    }
}
