using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services;

internal interface ILlmClient
{
    Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}