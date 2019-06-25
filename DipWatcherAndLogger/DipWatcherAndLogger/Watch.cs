using System;
using System.ServiceProcess;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Text;
using System.Diagnostics;

namespace DipWatcherAndLogger
{
    /// <summary>
    /// This class is used to watch folder's events.
    /// 1. OnCreated - this event fires when there is any file created.
    /// 2. OnRenamed - this event fires when there is any file renamed.
    /// 3. OnDeleted - this event fires whrn there is any file deleted.
    /// </summary>
    public static class Watch
    {
        private static string backupFolder = ConfigurationManager.AppSettings["WatchPathCopy"]; // Path for ingestion mirror
        private static string DeletedCopyFolder = ConfigurationManager.AppSettings["DeletedCopyOfDocument"]; //path for backup               

        private static Thread syncThread = null;
        private static System.Timers.Timer timer1;
        
        /// <summary>
        /// Executes Event
        /// </summary>
        private static void Execute(string watchPath)
        {
            try
            {
                //Debugger.Launch();
                timer1 = new System.Timers.Timer();                
                timer1.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["CycleTimeInMin"]) * 1000 * 60;;
                timer1.Enabled = true;
                timer1.Elapsed += (sender, e) => timer1_Elapsed(sender, e, watchPath);
                timer1.Start();
                

                PrepareFolderStucture(watchPath);
                if (Directory.Exists(watchPath))
                {
                    MakeEqualLevelFolderForSourceAndBackup(watchPath);
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = watchPath;
                    watcher.Filter = "*.*";
                    watcher.Created += OnCreated;
                    watcher.Deleted += OnDeleted;
                    watcher.Renamed += OnRenamed;
                    watcher.EnableRaisingEvents = true;
                }
                else
                {
                    if (Logger.captureApplicationLogs)
                        Logger.Write("Application: "+ DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Given ingestion folder ("+watchPath+") path is not found");
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: "+ DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Execute Function" + ex);
            }
        }
        static private void timer1_Elapsed(object sender, ElapsedEventArgs e, string watchPath)
        {
            ProcessedFolderBackup(watchPath);
            syncThread= new Thread(() => WatchFolder(watchPath));
            syncThread.Start();
        }
        static private void WatchFolder(string watchPath)
        {
            bool sendMail = false;
            StringBuilder data = new StringBuilder();
            foreach (var fileInfo in new DirectoryInfo(watchPath).GetFiles().OrderBy(x => x.LastWriteTime))
            {
                double totalExistsMinute = (System.DateTime.Now - fileInfo.LastWriteTime).TotalMinutes;
                double threshold = Convert.ToDouble(ConfigurationManager.AppSettings["WatchThresholdTimeInMin"]);
                if (fileInfo.Name != "Thumbs.db")
                {
                    if (totalExistsMinute > threshold)
                    {
                        data.AppendLine("INFO: "+DateTime.Now.ToString("MMddyyyy HH:mm:ss")+"   " +fileInfo.FullName + " exists more than " + threshold + " minute.");
                        sendMail = true;
                    }
                }
            }
            if (sendMail)
            {
                if (Logger.captureEventsLogs)
                    Logger.Write(data.ToString());
                SendMail(data.ToString());
            }

        }
        private static void  SendMail(string mailText)
        {
            if(Convert.ToBoolean(ConfigurationManager.AppSettings["SendMail"].Trim()) == true)
                Mailer.SendMail(mailText);
        }
        /// <summary>
        /// Method to launch multiple folder watcher
        /// </summary>
        public static void Run()
        {
            string[] watchPathArray = System.Configuration.ConfigurationManager.AppSettings["WatchPaths"].ToString().Split('|');
            foreach (string watchPath in watchPathArray)
            {
                Execute(watchPath);
            }
        }
        /// <summary>
        /// This event will fire if file name is renamed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                if (Path.GetExtension(e.FullPath).ToUpper() != ".TXT")
                {
                    CustomFileHandling.WaitForFile(e.FullPath);
                    string path = backupFolder + GetBackupFolderName(e.FullPath);                    
                    File.Move(path + e.OldName, path + e.Name);
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: "+DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Renamed Event \r\n" + ex.Message);
            }

        }
        /// <summary>
        /// Initially to make Source and SourceMirror on same level. Exclude txt files as it already being backed up by preprocessor exe.
        /// </summary>
        private static void MakeEqualLevelFolderForSourceAndBackup(string watchPath)
        {
            try
            {
                string[] allFiles = Directory.GetFiles(watchPath);
                foreach (string filename in allFiles)
                {
                    if (Path.GetExtension(filename).ToUpper() != ".TXT")
                    {
                        string pathToMove = backupFolder + GetBackupFolderName(watchPath);
                        File.Copy(filename, pathToMove + filename.Substring(filename.LastIndexOf("\\") + 1), true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: "+ DateTime.Now.ToString("MMddyyyy HH:mm:ss")+" Exception at MakeEqualLevelFolderForSourceAndBackup Function \r\n" + ex.Message);
            }

        }
        /// <summary>
        /// This function execute when create event happen in watcher folder 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {            
            try
            {
                if (Logger.captureEventsLogs)
                    Logger.Write("Event: "+e.FullPath + "\t IN \t" + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                if (Path.GetExtension(e.Name).ToUpper() != ".TXT")
                {
                    CustomFileHandling.WaitForFile(e.FullPath);
                    string pathToMove = backupFolder + GetBackupFolderName(e.FullPath);
                    File.Copy(e.FullPath, pathToMove + e.Name, true);
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: "+ DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Created Event \r\n" + ex.Message);
            }
        }
        /// <summary>
        /// This function executes when delete event happen in your watcher folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Logger.captureEventsLogs)
                    Logger.Write("Event: "+e.FullPath + "\t OUT \t" + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                
                if (Path.GetExtension(e.FullPath).ToUpper() != ".TXT")
                {
                    string pathToMove = DeletedCopyFolder + GetBackupFolderName(e.FullPath,true);
                    CustomFileHandling.CreateDirectoryIfDoesNotExist(pathToMove);
                    File.Move(backupFolder+ GetBackupFolderName(e.FullPath) + e.Name, pathToMove + AppendTimeStamp(e.Name));
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                    Logger.Write("Application: "+DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Deleted Event \r\n" + ex.Message);
            }
        }
        private static string AppendTimeStamp(this string fileName)
        {
            return string.Concat(
                Path.GetFileNameWithoutExtension(fileName),"_",
                DateTime.Now.ToString("MMddyyyyHHmmss"),
                Path.GetExtension(fileName)
                );
        }
        /// <summary>
        /// Create direcotries if not exist. Prepares structure as per the configuration file.
        /// </summary>
        private static void PrepareFolderStucture(string watchPath)
        {
            CustomFileHandling.CreateDirectoryIfDoesNotExist(backupFolder+ GetBackupFolderName(watchPath));
            CustomFileHandling.CreateDirectoryIfDoesNotExist(DeletedCopyFolder+ GetBackupFolderName(watchPath,true));
        }

        /// <summary>
        /// create folder for backup and mirror
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="isBackup"></param>
        /// <returns>returns folder name from source path</returns>
        private static string GetBackupFolderName(string sourcePath, bool isBackup=false)
        {
            if(isBackup)
                return sourcePath.Split('\\')[sourcePath.Split('\\').Length - 2] + "_Backup\\"+System.DateTime.Now.ToString("MMddyyyy")+"\\";
            else
                return sourcePath.Split('\\')[sourcePath.Split('\\').Length - 2] + "\\";
        }
        private static void ProcessedFolderBackup(string path)
        {
            try
            {
                string backupFolderName = ConfigurationManager.AppSettings["IndexFileBackupFolder"];
                string backupFolderPrefix = ConfigurationManager.AppSettings["IndexFileFolderPrefix"];
                string processedDirectory = path + "PROCESSED\\";
                if (Directory.Exists(processedDirectory))
                {
                    foreach (var fileInfo in new DirectoryInfo(processedDirectory).GetFiles().OrderBy(x => x.LastWriteTime))
                    {
                        string folderName = path + backupFolderName + "\\" + backupFolderPrefix + fileInfo.LastWriteTime.ToString("MM-yyyy") + "\\";
                        CustomFileHandling.CreateDirectoryIfDoesNotExist(folderName);
                        CustomFileHandling.WaitForFile(fileInfo.FullName);
                        File.Move(fileInfo.FullName, folderName + fileInfo.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                if(Logger.captureApplicationLogs)
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at ProcessedFolderBackup \r\n" + ex.Message);
            }

        }
    }
}
