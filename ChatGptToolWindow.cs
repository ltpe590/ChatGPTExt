using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;

namespace ChatGptVsix
{
    [Guid("1dfb2c2e-5f1a-4c1a-8a2a-43e2a1f7a9a2")]
    public class ChatGptToolWindow : ToolWindowPane
    {
        public ChatGptToolWindow() : base(null)
        {
            Caption = "ChatGPT";
            Content = new ChatGptToolWindowControl();
        }
    }
}
