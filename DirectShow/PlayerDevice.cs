using Redbox.DirectShow.Interop;
using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Redbox.DirectShow
{
    public class PlayerDevice : IVideoSource
    {
        private string deviceMoniker;
        private int framesReceived;
        private Redbox.DirectShow.VideoCapabilities videoResolution;
        private Redbox.DirectShow.VideoCapabilities snapshotResolution;
        private Thread thread;
        private ManualResetEvent stopEvent;
        private Redbox.DirectShow.VideoCapabilities[] videoCapabilities;
        private Redbox.DirectShow.VideoCapabilities[] snapshotCapabilities;
        private bool needToSetVideoInput;
        private bool m_simulateTrigger;
        private bool needToDisplayPropertyPage;
        private bool needToDisplayCrossBarPropertyPage;
        private IntPtr parentWindowForPropertyPage = IntPtr.Zero;
        private object sourceObject;
        private object sync = new object();
        private bool? isCrossbarAvailable;
        private VideoInput[] crossbarVideoInputs;
        private VideoInput crossbarVideoInput = VideoInput.Default;
        private static Dictionary<string, Redbox.DirectShow.VideoCapabilities[]> cacheVideoCapabilities = new Dictionary<string, Redbox.DirectShow.VideoCapabilities[]>();
        private static Dictionary<string, Redbox.DirectShow.VideoCapabilities[]> cacheSnapshotCapabilities = new Dictionary<string, Redbox.DirectShow.VideoCapabilities[]>();
        private static Dictionary<string, VideoInput[]> cacheCrossbarVideoInputs = new Dictionary<string, VideoInput[]>();
        private readonly bool Debug;
        private readonly int GrabTime;
        private readonly List<Bitmap> ReceivedFrames = new List<Bitmap>();
        private readonly object m_listLock = new object();

        public VideoInput CrossbarVideoInput
        {
            get => this.crossbarVideoInput;
            set
            {
                this.needToSetVideoInput = true;
                this.crossbarVideoInput = value;
            }
        }

        public VideoInput[] AvailableCrossbarVideoInputs
        {
            get
            {
                if (this.crossbarVideoInputs == null)
                {
                    lock (PlayerDevice.cacheCrossbarVideoInputs)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (PlayerDevice.cacheCrossbarVideoInputs.ContainsKey(this.deviceMoniker))
                                this.crossbarVideoInputs = PlayerDevice.cacheCrossbarVideoInputs[this.deviceMoniker];
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

        public event NewFrameEventHandler NewFrame;

        public event NewFrameEventHandler SnapshotFrame;

        public event VideoSourceErrorEventHandler VideoSourceError;

        public event PlayingFinishedEventHandler PlayingFinished;

        public virtual string Source
        {
            get => this.deviceMoniker;
            set
            {
                this.deviceMoniker = value;
                this.videoCapabilities = (Redbox.DirectShow.VideoCapabilities[])null;
                this.snapshotCapabilities = (Redbox.DirectShow.VideoCapabilities[])null;
                this.crossbarVideoInputs = (VideoInput[])null;
                this.isCrossbarAvailable = new bool?();
            }
        }

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
                    lock (PlayerDevice.cacheVideoCapabilities)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (PlayerDevice.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                                this.videoCapabilities = PlayerDevice.cacheVideoCapabilities[this.deviceMoniker];
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

        public Redbox.DirectShow.VideoCapabilities[] SnapshotCapabilities
        {
            get
            {
                if (this.snapshotCapabilities == null)
                {
                    lock (PlayerDevice.cacheSnapshotCapabilities)
                    {
                        if (!string.IsNullOrEmpty(this.deviceMoniker))
                        {
                            if (PlayerDevice.cacheSnapshotCapabilities.ContainsKey(this.deviceMoniker))
                                this.snapshotCapabilities = PlayerDevice.cacheSnapshotCapabilities[this.deviceMoniker];
                        }
                    }
                    if (this.snapshotCapabilities == null)
                    {
                        if (!this.IsRunning)
                        {
                            this.WorkerThread(false);
                        }
                        else
                        {
                            for (int index = 0; index < 500 && this.snapshotCapabilities == null; ++index)
                                Thread.Sleep(10);
                        }
                    }
                }
                return this.snapshotCapabilities == null ? new Redbox.DirectShow.VideoCapabilities[0] : this.snapshotCapabilities;
            }
        }

        public object SourceObject => this.sourceObject;

        public PlayerDevice(string deviceMoniker, int grabTime, bool debug)
        {
            this.deviceMoniker = deviceMoniker;
            this.GrabTime = grabTime;
            this.Debug = debug;
        }

        public bool Start()
        {
            if (!this.IsRunning)
            {
                if (string.IsNullOrEmpty(this.deviceMoniker))
                {
                    LogHelper.Instance.Log("[DirectShow] Video source is not specified.");
                    return false;
                }
                this.framesReceived = 0;
                this.isCrossbarAvailable = new bool?();
                this.needToSetVideoInput = true;
                this.stopEvent = new ManualResetEvent(false);
                lock (this.sync)
                {
                    this.thread = new Thread(new ThreadStart(this.WorkerThread));
                    this.thread.Name = this.deviceMoniker;
                    this.thread.Start();
                }
            }
            return true;
        }

        public void SignalToStop()
        {
            if (this.thread == null)
                return;
            this.stopEvent.Set();
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
            this.stopEvent.Close();
            this.stopEvent = (ManualResetEvent)null;
        }

        public void DisplayPropertyPage(IntPtr parentWindow)
        {
            if (this.deviceMoniker == null || this.deviceMoniker == string.Empty)
                throw new ArgumentException("Video source is not specified.");
            lock (this.sync)
            {
                if (this.IsRunning)
                {
                    this.parentWindowForPropertyPage = parentWindow;
                    this.needToDisplayPropertyPage = true;
                }
                else
                {
                    object filter;
                    try
                    {
                        filter = FilterInfo.CreateFilter(this.deviceMoniker);
                    }
                    catch
                    {
                        throw new ApplicationException("Failed creating device object for moniker.");
                    }
                    if (!(filter is ISpecifyPropertyPages))
                        throw new NotSupportedException("The video source does not support configuration property page.");
                    this.DisplayPropertyPage(parentWindow, filter);
                    Marshal.ReleaseComObject(filter);
                }
            }
        }

        public void DisplayCrossbarPropertyPage(IntPtr parentWindow)
        {
            lock (this.sync)
            {
                for (int index = 0; index < 500 && !this.isCrossbarAvailable.HasValue && this.IsRunning; ++index)
                    Thread.Sleep(10);
                if (!this.IsRunning || !this.isCrossbarAvailable.HasValue)
                    throw new ApplicationException("The video source must be running in order to display crossbar property page.");
                if (!this.isCrossbarAvailable.Value)
                    throw new NotSupportedException("Crossbar configuration is not supported by currently running video source.");
                this.parentWindowForPropertyPage = parentWindow;
                this.needToDisplayCrossBarPropertyPage = true;
            }
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

        public void SimulateTrigger() => this.m_simulateTrigger = true;

        public bool SetCameraProperty(
          CameraControlProperty property,
          int value,
          CameraControlFlags controlFlags)
        {
            bool flag = true;
            if (this.deviceMoniker == null || string.IsNullOrEmpty(this.deviceMoniker))
                throw new ArgumentException("Video source is not specified.");
            lock (this.sync)
            {
                object filter;
                try
                {
                    filter = FilterInfo.CreateFilter(this.deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }
                if (!(filter is IAMCameraControl))
                    throw new NotSupportedException("The video source does not support camera control.");
                flag = ((IAMCameraControl)filter).Set(property, value, controlFlags) >= 0;
                Marshal.ReleaseComObject(filter);
            }
            return flag;
        }

        public bool GetCameraProperty(
          CameraControlProperty property,
          out int value,
          out CameraControlFlags controlFlags)
        {
            bool cameraProperty = true;
            if (this.deviceMoniker == null || string.IsNullOrEmpty(this.deviceMoniker))
                throw new ArgumentException("Video source is not specified.");
            lock (this.sync)
            {
                object filter;
                try
                {
                    filter = FilterInfo.CreateFilter(this.deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }
                if (!(filter is IAMCameraControl))
                    throw new NotSupportedException("The video source does not support camera control.");
                cameraProperty = ((IAMCameraControl)filter).Get(property, out value, out controlFlags) >= 0;
                Marshal.ReleaseComObject(filter);
            }
            return cameraProperty;
        }

        public bool GetCameraPropertyRange(
          CameraControlProperty property,
          out int minValue,
          out int maxValue,
          out int stepSize,
          out int defaultValue,
          out CameraControlFlags controlFlags)
        {
            bool cameraPropertyRange = true;
            if (this.deviceMoniker == null || string.IsNullOrEmpty(this.deviceMoniker))
                throw new ArgumentException("Video source is not specified.");
            lock (this.sync)
            {
                object filter;
                try
                {
                    filter = FilterInfo.CreateFilter(this.deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }
                if (!(filter is IAMCameraControl))
                    throw new NotSupportedException("The video source does not support camera control.");
                cameraPropertyRange = ((IAMCameraControl)filter).GetRange(property, out minValue, out maxValue, out stepSize, out defaultValue, out controlFlags) >= 0;
                Marshal.ReleaseComObject(filter);
            }
            return cameraPropertyRange;
        }

        private void WorkerThread() => this.WorkerThread(true);

        private void WorkerThread(bool runGraph)
        {
            ReasonToFinishPlaying reason = ReasonToFinishPlaying.StoppedByUser;
            SampleGrabber callback = new SampleGrabber(new ProcessImageCallback(this.OnNewFrame));
            callback.Grab = true;
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
            IAMCrossbar amCrossbar = (IAMCrossbar)null;
            try
            {
                o1 = Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.CaptureGraphBuilder2) ?? throw new ApplicationException("Failed creating capture graph builder"));
                ICaptureGraphBuilder2 graphBuilder = (ICaptureGraphBuilder2)o1;
                o2 = Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.FilterGraph) ?? throw new ApplicationException("Failed creating filter graph"));
                IFilterGraph2 filterGraph2_2 = (IFilterGraph2)o2;
                graphBuilder.SetFiltergraph((IGraphBuilder)filterGraph2_2);
                this.sourceObject = FilterInfo.CreateFilter(this.deviceMoniker);
                IBaseFilter baseFilter3 = this.sourceObject != null ? (IBaseFilter)this.sourceObject : throw new ApplicationException("Failed creating device object for moniker");
                try
                {
                    IAMVideoControl sourceObject = (IAMVideoControl)this.sourceObject;
                }
                catch
                {
                }
                o3 = Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.SampleGrabber) ?? throw new ApplicationException("Failed creating sample grabber"));
                ISampleGrabber sampleGrabber2 = (ISampleGrabber)o3;
                IBaseFilter baseFilter4 = (IBaseFilter)o3;
                filterGraph2_2.AddFilter(baseFilter3, "source");
                filterGraph2_2.AddFilter(baseFilter4, "grabber_video");
                AMMediaType mediaType = new AMMediaType();
                mediaType.MajorType = MediaType.Video;
                mediaType.SubType = MediaSubType.RGB24;
                sampleGrabber2.SetMediaType(mediaType);
                graphBuilder.FindInterface(FindDirection.UpstreamOnly, Guid.Empty, baseFilter3, typeof(IAMCrossbar).GUID, out retInterface);
                if (retInterface != null)
                    amCrossbar = (IAMCrossbar)retInterface;
                this.isCrossbarAvailable = new bool?(amCrossbar != null);
                this.crossbarVideoInputs = this.ColletCrossbarVideoInputs(amCrossbar);
                sampleGrabber2.SetBufferSamples(false);
                sampleGrabber2.SetOneShot(false);
                sampleGrabber2.SetCallback((ISampleGrabberCB)callback, 1);
                this.GetPinCapabilitiesAndConfigureSizeAndRate(graphBuilder, baseFilter3, PinCategory.Capture, this.videoResolution, ref this.videoCapabilities);
                this.snapshotCapabilities = new Redbox.DirectShow.VideoCapabilities[0];
                lock (PlayerDevice.cacheVideoCapabilities)
                {
                    if (this.videoCapabilities != null)
                    {
                        if (!PlayerDevice.cacheVideoCapabilities.ContainsKey(this.deviceMoniker))
                            PlayerDevice.cacheVideoCapabilities.Add(this.deviceMoniker, this.videoCapabilities);
                    }
                }
                lock (PlayerDevice.cacheSnapshotCapabilities)
                {
                    if (this.snapshotCapabilities != null)
                    {
                        if (!PlayerDevice.cacheSnapshotCapabilities.ContainsKey(this.deviceMoniker))
                            PlayerDevice.cacheSnapshotCapabilities.Add(this.deviceMoniker, this.snapshotCapabilities);
                    }
                }
                if (runGraph)
                {
                    graphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, (object)baseFilter3, (IBaseFilter)null, baseFilter4);
                    if (sampleGrabber2.GetConnectedMediaType(mediaType) == 0)
                    {
                        VideoInfoHeader structure = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                        callback.Size = new Size(structure.BmiHeader.Width, structure.BmiHeader.Height);
                        mediaType.Dispose();
                    }
                    IMediaControl mediaControl2 = (IMediaControl)o2;
                    IMediaEventEx mediaEventEx2 = (IMediaEventEx)o2;
                    mediaControl2.Run();
                    do
                    {
                        DsEvCode lEventCode;
                        IntPtr lParam1;
                        IntPtr lParam2;
                        if (mediaEventEx2 != null && mediaEventEx2.GetEvent(out lEventCode, out lParam1, out lParam2, 0) >= 0U)
                        {
                            mediaEventEx2.FreeEventParams(lEventCode, lParam1, lParam2);
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
                                this.SetCurrentCrossbarInput(amCrossbar, this.crossbarVideoInput);
                                this.crossbarVideoInput = this.GetCurrentCrossbarInput(amCrossbar);
                            }
                        }
                        if (this.m_simulateTrigger)
                        {
                            this.m_simulateTrigger = false;
                            lock (this.m_listLock)
                                this.ReceivedFrames.Clear();
                            callback.Grab = true;
                            Thread.Sleep(this.GrabTime);
                            this.m_simulateTrigger = callback.Grab = false;
                            if (this.Debug)
                                LogHelper.Instance.Log("There are {0} frames in the list.", (object)this.ReceivedFrames.Count);
                            lock (this.m_listLock)
                            {
                                if (this.ReceivedFrames.Count > 0 && this.SnapshotFrame != null)
                                    this.SnapshotFrame((object)this, new NewFrameEventArgs(this.ReceivedFrames[this.ReceivedFrames.Count - 1]));
                                else
                                    LogHelper.Instance.Log("[DirectShow] There are no captured frames from the device.");
                                foreach (System.Drawing.Image receivedFrame in this.ReceivedFrames)
                                    receivedFrame.Dispose();
                                this.ReceivedFrames.Clear();
                            }
                        }
                        if (this.needToDisplayPropertyPage)
                        {
                            this.needToDisplayPropertyPage = false;
                            this.DisplayPropertyPage(this.parentWindowForPropertyPage, this.sourceObject);
                            if (amCrossbar != null)
                                this.crossbarVideoInput = this.GetCurrentCrossbarInput(amCrossbar);
                        }
                        if (this.needToDisplayCrossBarPropertyPage)
                        {
                            this.needToDisplayCrossBarPropertyPage = false;
                            if (amCrossbar != null)
                            {
                                this.DisplayPropertyPage(this.parentWindowForPropertyPage, (object)amCrossbar);
                                this.crossbarVideoInput = this.GetCurrentCrossbarInput(amCrossbar);
                            }
                        }
                    }
                    while (!this.stopEvent.WaitOne(100, false));
                    mediaControl2.Stop();
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

        private void DisplayPropertyPage(IntPtr parentWindow, object sourceObject)
        {
            try
            {
                CAUUID pPages;
                ((ISpecifyPropertyPages)sourceObject).GetPages(out pPages);
                FilterInfo filterInfo = new FilterInfo(this.deviceMoniker);
                Win32.OleCreatePropertyFrame(parentWindow, 0, 0, filterInfo.Name, 1, ref sourceObject, pPages.cElems, pPages.pElems, 0, 0, IntPtr.Zero);
                Marshal.FreeCoTaskMem(pPages.pElems);
            }
            catch
            {
            }
        }

        private VideoInput[] ColletCrossbarVideoInputs(IAMCrossbar crossbar)
        {
            lock (PlayerDevice.cacheCrossbarVideoInputs)
            {
                if (PlayerDevice.cacheCrossbarVideoInputs.ContainsKey(this.deviceMoniker))
                    return PlayerDevice.cacheCrossbarVideoInputs[this.deviceMoniker];
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
                PlayerDevice.cacheCrossbarVideoInputs.Add(this.deviceMoniker, array);
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

        private void OnNewFrame(Bitmap image)
        {
            ++this.framesReceived;
            if (this.stopEvent.WaitOne(0, false) || this.NewFrame == null)
                return;
            this.NewFrame((object)this, new NewFrameEventArgs(image));
        }
    }
}
