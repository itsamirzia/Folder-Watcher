using System.IO;
using System;
using System.Configuration;
using System.Threading;
using System.Collections;
using System.Timers;

namespace DipWatcherAndLogger
{
    /// <summary>
    /// Class Logger is to write logs in a log File named as Logs_MMyyyy.txt
    /// Through this class we can write logs for all the events fired in folder watcher application.
    /// </summary>
    static class Logger
    {
        public static bool captureApplicationLogs = true; // Convert.ToBoolean(ConfigurationManager.AppSettings["ApplicationLogs"].ToString().Trim());
        public static bool captureEventsLogs = true;// Convert.ToBoolean(ConfigurationManager.AppSettings["EventsLogs"].ToString().Trim());
        public static Queue AddtoWritingQueue = new Queue();
        
        private static System.Timers.Timer timer1;
        //private static object locker = new Object();
        /// <summary>
        /// Write logs into the log file
        /// </summary>
        /// <param name="textLine"></param>
        public static void Write()
        {
            timer1 = new System.Timers.Timer();
            timer1.Interval =  Convert.ToDouble(System.Configuration.ConfigurationManager.AppSettings["WritingLogsTimerInterval"]);
            timer1.Enabled = true;
            timer1.Elapsed += (sender, e) => timer1_Elapsed(sender, e);
            timer1.Start();
        }
        /// <summary>
        /// Timer for dequeue service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            DequeueService();
        }

        /// <summary>
        /// Write logs using dequeue service
        /// </summary>
        public static void DequeueService()
        {
            string logFile = CustomFileHandling.GetOrCreateLogFile(System.Configuration.ConfigurationManager.AppSettings["LogsFolder"].ToString());

            try
            {
                if (AddtoWritingQueue.Count > 0)
                {
                    foreach (object textLine in AddtoWritingQueue)
                    {
                        if (logFile != string.Empty)
                        {
                            using (StreamWriter sw = new StreamWriter(logFile, true))
                            {
                                sw.WriteLine(AddtoWritingQueue.Dequeue().ToString());
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {

            }
        }
    }
}
