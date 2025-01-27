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
    public class DirectShowFrameControl : IVideoSource, ISampleGrabberCB
    {
        private string deviceMoniker;
        private int framesReceived;
        private Size FrameSize;
        private Redbox.DirectShow.VideoCapabilities videoResolution;
        private Redbox.DirectShow.VideoCapabilities snapshotResolution;
        private string bitmapToSave;
        private Thread thread;
        private Redbox.DirectShow.VideoCapabilities[] videoCapabilities;
        private bool needToSetVideoInput;
        private int m_simulateTrigger = ControlConstants.NotTriggered;
        private object sourceObject;
        private int m_grabImage = ControlConstants.NotTriggered;
        private int m_request;
        private bool? isCrossbarAvailable;
        private VideoInput[] crossbarVideoInputs;
        private VideoInput crossbarVideoInput = VideoInput.Default;
        private const string Moniker = "DirectShowSlim";
        private readonly ManualResetEvent StartEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent FrameReceivedEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent ImageCapturedEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent StopEvent = new ManualResetEvent(false);
        private readonly object sync = new object();
        private readonly int GrabWaitTime;
        private readonly bool Debug;
        private static Dictionary<string, Redbox.DirectShow.VideoCapabilities[]> cacheVideoCapabilities = new Dictionary<string, Redbox.DirectShow.VideoCapabilities[]>();
        private static Dictionary<string, VideoInput[]> cacheCrossbarVideoInputs = new Dictionary<string, VideoInput[]>();

        public VideoInput CrossbarVideoInput
        {
            get => this.crossbarVideoInput;
            set
            {
                this.needToSetVideoInput = true;
                this.crossbarVideoInput = value;
            }
        }

        public int SampleCB(double sampleTime, IntPtr sample) => 0;

        public unsafe int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (Interlocked.CompareExchange(ref this.m_grabImage, ControlConstants.NotTriggered, ControlConstants.Triggered) == ControlConstants.NotTriggered)
                return 0;
            if (this.Debug)
            {
                LogHelper.Instance.Log("[{0}]: BufferCB called ( request = {1} ).", (object)"DirectShowSlim", (object)this.m_request);
                if (File.Exists(this.bitmapToSave))
                    LogHelper.Instance.Log("{0} WARNING: the file {1} already exists.", (object)"DirectShowSlim", (object)this.bitmapToSave);
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
            this.FrameReceivedEvent.Set();
            return 0;
        }

        public VideoInput[] AvailableCrossbarVideoInputs
        {
            get
            {
                if (this.crossbarVideoInputs == null)
                {
                    lock (DirectShowFrameControl.cacheCrossbarVideoInputs)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (DirectShowFrameControl.cacheCrossbarVideoInputs.ContainsKey(this.deviceMoniker))
                                this.crossbarVideoInputs = DirectShowFrameControl.cacheCrossbarVideoInputs[this.deviceMoniker];
                        }
                    }
                    if (this.crossbarVideoInputs == null)
                    {
                        if (!this.IsRunning)
                        {
                            this.WorkerThread(false);
                        }
                        else
                        {
                            for (int index = 0; index < 500 && this.crossbarVideoInputs == null; ++index)
                                Thread.Sleep(10);
                        }
                    }
                }
                return this.crossbarVideoInputs == null ? new VideoInput[0] : this.crossbarVideoInputs;
            }
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

        public event VideoSourceErrorEventHandler VideoSourceError;

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

        [Obsolete]
        public Size DesiredFrameSize
        {
            get => Size.Empty;
            set
            {
            }
        }

        [Obsolete]
        public Size DesiredSnapshotSize
        {
            get => Size.Empty;
            set
            {
            }
        }

        [Obsolete]
        public int DesiredFrameRate
        {
            get => 0;
            set
            {
            }
        }

        public Redbox.DirectShow.VideoCapabilities VideoResolution
        {
            get => this.videoResolution;
            set => this.videoResolution = value;
        }

        public Redbox.DirectShow.VideoCapabilities SnapshotResolution
        {
            get => this.snapshotResolution;
            set => this.snapshotResolution = value;
        }

        public Redbox.DirectShow.VideoCapabilities[] VideoCapabilities
        {
            get
            {
                if (this.videoCapabilities == null)
                {
                    lock (DirectShowFrameControl.cacheVideoCapabilities)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (DirectShowFrameControl.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                                this.videoCapabilities = DirectShowFrameControl.cacheVideoCapabilities[this.deviceMoniker];
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

        public object SourceObject => this.sourceObject;

        public DirectShowFrameControl(string deviceMoniker, int snapTimeout, bool debug)
        {
            this.deviceMoniker = deviceMoniker;
            this.GrabWaitTime = snapTimeout;
            this.Debug = debug;
        }

        public bool Start()
        {
            if (this.IsRunning)
                return true;
            if (string.IsNullOrEmpty(this.deviceMoniker))
            {
                LogHelper.Instance.Log("[{0}] Video source is not specified - cannot start graph.", (object)"DirectShowSlim");
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

        public void SignalToStop()
        {
            if (this.thread == null)
                return;
            this.StopEvent.Set();
        }

        public void WaitForStop()
        {
            if (this.thread == null)
                return;
            this.thread.Join();
            this.Free();
        }

        public void Stop()
        {
            if (!this.IsRunning)
                return;
            this.thread.Abort();
            this.WaitForStop();
        }

        private void Free()
        {
            this.thread = (Thread)null;
            this.StopEvent.Close();
            this.StartEvent.Close();
        }

        public bool CheckIfCrossbarAvailable()
        {
            lock (this.sync)
            {
                if (!this.isCrossbarAvailable.HasValue)
                {
                    if (!this.IsRunning)
                    {
                        this.WorkerThread(false);
                    }
                    else
                    {
                        for (int index = 0; index < 500 && !this.isCrossbarAvailable.HasValue; ++index)
                            Thread.Sleep(10);
                    }
                }
                return this.isCrossbarAvailable.HasValue && this.isCrossbarAvailable.Value;
            }
        }

        public bool SimulateTrigger(string fileName, int waitTime)
        {
            this.bitmapToSave = fileName;
            ++this.m_request;
            if (this.Debug)
                LogHelper.Instance.Log("[{0}] SnapRequest {1}", (object)"DirectShowSlim", (object)this.m_request);
            try
            {
                this.ImageCapturedEvent.Reset();
                this.FrameReceivedEvent.Reset();
                if (Interlocked.Exchange(ref this.m_simulateTrigger, ControlConstants.Triggered) == ControlConstants.Triggered)
                    return false;
                if (this.ImageCapturedEvent.WaitOne(waitTime))
                    return true;
                LogHelper.Instance.Log("[{0}] The trigger event was not set.", (object)"DirectShowSlim");
                return false;
            }
            finally
            {
                this.bitmapToSave = (string)null;
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
                    LogHelper.Instance.Log("[{0}] Failed creating capture graph builder", (object)"DirectShowSlim");
                    return;
                }
                o1 = Activator.CreateInstance(typeFromClsid1);
                ICaptureGraphBuilder2 graphBuilder = (ICaptureGraphBuilder2)o1;
                Type typeFromClsid2 = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (typeFromClsid2 == null)
                {
                    LogHelper.Instance.Log("[{0}]Failed creating filter graph", (object)"DirectShowSlim");
                    return;
                }
                o2 = Activator.CreateInstance(typeFromClsid2);
                IFilterGraph2 filterGraph2_2 = (IFilterGraph2)o2;
                graphBuilder.SetFiltergraph((IGraphBuilder)filterGraph2_2);
                this.sourceObject = FilterInfo.CreateFilter(this.deviceMoniker);
                if (this.sourceObject == null)
                {
                    LogHelper.Instance.Log("[{0}] Failed creating device object for moniker", (object)"DirectShowSlim");
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
                    LogHelper.Instance.Log("[{0}] Failed creating sample grabber", (object)"DirectShowSlim");
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
                lock (DirectShowFrameControl.cacheVideoCapabilities)
                {
                    if (this.videoCapabilities != null)
                    {
                        if (!DirectShowFrameControl.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                            DirectShowFrameControl.cacheVideoCapabilities.Add(this.deviceMoniker, this.videoCapabilities);
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
                    LogHelper.Instance.Log("[{0}] graph is running; avg rate = {1}.", (object)"DirectShowSlim", (object)this.videoResolution.AverageFrameRate);
                    do
                    {
                        DsEvCode lEventCode;
                        IntPtr lParam1;
                        IntPtr lParam2;
                        if (mediaEventEx2 != null && mediaEventEx2.GetEvent(out lEventCode, out lParam1, out lParam2, 0) == 0U)
                        {
                            mediaEventEx2.FreeEventParams(lEventCode, lParam1, lParam2);
                            if (this.Debug)
                                LogHelper.Instance.Log("[{0}]Event: {1}", (object)"DirectShowSlim", (object)lEventCode.ToString());
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
                        if (Thread.VolatileRead(ref this.m_simulateTrigger) == ControlConstants.Triggered)
                        {
                            Interlocked.Exchange(ref this.m_grabImage, ControlConstants.Triggered);
                            if (!this.FrameReceivedEvent.WaitOne(this.GrabWaitTime))
                            {
                                Interlocked.Exchange(ref this.m_grabImage, ControlConstants.NotTriggered);
                                LogHelper.Instance.Log("[{0}] There are no captured frames from the device.", (object)"DirectShowSlim");
                                if (mediaEventEx2 != null && mediaEventEx2.GetEvent(out lEventCode, out lParam1, out lParam2, 0) == 0U)
                                {
                                    mediaEventEx2.FreeEventParams(lEventCode, lParam1, lParam2);
                                    LogHelper.Instance.Log(">> [{0}] Event: {1}", (object)"DirectShowSlim", (object)lEventCode.ToString());
                                }
                            }
                            else
                                this.ImageCapturedEvent.Set();
                            Interlocked.Exchange(ref this.m_simulateTrigger, ControlConstants.NotTriggered);
                        }
                    }
                    while (!this.StopEvent.WaitOne(100, false));
                    mediaControl2.Stop();
                    LogHelper.Instance.Log("[{0}] Shutdown statistics: there were {1} frames received via video.", (object)"DirectShowSlim", (object)this.framesReceived);
                }
            }
            catch (Exception ex)
            {
                if (this.VideoSourceError != null)
                    this.VideoSourceError((object)this, new VideoSourceErrorEventArgs(ex.Message));
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
            lock (DirectShowFrameControl.cacheCrossbarVideoInputs)
            {
                if (DirectShowFrameControl.cacheCrossbarVideoInputs.ContainsKey(this.deviceMoniker))
                    return DirectShowFrameControl.cacheCrossbarVideoInputs[this.deviceMoniker];
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
                DirectShowFrameControl.cacheCrossbarVideoInputs.Add(this.deviceMoniker, array);
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
