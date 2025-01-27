using Redbox.HAL.Component.Model;
using System.Threading;

namespace Redbox.DirectShow
{
    internal sealed class FrameControlLegacy : AbstractDirectShowFrameControl
    {
        private int m_simulateTrigger = ControlConstants.NotTriggered;
        private int m_grabImage = ControlConstants.NotTriggered;
        private readonly ManualResetEvent FrameReceivedEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent ImageCapturedEvent = new ManualResetEvent(false);

        protected override bool OnSimulateTrigger(string file, int captureWait)
        {
            this.ImageCapturedEvent.Reset();
            this.FrameReceivedEvent.Reset();
            if (Interlocked.Exchange(ref this.m_simulateTrigger, ControlConstants.Triggered) == ControlConstants.Triggered)
                return false;
            bool flag = this.ImageCapturedEvent.WaitOne(captureWait);
            if (!flag)
                LogHelper.Instance.Log("[{0}] The trigger event was not set.", (object)this.ControlMoniker);
            return flag;
        }

        protected override void OnImageCaptured() => this.FrameReceivedEvent.Set();

        protected override bool OnGraphEvent()
        {
            bool flag = true;
            if (Thread.VolatileRead(ref this.m_simulateTrigger) == ControlConstants.Triggered)
            {
                Interlocked.Exchange(ref this.m_grabImage, ControlConstants.Triggered);
                if (!this.FrameReceivedEvent.WaitOne(this.GrabWaitTime))
                {
                    Interlocked.Exchange(ref this.m_grabImage, ControlConstants.NotTriggered);
                    LogHelper.Instance.Log("[{0}] There are no captured frames from the device.", (object)this.ControlMoniker);
                    flag = false;
                }
                else
                    this.ImageCapturedEvent.Set();
                Interlocked.Exchange(ref this.m_simulateTrigger, ControlConstants.NotTriggered);
            }
            return flag;
        }

        protected override void OnFree()
        {
            this.FrameReceivedEvent.Close();
            this.ImageCapturedEvent.Close();
        }

        protected override bool SnapRequested
        {
            get
            {
                int num = Interlocked.CompareExchange(ref this.m_grabImage, ControlConstants.NotTriggered, ControlConstants.Triggered);
                return ControlConstants.Triggered == num;
            }
        }

        internal FrameControlLegacy(string moniker, int grab, bool debug)
          : base(moniker, grab, debug)
        {
        }
    }
}
