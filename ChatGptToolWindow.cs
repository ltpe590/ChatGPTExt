using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace ChatGptVsix
{
    [Guid("1dfb2c2e-5f1a-4c1a-8a2a-43e2a1f7a9a2")]
    public class ChatGptToolWindow : ToolWindowPane
    {
        public ChatGptToolWindow() : base(null)
        {
            Caption = "AI Assistant";
            Content = new ChatGptToolWindowControl();
        }

        /// <summary>
        /// Called by VS after the tool window is created.
        /// Passes the package so the control can read Options at send-time.
        /// </summary>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            if (Content is ChatGptToolWindowControl ctrl)
                ctrl.SetPackage((AsyncPackage)Package);
        }
    }
}
