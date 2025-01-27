using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Services;
using Redbox.HAL.Component.Model.Threading;
using Redbox.HAL.DataMatrix.Framework;
using System;
using System.Windows.Forms;

namespace Redbox.HAL.CameraTuner
{
    public class Program : IDisposable
    {
        private bool Disposed;
        private readonly NamedLock InstanceLock;
        private readonly Guid AppGuid = new Guid("96C0A029-9835-41B5-8F45-53E9AD339B6D");

        public void Dispose() => this.DisposeInner(true);

        ~Program() => this.DisposeInner(false);

        public void Run(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServiceLocator.Instance.AddService(typeof(IRuntimeService), (object)new RuntimeService());
            ServiceLocator.Instance.AddService(typeof(IEncryptionService), (object)new TripleDesEncryptionService());
            TunerLog tunerLog = new TunerLog();
            ServiceLocator.Instance.AddService(typeof(ILogger), (object)tunerLog);
            BarcodeConfiguration.MakeNewInstance2();
            BarcodeReaderFactory instance = new BarcodeReaderFactory();
            ServiceLocator.Instance.AddService(typeof(IBarcodeReaderFactory), (object)instance);
            instance.Initialize(new ErrorList());
            Application.Run((Form)new MainForm(tunerLog));
        }

        [STAThread]
        public static void Main(string[] args)
        {
            using (Program program = new Program())
            {
                if (!program.InstanceLock.IsOwned)
                    Environment.Exit(10);
                program.Run(args);
            }
        }

        public Program() => this.InstanceLock = new NamedLock(this.AppGuid.ToString());

        private void DisposeInner(bool fromDispose)
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            if (!fromDispose)
                return;
            this.InstanceLock.Dispose();
        }
    }
}
