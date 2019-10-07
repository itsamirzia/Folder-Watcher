using System.IO;
using System;
using System.Configuration;
using System.Threading;

namespace DipWatcherAndLogger
{
    /// <summary>
    /// Class Logger is to write logs in a log File named as Logs_MMyyyy.txt
    /// Through this class we can write logs for all the events fired in folder watcher application.
    /// </summary>
    static class Logger
    {
        public static bool captureApplicationLogs = Convert.ToBoolean(ConfigurationManager.AppSettings["ApplicationLogs"].ToString().Trim());
        public static bool captureEventsLogs = Convert.ToBoolean(ConfigurationManager.AppSettings["EventsLogs"].ToString().Trim());
        static ReaderWriterLock locker = new ReaderWriterLock();
        //private static object locker = new Object();
        /// <summary>
        /// Write logs into the log file
        /// </summary>
        /// <param name="textLine"></param>
        public static void Write(string textLine)
        {
            try
            {
                string logFile = CustomFileHandling.GetOrCreateLogFile(System.Configuration.ConfigurationManager.AppSettings["LogsFolder"].ToString());



                if (logFile != string.Empty)
                {
                    //CustomFileHandling.WaitForFile(logFile);
                    using (StreamWriter sw = new StreamWriter(logFile, true))
                    {
                        locker.AcquireWriterLock(int.MaxValue);
                        sw.WriteLine(textLine);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if(locker.IsWriterLockHeld)
                    locker.ReleaseWriterLock();
            }
        }
    }
}
