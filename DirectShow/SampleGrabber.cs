using Redbox.DirectShow.Interop;
using Redbox.HAL.Component.Model;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Redbox.DirectShow
{
    internal class SampleGrabber : ISampleGrabberCB
    {
        private readonly ProcessImageCallback ProcessImage;

        public int SampleCB(double sampleTime, IntPtr sample) => 0;

        public unsafe int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (!this.Grab)
                return 0;
            if (this.LogSnapReceived)
                LogHelper.Instance.Log("Snap requested.");
            Bitmap b = new Bitmap(this.Size.Width, this.Size.Height, PixelFormat.Format24bppRgb);
            BitmapData bitmapdata = b.LockBits(new Rectangle(0, 0, this.Size.Width, this.Size.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int stride1 = bitmapdata.Stride;
            int stride2 = bitmapdata.Stride;
            byte* dst = (byte*)bitmapdata.Scan0.ToPointer() + stride2 * (this.Size.Height - 1);
            byte* pointer = (byte*)buffer.ToPointer();
            for (int index = 0; index < this.Size.Height; ++index)
            {
                Win32.memcpy(dst, pointer, stride1);
                dst -= stride2;
                pointer += stride1;
            }
            b.UnlockBits(bitmapdata);
            this.ProcessImage(b);
            b.Dispose();
            return 0;
        }

        internal bool LogSnapReceived { get; set; }

        internal Size Size { get; set; }

        internal bool Grab { get; set; }

        internal SampleGrabber(ProcessImageCallback callback)
        {
            this.ProcessImage = callback != null ? callback : throw new ArgumentException("Callback cannot be null.");
        }
    }
}
