using Redbox.HAL.Component.Model;
using System;
using System.IO;

namespace Redbox.HAL.CameraTuner
{
    internal sealed class ScanResult
    {
        private static string SecretToken = ServiceLocator.Instance.GetService<IEncryptionService>().DecryptFromBase64("2PiCFxFlrO8=");

        internal int ReadCount { get; private set; }

        internal string ScannedMatrix { get; private set; }

        internal TimeSpan ExecutionTime { get; private set; }

        internal bool SnapOk { get; private set; }

        internal int SecureCount { get; private set; }

        internal static ScanResult ErrorResult() => new ScanResult();

        internal static ScanResult Scan(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                return ScanResult.ErrorResult();
            IScanResult scanResult1 = ServiceLocator.Instance.GetService<IBarcodeReaderFactory>().GetConfiguredReader().Scan(file);
            ScanResult scanResult2 = new ScanResult()
            {
                SnapOk = true
            };
            if (scanResult1.DecodeResults.Count > 2 || scanResult1.DecodeResults.Count == 0)
            {
                LogHelper.Instance.Log("[ScanResult] There are {0} matrix entries.", (object)scanResult1.DecodeResults.Count);
                return scanResult2;
            }
            IDecodeResult decodeResult1 = (IDecodeResult)null;
            foreach (IDecodeResult decodeResult2 in scanResult1.DecodeResults)
            {
                if (!decodeResult2.Matrix.Equals(ScanResult.SecretToken, StringComparison.CurrentCultureIgnoreCase))
                    decodeResult1 = decodeResult2;
                else
                    scanResult2.SecureCount = decodeResult2.Count;
            }
            if (decodeResult1 != null)
            {
                scanResult2.ScannedMatrix = decodeResult1.Matrix;
                scanResult2.ReadCount = decodeResult1.Count > 4 ? 4 : decodeResult1.Count;
                scanResult2.ExecutionTime = scanResult1.ExecutionTime;
            }
            return scanResult2;
        }

        private ScanResult()
        {
            this.ReadCount = 0;
            this.ScannedMatrix = "UNKNOWN";
            this.ExecutionTime = new TimeSpan();
            this.SecureCount = 0;
        }
    }
}
