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

        // Track the last file path used in a Fix Errors request so Apply Fix knows where to write
        private string? _lastFixTargetFile;

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

        // ── Public API used by commands ───────────────────────────────────────

        /// <summary>Sets the target file for the next Apply Fix operation.</summary>
        public void SetFixTarget(string? filePath) => _lastFixTargetFile = filePath;

        public async Task SendAsync(string prompt)
        {
            CancelInFlight();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            SetBusy(true);
            ApplyFixBtn.IsEnabled  = false;
            ApplyStatusText.Text   = "";

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

                // Enable Apply Fix if a code block is present and we have a target file
                ApplyFixBtn.IsEnabled = !string.IsNullOrEmpty(_lastFixTargetFile)
                                        && answer.Contains("```");
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

        // ── Button handlers ───────────────────────────────────────────────────

        private void SendBtn_Click(object sender, RoutedEventArgs e)
            => _ = SendButtonHandlerAsync();

        private async Task SendButtonHandlerAsync()
        {
            try
            {
                var prompt = PromptBox.Text;
                if (string.IsNullOrWhiteSpace(prompt)) return;
                _lastFixTargetFile = null; // manual send — no auto apply target
                await SendAsync(prompt).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ResponseBox.Text = ex.ToString();
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => CancelInFlight();

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            PromptBox.Text        = "";
            ResponseBox.Text      = "";
            ApplyStatusText.Text  = "";
            ApplyFixBtn.IsEnabled = false;
            _lastFixTargetFile    = null;
            StatusText.Text       = "";
        }

        private void ApplyFixBtn_Click(object sender, RoutedEventArgs e)
            => _ = ApplyFixAsync();

        private async Task ApplyFixAsync()
        {
            if (_package == null || string.IsNullOrEmpty(_lastFixTargetFile)) return;

            ApplyFixBtn.IsEnabled = false;
            ApplyStatusText.Text  = "Applying fix...";

            try
            {
                var svc = new ApplyFixService(_package);
                var (success, message) = await svc.ApplyAsync(
                    ResponseBox.Text, _lastFixTargetFile!, CancellationToken.None)
                    .ConfigureAwait(true);

                ApplyStatusText.Text      = success ? $"✔ {message}" : $"✘ {message}";
                ApplyStatusText.Foreground = success
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;

                if (!success)
                    ApplyFixBtn.IsEnabled = true; // allow retry
            }
            catch (Exception ex)
            {
                ApplyStatusText.Text      = $"✘ {ex.Message}";
                ApplyStatusText.Foreground = System.Windows.Media.Brushes.Red;
                ApplyFixBtn.IsEnabled      = true;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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
            return LlmClientFactory.Create(settings);
        }

        private void CancelInFlight()
        {
            try { _cts?.Cancel(); } catch { }
        }

        private void SetBusy(bool isBusy)
        {
            if (SendBtn   != null) SendBtn.IsEnabled   = !isBusy;
            if (CancelBtn != null) CancelBtn.IsEnabled = isBusy;
            Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
        }
    }
}
