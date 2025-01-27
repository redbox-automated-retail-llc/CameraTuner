using System;
using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public class OutputBox
    {
        private readonly ListBox TheBox;

        public void Write(string msg)
        {
            if (this.TheBox.InvokeRequired)
                this.TheBox.Invoke((Delegate)new OutputBox.WriteCallback(this.WriteToOutput), (object)msg);
            else
                this.WriteToOutput(msg);
        }

        public void Write(string fmt, params object[] stuff) => this.Write(string.Format(fmt, stuff));

        public void Clear() => this.TheBox.Items.Clear();

        public OutputBox(ListBox box) => this.TheBox = box;

        protected virtual void PostWrite(string s)
        {
        }

        protected virtual string PrewriteFormat(string msg) => msg;

        private void WriteToOutput(string msg)
        {
            this.TheBox.Items.Insert(0, (object)this.PrewriteFormat(msg));
            this.PostWrite(msg);
        }

        private delegate void WriteCallback(string s);
    }
}
