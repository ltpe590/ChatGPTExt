namespace ChatGptVsix.Services
{
    /// <summary>
    /// All supported LLM back-ends. Add new entries here — no other changes needed
    /// as long as the provider is OpenAI-compatible (or has its own client like Ollama).
    /// </summary>
    public enum LlmProvider
    {
        // ── Cloud (OpenAI-compatible API shape) ───────────────────────────────
        GitHubModels,   // Free  — https://models.inference.ai.azure.com
        OpenAI,         // Paid  — https://api.openai.com
        AzureOpenAI,    // Paid  — custom endpoint per deployment
        Groq,           // Free  — https://api.groq.com/openai
        OpenRouter,     // Free  — https://openrouter.ai/api
        Mistral,        // Free  — https://api.mistral.ai

        // ── Local ─────────────────────────────────────────────────────────────
        Ollama,         // http://localhost:11434  (Ollama-native API)
        LmStudio,       // http://localhost:1234   (OpenAI-compatible)
    }
}
