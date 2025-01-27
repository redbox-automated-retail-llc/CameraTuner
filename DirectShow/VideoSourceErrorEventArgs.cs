using System;

namespace Redbox.DirectShow
{
    public class VideoSourceErrorEventArgs : EventArgs
    {
        private string description;

        public VideoSourceErrorEventArgs(string description) => this.description = description;

        public string Description => this.description;
    }
}
