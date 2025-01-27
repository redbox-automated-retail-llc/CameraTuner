using System;
using System.Drawing;
using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public struct ButtonAspects : IDisposable
    {
        public readonly Button Button;
        private readonly Color RunningColor;
        private readonly Color CompleteColor;

        public void Dispose()
        {
            if (this.Button == null)
                return;
            this.Button.BackColor = this.CompleteColor;
            Application.DoEvents();
        }

        public string GetTagInstruction()
        {
            return this.Button != null ? this.Button.Tag as string : string.Empty;
        }

        internal ButtonAspects(Button b, Color running)
        {
            this.Button = b;
            this.RunningColor = running;
            if (this.Button == null)
            {
                this.CompleteColor = Color.LightGray;
            }
            else
            {
                this.CompleteColor = this.Button.BackColor;
                this.Button.BackColor = this.RunningColor;
            }
            Application.DoEvents();
        }
    }
}
