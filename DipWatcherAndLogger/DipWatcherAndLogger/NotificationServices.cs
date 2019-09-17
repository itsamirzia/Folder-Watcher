using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace DipWatcherAndLogger
{
    public static class NotificationServices
    {
        private static Thread syncThread = null;
        private static System.Timers.Timer timer1;
        public static void Notify()
        {
            timer1 = new System.Timers.Timer();
            timer1.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["CycleTimeInMin"]) * 1000 * 60; ;
            timer1.Enabled = true;
            timer1.Elapsed += (sender, e) => timer1_Elapsed(sender, e);
            timer1.Start();
        }

        private static void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            syncThread = new Thread(() => SendNotification());
            syncThread.Start();
        }
        static private void SendNotification()
        {
            try
            {
                string[] watchPathArray = System.Configuration.ConfigurationManager.AppSettings["NotificationPaths"].ToString().Split('|');
                double threshold = Convert.ToDouble(ConfigurationManager.AppSettings["WatchThresholdTimeInMin"]);

                string message = @"<html>
                <head>
                <style>
                p { background - color: black; }
                p { color: while; }
                h2 { background - color: black; }
                h2 { color: while; }
                </style>
                </head>
                <body><h2>This is an auto-generated email</h2><br /><br />Hi Team,<br /><br />";
                bool dataFound = false;
                foreach (string watchPath in watchPathArray)
                {

                    int counter = 0;
                    foreach (var fileInfo in new DirectoryInfo(watchPath).GetFiles().OrderBy(x => x.LastWriteTime))
                    {
                        double totalExistsMinute = (System.DateTime.Now - fileInfo.LastWriteTime).TotalMinutes;
                        if (fileInfo.Name != "Thumbs.db")
                        {
                            if (totalExistsMinute > threshold)
                            {
                                counter++;
                                dataFound = true;
                            }
                        }
                    }
                    if (counter > 0)
                    {

                        if (counter > 1)
                            message += counter + " documents are sitting for more than " + threshold + " minutes on the below path<br /><b>" + watchPath + "</b><br /><br />";
                        else
                            message += "1 document is sitting for more than " + threshold + " minutes on the below path<br /><b>" + watchPath + "</b><br /><br />";
                    }
                }
                if (dataFound)
                {
                    message += "Please take immediate action.<br /><br />-FolderWatcher Notification</body></html>";

                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["WriteNotification"].Trim()) == true)
                    {
                        Logger.Write("INFO: Email notification sent at " + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                        Thread.Sleep(100);
                    }

                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["EmailNotification"].Trim()) == true)
                        Mailer.SendMail(message);
                }
                
        
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Error : occured in Notification service -" + ex.Message);
                    Thread.Sleep(100);
                }
            }
        }
       
    }
}
