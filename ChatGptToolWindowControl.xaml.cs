using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatGptVsix
{
    public partial class ChatGptToolWindowControl : UserControl
    {
        public ChatGptToolWindowControl()
        {
            InitializeComponent();
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = SendButtonHandlerAsync();
        }

        private async Task SendButtonHandlerAsync()
        {
            try
            {
                var prompt = PromptBox.Text;

                if (string.IsNullOrWhiteSpace(prompt))
                    return;

                await SendAsync(prompt);
            }
            catch (Exception ex)
            {
                ResponseBox.Text = ex.ToString();
            }
        }

        public async Task SendAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            try
            {
                StatusText.Text = "Calling API...";
                SendBtn.IsEnabled = false;

                var model = string.IsNullOrWhiteSpace(ModelBox.Text) ? "gpt-5" : ModelBox.Text.Trim();
                var maxTokens = int.TryParse(MaxTokensBox.Text, out var mt) ? mt : 400;

                var text = await OpenAiClient.SendPromptAsync(prompt, model, maxTokens);
                ResponseBox.Text = text;
                StatusText.Text = "Done";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error";
                ResponseBox.Text = ex.ToString();
            }
            finally
            {
                SendBtn.IsEnabled = true;
            }
        }
    }
}
