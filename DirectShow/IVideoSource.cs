namespace Redbox.DirectShow
{
    public interface IVideoSource
    {
        bool Start();

        void SignalToStop();

        void WaitForStop();

        void Stop();

        event NewFrameEventHandler NewFrame;

        event VideoSourceErrorEventHandler VideoSourceError;

        event PlayingFinishedEventHandler PlayingFinished;

        string Source { get; }

        int FramesReceived { get; }

        bool IsRunning { get; }
    }
}
