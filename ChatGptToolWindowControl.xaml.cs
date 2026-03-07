using ChatGptVsix.Services;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatGptVsix
{
    public partial class ChatGptToolWindowControl : UserControl, IDisposable
    {
        private CancellationTokenSource? _cts;
        private AsyncPackage?            _package;

        public ChatGptToolWindowControl()
        {
            InitializeComponent();
        }

        public void SetPackage(AsyncPackage package) => _package = package;

        public void Dispose()
        {
            CancelInFlight();
            _cts?.Dispose();
        }

        private void CancelInFlight()
        {
            try { _cts?.Cancel(); } catch { }
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
                settings = ProviderSettings.DefaultsFor(LlmProvider.GitHubModels);
            }

            StatusText.Text = $"[{settings.Provider} / {settings.Model}]";
            return LlmClientFactory.Create(settings);  // each client owns its own HttpClient
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
