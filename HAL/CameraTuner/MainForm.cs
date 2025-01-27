using Redbox.DirectShow;
using Redbox.HAL.CameraTuner.Properties;
using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Redbox.HAL.CameraTuner
{
    public class MainForm : Form
    {
        private readonly AtomicFlag SnapFlag = new AtomicFlag();
        private readonly TunerLog TunerLogInstance;
        private readonly Size ImageSize;
        private readonly string ImagesFolder;
        private readonly AutoResetEvent ImageGrabbedWaiter;
        private readonly bool Details;
        private string CurrentImage;
        private FilterInfoCollection videoDevices;
        private PlayerDevice videoDevice;
        private IContainer components;
        private VideoSourcePlayer videoSourcePlayer;
        private Button button1;
        private Button button2;
        private Button button3;
        private TextBox m_numberOfBarcodesBox;
        private TextBox m_decodeTimeBox;
        private TextBox m_barcodeBox;
        private TextBox m_snapStatusBox;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label6;
        private TextBox m_detectedErrorsTB;
        private Button button4;
        private BackgroundWorker backgroundWorker1;
        private Label label5;
        private TextBox m_secureReadTB;

        public MainForm(TunerLog log)
        {
            this.InitializeComponent();
            this.TunerLogInstance = log;
            this.ImageGrabbedWaiter = new AutoResetEvent(false);
            this.ImageSize = new Size(640, 480);
            this.ImagesFolder = ServiceLocator.Instance.GetService<IRuntimeService>().InstallPath("Video");
            try
            {
                if (!Directory.Exists(this.ImagesFolder))
                    Directory.CreateDirectory(this.ImagesFolder);
            }
            catch
            {
            }
            this.Details = Settings.Default.DetailedLog;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (this.videoDevices == null || this.videoDevices.Count == 0)
            {
                this.WriteErrorText("Found no devices.");
            }
            else
            {
                this.videoDevice = new PlayerDevice(this.videoDevices[0].MonikerString, 500, false);
                if (this.videoDevice == null)
                {
                    this.WriteErrorText("Found no devices.");
                }
                else
                {
                    bool flag = false;
                    foreach (VideoCapabilities videoCapability in this.videoDevice.VideoCapabilities)
                    {
                        if (videoCapability.FrameSize == this.ImageSize)
                        {
                            flag = true;
                            this.videoDevice.VideoResolution = videoCapability;
                            break;
                        }
                    }
                    if (!flag)
                        LogHelper.Instance.Log("Unable to find video capability with size {0}w X {1}h", (object)this.Size.Width, (object)this.Size.Height);
                    this.videoDevice.PlayingFinished += new PlayingFinishedEventHandler(this.videoSource_PlayingFinished);
                    this.videoSourcePlayer.NewFrame += new VideoSourcePlayer.NewFrameHandler(this.SnapFrame);
                    this.videoSourcePlayer.VideoSource = (IVideoSource)this.videoDevice;
                    this.videoSourcePlayer.Start();
                    Thread.Sleep(Settings.Default.WakeupPause);
                    this.ResetView();
                }
            }
        }

        private void videoSource_PlayingFinished(object sender, ReasonToFinishPlaying reason)
        {
            if (ReasonToFinishPlaying.StoppedByUser == reason)
                return;
            string msg = reason.ToString();
            LogHelper.Instance.Log(msg);
            this.WriteErrorText(msg);
        }

        private void SnapFrame(object sender, ref Bitmap image)
        {
            if (!this.SnapFlag.Clear())
                return;
            this.CurrentImage = Path.Combine(this.ImagesFolder, ServiceLocator.Instance.GetService<IRuntimeService>().GenerateUniqueFile("jpg"));
            if (this.Details)
                LogHelper.Instance.Log("Snapframe called captured image {0}", (object)this.CurrentImage);
            image.Save(this.CurrentImage);
            this.ImageGrabbedWaiter.Set();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.OnShutdown();
            Application.Exit();
        }

        private void ResetView()
        {
            this.m_barcodeBox.Text = "None";
            this.m_decodeTimeBox.Text = "0.0 s";
            this.m_snapStatusBox.Text = string.Empty;
            this.m_secureReadTB.Text = this.m_numberOfBarcodesBox.Text = "0";
            Application.DoEvents();
        }

        private void OnShutdown()
        {
            this.ShutdownPlayer();
            this.TunerLogInstance.Dispose();
        }

        private void ShutdownPlayer()
        {
            if (this.videoSourcePlayer == null)
                return;
            this.videoSourcePlayer.SignalToStop();
            this.videoSourcePlayer.WaitForStop();
            if (this.videoDevice != null)
            {
                this.videoSourcePlayer.NewFrame -= new VideoSourcePlayer.NewFrameHandler(this.SnapFrame);
                this.videoDevice.PlayingFinished -= new PlayingFinishedEventHandler(this.videoSource_PlayingFinished);
            }
            this.videoSourcePlayer.VideoSource = (IVideoSource)(this.videoDevice = (PlayerDevice)null);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.button1.BackColor = Color.Red;
            this.button1.Enabled = false;
            this.ResetView();
            this.SnapFlag.Set();
            this.backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.videoDevice == null)
                return;
            this.videoDevice.DisplayPropertyPage(Process.GetCurrentProcess().MainWindowHandle);
        }

        private void WriteErrorText(string msg)
        {
            if (this.m_detectedErrorsTB.InvokeRequired)
                this.Invoke((Delegate)new MainForm.SetTextCallback(this.WriteErrorText), (object)msg);
            else
                this.m_detectedErrorsTB.Text = msg;
        }

        private void button4_Click_1(object sender, EventArgs e) => this.WriteErrorText(string.Empty);

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            bool flag = false;
            using (new AtomicFlagHelper(this.SnapFlag))
                flag = this.ImageGrabbedWaiter.WaitOne(5000);
            e.Result = flag ? (object)ScanResult.Scan(this.CurrentImage) : (object)ScanResult.ErrorResult();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.button1.BackColor = Color.LightGray;
            this.button1.Enabled = true;
            ScanResult result = e.Result as ScanResult;
            this.m_snapStatusBox.Text = !result.SnapOk ? "CAPTURE ERROR" : "SUCCESS";
            this.m_numberOfBarcodesBox.Text = result.ReadCount.ToString();
            this.m_decodeTimeBox.Text = string.Format("{0}.{1} s", (object)result.ExecutionTime.Seconds, (object)result.ExecutionTime.Milliseconds);
            this.m_barcodeBox.Text = result.ScannedMatrix;
            this.m_secureReadTB.Text = result.SecureCount.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.button1 = new Button();
            this.button2 = new Button();
            this.button3 = new Button();
            this.m_numberOfBarcodesBox = new TextBox();
            this.m_decodeTimeBox = new TextBox();
            this.m_barcodeBox = new TextBox();
            this.m_snapStatusBox = new TextBox();
            this.label1 = new Label();
            this.label2 = new Label();
            this.label3 = new Label();
            this.label4 = new Label();
            this.label6 = new Label();
            this.m_detectedErrorsTB = new TextBox();
            this.button4 = new Button();
            this.videoSourcePlayer = new VideoSourcePlayer();
            this.backgroundWorker1 = new BackgroundWorker();
            this.label5 = new Label();
            this.m_secureReadTB = new TextBox();
            this.SuspendLayout();
            this.button1.BackColor = Color.LightGray;
            this.button1.Location = new Point(31, 504);
            this.button1.Name = "button1";
            this.button1.Size = new Size(128, 61);
            this.button1.TabIndex = 1;
            this.button1.Text = "Snap And Decode";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new EventHandler(this.button4_Click);
            this.button2.BackColor = Color.LightGray;
            this.button2.Location = new Point(31, 591);
            this.button2.Name = "button2";
            this.button2.Size = new Size(128, 63);
            this.button2.TabIndex = 2;
            this.button2.Text = "Camera Properties";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new EventHandler(this.button2_Click);
            this.button3.BackColor = Color.GreenYellow;
            this.button3.Location = new Point(527, 603);
            this.button3.Name = "button3";
            this.button3.Size = new Size(144, 60);
            this.button3.TabIndex = 3;
            this.button3.Text = "Exit";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new EventHandler(this.button3_Click);
            this.m_numberOfBarcodesBox.BackColor = Color.White;
            this.m_numberOfBarcodesBox.Location = new Point(305, 504);
            this.m_numberOfBarcodesBox.Name = "m_numberOfBarcodesBox";
            this.m_numberOfBarcodesBox.ReadOnly = true;
            this.m_numberOfBarcodesBox.Size = new Size(100, 20);
            this.m_numberOfBarcodesBox.TabIndex = 4;
            this.m_decodeTimeBox.BackColor = Color.White;
            this.m_decodeTimeBox.Location = new Point(305, 534);
            this.m_decodeTimeBox.Name = "m_decodeTimeBox";
            this.m_decodeTimeBox.ReadOnly = true;
            this.m_decodeTimeBox.Size = new Size(100, 20);
            this.m_decodeTimeBox.TabIndex = 5;
            this.m_barcodeBox.BackColor = Color.White;
            this.m_barcodeBox.Location = new Point(305, 568);
            this.m_barcodeBox.Name = "m_barcodeBox";
            this.m_barcodeBox.ReadOnly = true;
            this.m_barcodeBox.Size = new Size(100, 20);
            this.m_barcodeBox.TabIndex = 6;
            this.m_snapStatusBox.BackColor = Color.White;
            this.m_snapStatusBox.Location = new Point(305, 603);
            this.m_snapStatusBox.Name = "m_snapStatusBox";
            this.m_snapStatusBox.ReadOnly = true;
            this.m_snapStatusBox.Size = new Size(100, 20);
            this.m_snapStatusBox.TabIndex = 7;
            this.label1.AutoSize = true;
            this.label1.Location = new Point(172, 507);
            this.label1.Name = "label1";
            this.label1.Size = new Size(89, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Number of Codes";
            this.label2.AutoSize = true;
            this.label2.Location = new Point(172, 537);
            this.label2.Name = "label2";
            this.label2.Size = new Size(30, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Time";
            this.label3.AutoSize = true;
            this.label3.Location = new Point(172, 575);
            this.label3.Name = "label3";
            this.label3.Size = new Size(47, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Barcode";
            this.label4.AutoSize = true;
            this.label4.Location = new Point(172, 610);
            this.label4.Name = "label4";
            this.label4.Size = new Size(65, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Snap Status";
            this.label6.AutoSize = true;
            this.label6.Location = new Point(509, 502);
            this.label6.Name = "label6";
            this.label6.Size = new Size(83, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Detected errors:";
            this.m_detectedErrorsTB.Location = new Point(512, 518);
            this.m_detectedErrorsTB.Name = "m_detectedErrorsTB";
            this.m_detectedErrorsTB.ReadOnly = true;
            this.m_detectedErrorsTB.Size = new Size(159, 20);
            this.m_detectedErrorsTB.TabIndex = 15;
            this.button4.BackColor = Color.LightGray;
            this.button4.Location = new Point(512, 544);
            this.button4.Name = "button4";
            this.button4.Size = new Size(126, 44);
            this.button4.TabIndex = 16;
            this.button4.Text = "Clear errors box";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new EventHandler(this.button4_Click_1);
            this.videoSourcePlayer.Location = new Point(31, 12);
            this.videoSourcePlayer.Name = "videoSourcePlayer";
            this.videoSourcePlayer.Size = new Size(640, 480);
            this.videoSourcePlayer.TabIndex = 0;
            this.videoSourcePlayer.Text = "videoSourcePlayer1";
            this.videoSourcePlayer.VideoSource = (IVideoSource)null;
            this.backgroundWorker1.DoWork += new DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            this.label5.AutoSize = true;
            this.label5.Location = new Point(172, 641);
            this.label5.Name = "label5";
            this.label5.Size = new Size(108, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "Secure Marker Count";
            this.m_secureReadTB.Location = new Point(305, 634);
            this.m_secureReadTB.Name = "m_secureReadTB";
            this.m_secureReadTB.Size = new Size(100, 20);
            this.m_secureReadTB.TabIndex = 18;
            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.DimGray;
            this.ClientSize = new Size(723, 670);
            this.Controls.Add((Control)this.m_secureReadTB);
            this.Controls.Add((Control)this.label5);
            this.Controls.Add((Control)this.button4);
            this.Controls.Add((Control)this.m_detectedErrorsTB);
            this.Controls.Add((Control)this.label6);
            this.Controls.Add((Control)this.label4);
            this.Controls.Add((Control)this.label3);
            this.Controls.Add((Control)this.label2);
            this.Controls.Add((Control)this.label1);
            this.Controls.Add((Control)this.m_snapStatusBox);
            this.Controls.Add((Control)this.m_barcodeBox);
            this.Controls.Add((Control)this.m_decodeTimeBox);
            this.Controls.Add((Control)this.m_numberOfBarcodesBox);
            this.Controls.Add((Control)this.button3);
            this.Controls.Add((Control)this.button2);
            this.Controls.Add((Control)this.button1);
            this.Controls.Add((Control)this.videoSourcePlayer);
            this.Name = nameof(MainForm);
            this.Text = "Redbox Camera Tuner";
            this.Load += new EventHandler(this.OnLoad);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private delegate void SetTextCallback(string msg);
    }
}
