using System;
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

                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: Window Service Started at " + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));

                Watch.Run();
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: Exception at OnStart Function" + ex);
            }
        }

       



        protected override void OnStop()
        {
            if (Logger.captureApplicationLogs)
                Logger.Write("Application: Window Service Stopped at "+ System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
        }
    }
}
