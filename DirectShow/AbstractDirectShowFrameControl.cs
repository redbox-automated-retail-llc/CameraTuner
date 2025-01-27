using Redbox.DirectShow.Interop;
using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Redbox.DirectShow
{
    public abstract class AbstractDirectShowFrameControl : IVideoSource, ISampleGrabberCB
    {
        protected readonly string ControlMoniker;
        protected readonly int GrabWaitTime;
        protected readonly bool Debug;
        protected string bitmapToSave;
        private string deviceMoniker;
        private int framesReceived;
        private Size FrameSize;
        private Redbox.DirectShow.VideoCapabilities videoResolution;
        private Thread thread;
        private Redbox.DirectShow.VideoCapabilities[] videoCapabilities;
        private bool needToSetVideoInput;
        private object sourceObject;
        private int m_request;
        private bool? isCrossbarAvailable;
        private VideoInput[] crossbarVideoInputs;
        private VideoInput crossbarVideoInput = VideoInput.Default;
        private readonly ManualResetEvent StartEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent StopEvent = new ManualResetEvent(false);
        private readonly object sync = new object();
        private static Dictionary<string, Redbox.DirectShow.VideoCapabilities[]> cacheVideoCapabilities = new Dictionary<string, Redbox.DirectShow.VideoCapabilities[]>();
        private static Dictionary<string, VideoInput[]> cacheCrossbarVideoInputs = new Dictionary<string, VideoInput[]>();

        public static AbstractDirectShowFrameControl GetControl(
          FrameControls control,
          string moniker,
          int grab,
          bool debug)
        {
            if (control == FrameControls.Unknown)
                throw new ArgumentException(nameof(control));
            return FrameControls.Modern == control ? (AbstractDirectShowFrameControl)new FrameControlModern(moniker, grab, debug) : (AbstractDirectShowFrameControl)new FrameControlLegacy(moniker, grab, debug);
        }

        public int SampleCB(double sampleTime, IntPtr sample) => 0;

        public unsafe int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (!this.SnapRequested)
                return 0;
            if (this.Debug)
            {
                LogHelper.Instance.Log("[{0}]: BufferCB called ( request = {1} ).", (object)this.ControlMoniker, (object)this.m_request);
                if (File.Exists(this.bitmapToSave))
                    LogHelper.Instance.Log("{0} WARNING: the file {1} already exists.", (object)this.ControlMoniker, (object)this.bitmapToSave);
            }
            ++this.framesReceived;
            using (Bitmap bitmap = new Bitmap(this.FrameSize.Width, this.FrameSize.Height, PixelFormat.Format24bppRgb))
            {
                BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, this.FrameSize.Width, this.FrameSize.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride1 = bitmapdata.Stride;
                int stride2 = bitmapdata.Stride;
                byte* dst = (byte*)bitmapdata.Scan0.ToPointer() + stride2 * (this.FrameSize.Height - 1);
                byte* pointer = (byte*)buffer.ToPointer();
                for (int index = 0; index < this.FrameSize.Height; ++index)
                {
                    Win32.memcpy(dst, pointer, stride1);
                    dst -= stride2;
                    pointer += stride1;
                }
                bitmap.UnlockBits(bitmapdata);
                bitmap.Save(this.bitmapToSave);
            }
            this.OnImageCaptured();
            return 0;
        }

        public event NewFrameEventHandler NewFrame
        {
            add
            {
            }
            remove
            {
            }
        }

        public event VideoSourceErrorEventHandler VideoSourceError
        {
            add
            {
            }
            remove
            {
            }
        }

        public event PlayingFinishedEventHandler PlayingFinished;

        public virtual string Source => this.deviceMoniker;

        public int FramesReceived
        {
            get
            {
                int framesReceived = this.framesReceived;
                this.framesReceived = 0;
                return framesReceived;
            }
        }

        public bool IsRunning
        {
            get
            {
                if (this.thread != null)
                {
                    if (!this.thread.Join(0))
                        return true;
                    this.Free();
                }
                return false;
            }
        }

        public Redbox.DirectShow.VideoCapabilities VideoResolution
        {
            get => this.videoResolution;
            set => this.videoResolution = value;
        }

        public Redbox.DirectShow.VideoCapabilities[] VideoCapabilities
        {
            get
            {
                if (this.videoCapabilities == null)
                {
                    lock (AbstractDirectShowFrameControl.cacheVideoCapabilities)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (AbstractDirectShowFrameControl.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                                this.videoCapabilities = AbstractDirectShowFrameControl.cacheVideoCapabilities[this.deviceMoniker];
                        }
                    }
                    if (this.videoCapabilities == null)
                    {
                        if (!this.IsRunning)
                        {
                            this.WorkerThread(false);
                        }
                        else
                        {
                            for (int index = 0; index < 500 && this.videoCapabilities == null; ++index)
                                Thread.Sleep(10);
                        }
                    }
                }
                return this.videoCapabilities == null ? new Redbox.DirectShow.VideoCapabilities[0] : this.videoCapabilities;
            }
        }

        public bool Start()
        {
            if (this.IsRunning)
                return true;
            if (string.IsNullOrEmpty(this.deviceMoniker))
            {
                LogHelper.Instance.Log("[{0}] Video source is not specified - cannot start graph.", (object)this.ControlMoniker);
                return false;
            }
            this.framesReceived = 0;
            this.isCrossbarAvailable = new bool?();
            this.needToSetVideoInput = true;
            lock (this.sync)
            {
                this.thread = new Thread(new ThreadStart(this.WorkerThread));
                this.thread.Name = this.deviceMoniker;
                this.thread.Start();
            }
            if (this.StartEvent.WaitOne(10000))
                return true;
            LogHelper.Instance.Log("timed out waiting for start event.");
            return false;
        }

        public void SignalToStop() => this.SignalToStop(true);

        public void SignalToStop(bool wait)
        {
            if (this.thread == null)
                return;
            this.StopEvent.Set();
            if (wait)
                this.thread.Join();
            this.Free();
        }

        public void WaitForStop() => this.SignalToStop();

        public void Stop() => this.SignalToStop();

        public bool SimulateTrigger(string fileName, int waitTime)
        {
            this.bitmapToSave = fileName;
            ++this.m_request;
            if (this.Debug)
                LogHelper.Instance.Log("[{0}] SnapRequest {1}", (object)this.ControlMoniker, (object)this.m_request);
            try
            {
                return this.OnSimulateTrigger(fileName, waitTime);
            }
            finally
            {
                this.bitmapToSave = (string)null;
            }
        }

        protected AbstractDirectShowFrameControl(string deviceMoniker, int snapTimeout, bool debug)
        {
            this.deviceMoniker = deviceMoniker;
            this.GrabWaitTime = snapTimeout;
            this.Debug = debug;
            this.ControlMoniker = this.GetType().Name;
        }

        protected abstract bool OnSimulateTrigger(string file, int captureWait);

        protected abstract void OnImageCaptured();

        protected virtual bool OnGraphEvent() => true;

        protected abstract void OnFree();

        protected abstract bool SnapRequested { get; }

        private void Free()
        {
            this.thread = (Thread)null;
            this.StopEvent.Close();
            this.StartEvent.Close();
            try
            {
                this.OnFree();
            }
            catch
            {
            }
        }

        private void WorkerThread() => this.WorkerThread(true);

        private void WorkerThread(bool runGraph)
        {
            ReasonToFinishPlaying reason = ReasonToFinishPlaying.StoppedByUser;
            object o1 = (object)null;
            object o2 = (object)null;
            object o3 = (object)null;
            object retInterface = (object)null;
            ICaptureGraphBuilder2 captureGraphBuilder2 = (ICaptureGraphBuilder2)null;
            IFilterGraph2 filterGraph2_1 = (IFilterGraph2)null;
            IBaseFilter baseFilter1 = (IBaseFilter)null;
            IBaseFilter baseFilter2 = (IBaseFilter)null;
            ISampleGrabber sampleGrabber1 = (ISampleGrabber)null;
            IMediaControl mediaControl1 = (IMediaControl)null;
            IMediaEventEx mediaEventEx1 = (IMediaEventEx)null;
            IAMCrossbar crossbar = (IAMCrossbar)null;
            try
            {
                Type typeFromClsid1 = Type.GetTypeFromCLSID(Clsid.CaptureGraphBuilder2);
                if (typeFromClsid1 == null)
                {
                    LogHelper.Instance.Log("[{0}] Failed creating capture graph builder", (object)this.ControlMoniker);
                    return;
                }
                o1 = Activator.CreateInstance(typeFromClsid1);
                ICaptureGraphBuilder2 graphBuilder = (ICaptureGraphBuilder2)o1;
                Type typeFromClsid2 = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (typeFromClsid2 == null)
                {
                    LogHelper.Instance.Log("[{0}]Failed creating filter graph", (object)this.ControlMoniker);
                    return;
                }
                o2 = Activator.CreateInstance(typeFromClsid2);
                IFilterGraph2 filterGraph2_2 = (IFilterGraph2)o2;
                graphBuilder.SetFiltergraph((IGraphBuilder)filterGraph2_2);
                this.sourceObject = FilterInfo.CreateFilter(this.deviceMoniker);
                if (this.sourceObject == null)
                {
                    LogHelper.Instance.Log("[{0}] Failed creating device object for moniker", (object)this.ControlMoniker);
                    return;
                }
                IBaseFilter sourceObject1 = (IBaseFilter)this.sourceObject;
                try
                {
                    IAMVideoControl sourceObject2 = (IAMVideoControl)this.sourceObject;
                }
                catch
                {
                }
                Type typeFromClsid3 = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (typeFromClsid3 == null)
                {
                    LogHelper.Instance.Log("[{0}] Failed creating sample grabber", (object)this.ControlMoniker);
                    return;
                }
                o3 = Activator.CreateInstance(typeFromClsid3);
                ISampleGrabber sampleGrabber2 = (ISampleGrabber)o3;
                IBaseFilter baseFilter3 = (IBaseFilter)o3;
                filterGraph2_2.AddFilter(sourceObject1, "source");
                filterGraph2_2.AddFilter(baseFilter3, "grabber_video");
                AMMediaType mediaType = new AMMediaType();
                mediaType.MajorType = MediaType.Video;
                mediaType.SubType = MediaSubType.RGB24;
                sampleGrabber2.SetMediaType(mediaType);
                graphBuilder.FindInterface(FindDirection.UpstreamOnly, Guid.Empty, sourceObject1, typeof(IAMCrossbar).GUID, out retInterface);
                if (retInterface != null)
                    crossbar = (IAMCrossbar)retInterface;
                this.isCrossbarAvailable = new bool?(crossbar != null);
                this.crossbarVideoInputs = this.ColletCrossbarVideoInputs(crossbar);
                sampleGrabber2.SetBufferSamples(false);
                sampleGrabber2.SetOneShot(false);
                sampleGrabber2.SetCallback((ISampleGrabberCB)this, 1);
                this.GetPinCapabilitiesAndConfigureSizeAndRate(graphBuilder, sourceObject1, PinCategory.Capture, this.videoResolution, ref this.videoCapabilities);
                lock (AbstractDirectShowFrameControl.cacheVideoCapabilities)
                {
                    if (this.videoCapabilities != null)
                    {
                        if (!AbstractDirectShowFrameControl.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                            AbstractDirectShowFrameControl.cacheVideoCapabilities.Add(this.deviceMoniker, this.videoCapabilities);
                    }
                }
                if (runGraph)
                {
                    graphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, (object)sourceObject1, (IBaseFilter)null, baseFilter3);
                    if (sampleGrabber2.GetConnectedMediaType(mediaType) == 0)
                    {
                        VideoInfoHeader structure = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                        this.FrameSize = new Size(structure.BmiHeader.Width, structure.BmiHeader.Height);
                        mediaType.Dispose();
                    }
                    IMediaControl mediaControl2 = (IMediaControl)o2;
                    IMediaEventEx mediaEventEx2 = (IMediaEventEx)o2;
                    mediaControl2.Run();
                    this.StartEvent.Set();
                    LogHelper.Instance.Log("[{0}] graph is running; avg rate = {1}.", (object)this.ControlMoniker, (object)this.videoResolution.AverageFrameRate);
                    do
                    {
                        DsEvCode lEventCode;
                        IntPtr lParam1;
                        IntPtr lParam2;
                        if (mediaEventEx2 != null && mediaEventEx2.GetEvent(out lEventCode, out lParam1, out lParam2, 0) == 0U)
                        {
                            mediaEventEx2.FreeEventParams(lEventCode, lParam1, lParam2);
                            if (this.Debug)
                                LogHelper.Instance.Log("[{0}]Event: {1}", (object)this.ControlMoniker, (object)lEventCode.ToString());
                            if (lEventCode == DsEvCode.DeviceLost)
                            {
                                reason = ReasonToFinishPlaying.DeviceLost;
                                break;
                            }
                        }
                        if (this.needToSetVideoInput)
                        {
                            this.needToSetVideoInput = false;
                            if (this.isCrossbarAvailable.Value)
                            {
                                this.SetCurrentCrossbarInput(crossbar, this.crossbarVideoInput);
                                this.crossbarVideoInput = this.GetCurrentCrossbarInput(crossbar);
                            }
                        }
                        if (!this.OnGraphEvent() && mediaEventEx2 != null && mediaEventEx2.GetEvent(out lEventCode, out lParam1, out lParam2, 0) == 0U)
                        {
                            mediaEventEx2.FreeEventParams(lEventCode, lParam1, lParam2);
                            LogHelper.Instance.Log(">> [{0}] Event: {1}", (object)this.ControlMoniker, (object)lEventCode.ToString());
                        }
                    }
                    while (!this.StopEvent.WaitOne(100, false));
                    mediaControl2.Stop();
                    LogHelper.Instance.Log("[{0}] Shutdown statistics: there were {1} frames received via video.", (object)this.ControlMoniker, (object)this.framesReceived);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                captureGraphBuilder2 = (ICaptureGraphBuilder2)null;
                filterGraph2_1 = (IFilterGraph2)null;
                baseFilter1 = (IBaseFilter)null;
                mediaControl1 = (IMediaControl)null;
                mediaEventEx1 = (IMediaEventEx)null;
                baseFilter2 = (IBaseFilter)null;
                sampleGrabber1 = (ISampleGrabber)null;
                if (o2 != null)
                    Marshal.ReleaseComObject(o2);
                if (this.sourceObject != null)
                {
                    Marshal.ReleaseComObject(this.sourceObject);
                    this.sourceObject = (object)null;
                }
                if (o3 != null)
                    Marshal.ReleaseComObject(o3);
                if (o1 != null)
                    Marshal.ReleaseComObject(o1);
                if (retInterface != null)
                    Marshal.ReleaseComObject(retInterface);
            }
            if (this.PlayingFinished == null)
                return;
            this.PlayingFinished((object)this, reason);
        }

        private void SetResolution(IAMStreamConfig streamConfig, Redbox.DirectShow.VideoCapabilities resolution)
        {
            if (resolution == (Redbox.DirectShow.VideoCapabilities)null)
                return;
            int count = 0;
            int size = 0;
            AMMediaType mediaType = (AMMediaType)null;
            VideoStreamConfigCaps streamConfigCaps = new VideoStreamConfigCaps();
            streamConfig.GetNumberOfCapabilities(out count, out size);
            for (int index = 0; index < count; ++index)
            {
                try
                {
                    Redbox.DirectShow.VideoCapabilities videoCapabilities = new Redbox.DirectShow.VideoCapabilities(streamConfig, index);
                    if (resolution == videoCapabilities)
                    {
                        if (streamConfig.GetStreamCaps(index, out mediaType, streamConfigCaps) == 0)
                            break;
                    }
                }
                catch
                {
                }
            }
            if (mediaType == null)
                return;
            streamConfig.SetFormat(mediaType);
            mediaType.Dispose();
        }

        private void GetPinCapabilitiesAndConfigureSizeAndRate(
          ICaptureGraphBuilder2 graphBuilder,
          IBaseFilter baseFilter,
          Guid pinCategory,
          Redbox.DirectShow.VideoCapabilities resolutionToSet,
          ref Redbox.DirectShow.VideoCapabilities[] capabilities)
        {
            object retInterface;
            graphBuilder.FindInterface(pinCategory, MediaType.Video, baseFilter, typeof(IAMStreamConfig).GUID, out retInterface);
            if (retInterface != null)
            {
                IAMStreamConfig amStreamConfig = (IAMStreamConfig)null;
                try
                {
                    amStreamConfig = (IAMStreamConfig)retInterface;
                }
                catch (InvalidCastException ex)
                {
                }
                if (amStreamConfig != null)
                {
                    if (capabilities == null)
                    {
                        try
                        {
                            capabilities = Redbox.DirectShow.VideoCapabilities.FromStreamConfig(amStreamConfig);
                        }
                        catch
                        {
                        }
                    }
                    if (resolutionToSet != (Redbox.DirectShow.VideoCapabilities)null)
                        this.SetResolution(amStreamConfig, resolutionToSet);
                }
            }
            if (capabilities != null)
                return;
            capabilities = new Redbox.DirectShow.VideoCapabilities[0];
        }

        private VideoInput[] ColletCrossbarVideoInputs(IAMCrossbar crossbar)
        {
            lock (AbstractDirectShowFrameControl.cacheCrossbarVideoInputs)
            {
                if (AbstractDirectShowFrameControl.cacheCrossbarVideoInputs.ContainsKey(this.deviceMoniker))
                    return AbstractDirectShowFrameControl.cacheCrossbarVideoInputs[this.deviceMoniker];
                List<VideoInput> videoInputList = new List<VideoInput>();
                int inputPinCount;
                if (crossbar != null && crossbar.get_PinCounts(out int _, out inputPinCount) == 0)
                {
                    for (int index = 0; index < inputPinCount; ++index)
                    {
                        PhysicalConnectorType physicalType;
                        if (crossbar.get_CrossbarPinInfo(true, index, out int _, out physicalType) == 0 && physicalType < PhysicalConnectorType.AudioTuner)
                            videoInputList.Add(new VideoInput(index, physicalType));
                    }
                }
                VideoInput[] array = new VideoInput[videoInputList.Count];
                videoInputList.CopyTo(array);
                AbstractDirectShowFrameControl.cacheCrossbarVideoInputs.Add(this.deviceMoniker, array);
                return array;
            }
        }

        private VideoInput GetCurrentCrossbarInput(IAMCrossbar crossbar)
        {
            VideoInput currentCrossbarInput = VideoInput.Default;
            int outputPinCount;
            if (crossbar.get_PinCounts(out outputPinCount, out int _) == 0)
            {
                int outputPinIndex = -1;
                int pinIndexRelated;
                for (int pinIndex = 0; pinIndex < outputPinCount; ++pinIndex)
                {
                    PhysicalConnectorType physicalType;
                    if (crossbar.get_CrossbarPinInfo(false, pinIndex, out pinIndexRelated, out physicalType) == 0 && physicalType == PhysicalConnectorType.VideoDecoder)
                    {
                        outputPinIndex = pinIndex;
                        break;
                    }
                }
                int inputPinIndex;
                if (outputPinIndex != -1 && crossbar.get_IsRoutedTo(outputPinIndex, out inputPinIndex) == 0)
                {
                    PhysicalConnectorType physicalType;
                    crossbar.get_CrossbarPinInfo(true, inputPinIndex, out pinIndexRelated, out physicalType);
                    currentCrossbarInput = new VideoInput(inputPinIndex, physicalType);
                }
            }
            return currentCrossbarInput;
        }

        private void SetCurrentCrossbarInput(IAMCrossbar crossbar, VideoInput videoInput)
        {
            int outputPinCount;
            int inputPinCount;
            if (videoInput.Type == PhysicalConnectorType.Default || crossbar.get_PinCounts(out outputPinCount, out inputPinCount) != 0)
                return;
            int outputPinIndex = -1;
            int inputPinIndex = -1;
            int pinIndexRelated;
            PhysicalConnectorType physicalType;
            for (int pinIndex = 0; pinIndex < outputPinCount; ++pinIndex)
            {
                if (crossbar.get_CrossbarPinInfo(false, pinIndex, out pinIndexRelated, out physicalType) == 0 && physicalType == PhysicalConnectorType.VideoDecoder)
                {
                    outputPinIndex = pinIndex;
                    break;
                }
            }
            for (int pinIndex = 0; pinIndex < inputPinCount; ++pinIndex)
            {
                if (crossbar.get_CrossbarPinInfo(true, pinIndex, out pinIndexRelated, out physicalType) == 0 && physicalType == videoInput.Type && pinIndex == videoInput.Index)
                {
                    inputPinIndex = pinIndex;
                    break;
                }
            }
            if (inputPinIndex == -1 || outputPinIndex == -1 || crossbar.CanRoute(outputPinIndex, inputPinIndex) != 0)
                return;
            crossbar.Route(outputPinIndex, inputPinIndex);
        }
    }
}
