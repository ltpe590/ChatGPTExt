using ChatGptVsix.Services;
using Microsoft.VisualStudio.Shell;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatGptVsix
{
    public partial class ChatGptToolWindowControl : UserControl, IDisposable
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;
        private ILlmClient? _llmClient;

        // Package reference so we can read Options at send-time
        private AsyncPackage? _package;

        public ChatGptToolWindowControl()
        {
            InitializeComponent();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        /// <summary>Called by ChatGptToolWindow after creation so we can reach Options.</summary>
        public void SetPackage(AsyncPackage package) => _package = package;

        public void Dispose()
        {
            CancelInFlight();
            _cts?.Dispose();
            _httpClient.Dispose();
        }

        private void CancelInFlight()
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
        }

        private ILlmClient BuildClient()
        {
            ProviderSettings settings;

            if (_package != null)
            {
                var page = (ChatGptOptionsPage)_package.GetDialogPage(typeof(ChatGptOptionsPage));
                settings = page.ToProviderSettings();
            }
            else
            {
                // Fallback when package not yet wired (design-time / unit tests)
                settings = ProviderSettings.DefaultsFor(LlmProvider.GitHubModels);
            }

            // Invalidate cached client when settings change
            _llmClient = LlmClientFactory.Create(settings, _httpClient);

            // Update status label
            StatusText.Text = $"[{settings.Provider} / {settings.Model}]";

            return _llmClient;
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
            => _ = SendButtonHandlerAsync();

        private async Task SendButtonHandlerAsync()
        {
            try
            {
                var prompt = PromptBox.Text;
                if (string.IsNullOrWhiteSpace(prompt)) return;
                await SendAsync(prompt).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ResponseBox.Text = ex.ToString();
            }
        }

        public async Task SendAsync(string prompt)
        {
            CancelInFlight();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            SetBusy(true);
            try
            {
                ResponseBox.Text = string.Empty;

                // Read system prompt from options (falls back to default if package not set)
                string systemPrompt = "You are a senior C# developer. Be concise. Provide actionable suggestions and code.";
                if (_package != null)
                {
                    var page = (ChatGptOptionsPage)_package.GetDialogPage(typeof(ChatGptOptionsPage));
                    if (!string.IsNullOrWhiteSpace(page.SystemPrompt))
                        systemPrompt = page.SystemPrompt;
                }

                var client = BuildClient();
                var answer = await client.ChatAsync(systemPrompt, prompt, ct).ConfigureAwait(true);
                ResponseBox.Text = answer;
            }
            catch (OperationCanceledException)
            {
                ResponseBox.Text = "Canceled.";
            }
            catch (Exception ex)
            {
                ResponseBox.Text = $"Error: {ex.Message}\n\n{ex}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => CancelInFlight();

        private void SetBusy(bool isBusy)
        {
            if (SendBtn   != null) SendBtn.IsEnabled   = !isBusy;
            if (CancelBtn != null) CancelBtn.IsEnabled = isBusy;
            Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
        }
    }
}
