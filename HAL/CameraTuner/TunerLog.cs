using Redbox.HAL.Component.Model;
using System;
using System.IO;

namespace Redbox.HAL.CameraTuner
{
    public sealed class TunerLog : ILogger
    {
        private bool Disposed;
        private TextWriter m_logFile;
        private const string LogFileName = "CameraTuner.log";
        private const string LogDirectory = "c:\\Program Files\\Redbox\\KioskLogs\\Service";

        public void Log(string message, Exception e)
        {
            this.WriteToLogFile(message);
            this.WriteToLogFile(e.Message);
            this.WriteToLogFile(e.StackTrace);
        }

        public void Log(string message, LogEntryType type) => this.WriteToLogFile(message);

        public void Log(string message, Exception e, LogEntryType type)
        {
            this.WriteToLogFile(message);
            this.WriteToLogFile(e.Message);
            this.WriteToLogFile(e.StackTrace);
        }

        public bool IsLevelEnabled(LogEntryType entryLogLevel) => true;

        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            try
            {
                if (this.m_logFile != null)
                {
                    this.m_logFile.Flush();
                    this.m_logFile.Close();
                }
            }
            catch
            {
            }
            GC.SuppressFinalize((object)this);
        }

        private TextWriter LogFile
        {
            get
            {
                if (this.m_logFile == null)
                    this.m_logFile = (TextWriter)new StreamWriter((Stream)File.Open(Path.Combine("c:\\Program Files\\Redbox\\KioskLogs\\Service", "CameraTuner.log"), FileMode.Append, FileAccess.Write, FileShare.Read));
                return this.m_logFile;
            }
        }

        private void WriteToLogFile(string msg)
        {
            try
            {
                this.LogFile.WriteLine(string.Format("{0}: {1}", (object)DateTime.Now, (object)msg));
                this.LogFile.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Write to log file caught an exception.");
            }
        }
    }
}
