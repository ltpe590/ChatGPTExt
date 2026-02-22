using ChatGptVsix.Services;
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
        // Single HttpClient for the control lifetime.
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;

        // Cache the client so you don't re-create it each request.
        private ILlmClient? _llmClient;

        public ChatGptToolWindowControl()
        {
            InitializeComponent();

            _httpClient = new HttpClient
            {
                // For OpenAI client, set this in OpenAiClient if it builds full URLs.
                // If OpenAiClient uses relative URLs, set BaseAddress here.
                // BaseAddress = new Uri("https://api.openai.com/"),
                Timeout = TimeSpan.FromSeconds(120)
            };
        }

        public void Dispose()
        {
            CancelInFlight();
            _cts?.Dispose();
            _httpClient.Dispose();
        }

        private void CancelInFlight()
        {
            try { _cts?.Cancel(); }
            catch { /* ignore */ }
        }

        private ILlmClient GetOrCreateClient()
        {
            const string model = "qwen2.5-coder:3b";
            return _llmClient ??= new OllamaClient(_httpClient, model);
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = SendButtonHandlerAsync();
        }

        private async Task SendButtonHandlerAsync()
        {
            // Keep event handler non-async; exceptions handled here.
            try
            {
                var prompt = PromptBox.Text;
                if (string.IsNullOrWhiteSpace(prompt))
                    return;

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

                const string system =
                    "You are a senior C# developer. Be concise. Provide actionable suggestions and code.";

                var client = GetOrCreateClient();
                var answer = await client.ChatAsync(system, prompt, ct).ConfigureAwait(true);

                ResponseBox.Text = answer;
            }
            catch (OperationCanceledException)
            {
                ResponseBox.Text = "Canceled.";
            }
            catch (Exception ex)
            {
                ResponseBox.Text = ex.ToString();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CancelInFlight();
        }

        private void SetBusy(bool isBusy)
        {
            // These controls must exist in XAML with x:Name="SendBtn" and "CancelBtn".
            if (SendBtn != null) SendBtn.IsEnabled = !isBusy;
            if (CancelBtn != null) CancelBtn.IsEnabled = isBusy;

            // Optional: cursor feedback
            Mouse.OverrideCursor = isBusy ? System.Windows.Input.Cursors.Wait : null;
        }
    }
}