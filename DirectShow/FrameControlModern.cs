using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Threading;
using System.IO;
using System.Threading;

namespace Redbox.DirectShow
{
    internal sealed class FrameControlModern : AbstractDirectShowFrameControl
    {
        private readonly AtomicFlag TriggerFlag = new AtomicFlag();
        private readonly ManualResetEvent ImageCapturedEvent = new ManualResetEvent(false);

        protected override bool OnSimulateTrigger(string file, int captureWait)
        {
            this.ImageCapturedEvent.Reset();
            if (!this.TriggerFlag.Set())
                return false;
            if (this.ImageCapturedEvent.WaitOne(captureWait))
                return File.Exists(file);
            LogHelper.Instance.Log("[{0}] The trigger event was not set.", (object)this.ControlMoniker);
            return false;
        }

        protected override void OnImageCaptured() => this.ImageCapturedEvent.Set();

        protected override void OnFree() => this.ImageCapturedEvent.Close();

        protected override bool SnapRequested => this.TriggerFlag.Clear();

        internal FrameControlModern(string moniker, int grab, bool debug)
          : base(moniker, grab, debug)
        {
        }
    }
}
