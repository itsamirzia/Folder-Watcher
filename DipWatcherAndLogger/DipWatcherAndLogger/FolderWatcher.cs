using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;


namespace DipWatcherAndLogger
{
    public partial class FolderWatcher : ServiceBase
    {
        public FolderWatcher()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                //Debugger.Launch();
                if (Logger.captureApplicationLogs)
                    Logger.AddtoWritingQueue.Enqueue("Application: Window Service Started at " + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                 
                BackupSourceFile.Run();
                Archiving.Archive();
                NotificationServices.Notify();
                Logging.Enqueue();
                Logger.Write();
            }
            catch (Exception ex)
            {
                if(Logger.captureApplicationLogs)
                    Logger.AddtoWritingQueue.Enqueue("Application: Exception at OnStart Function" + ex);
            }
        }

        protected override void OnStop()
        {
            if(Logger.captureApplicationLogs)
                Logger.AddtoWritingQueue.Enqueue("Application: Window Service Stopped at " + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
        }
    }
}
