using System.IO;
using System;
using System.Configuration;

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
        //private static object locker = new Object();
        /// <summary>
        /// Write logs into the log file
        /// </summary>
        /// <param name="textLine"></param>
        public static void Write(string textLine)
        {
            string logFile = CustomFileHandling.GetOrCreateLogFile(System.Configuration.ConfigurationManager.AppSettings["LogsFolder"].ToString());
            using (StreamWriter sw = new StreamWriter(logFile, true))
            {
                sw.WriteLine(textLine);
            }
        }
    }
}
