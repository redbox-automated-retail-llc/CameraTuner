using System;
using System.Drawing;

namespace Redbox.DirectShow
{
    public class NewFrameEventArgs : EventArgs
    {
        private Bitmap frame;

        public NewFrameEventArgs(Bitmap frame) => this.frame = frame;

        public Bitmap Frame => this.frame;
    }
}
