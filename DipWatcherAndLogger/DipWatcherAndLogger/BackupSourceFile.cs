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
    public static class BackupSourceFile
    {
        private static string DeletedCopyFolder = ConfigurationManager.AppSettings["DeletedCopyOfDocument"]; //path for backup               
        
        /// <summary>
        /// Executes Event
        /// </summary>
        private static void Execute(string watchPath)
        {
            try
            {
                //Debugger.Launch();                

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
                    {
                        Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Given ingestion folder (" + watchPath + ") path is not found");
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Execute Function -" + ex);
                    Thread.Sleep(50);
                }
            }
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
                    string path = DeletedCopyFolder + GetBackupFolderName(e.FullPath, true);                    
                    File.Move(e.FullPath, path + "Renamed("+e.OldName+")_"+AppendTimeStamp(e.Name));
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Renamed Event \r\n" + ex.Message);
                    Thread.Sleep(50);
                }
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
                        string pathToMove = DeletedCopyFolder + GetBackupFolderName(watchPath, true);
                        File.Copy(filename, pathToMove + AppendTimeStamp(filename.Substring(filename.LastIndexOf("\\") + 1)), true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at MakeEqualLevelFolderForSourceAndBackup Function \r\n" + ex.Message);
                    Thread.Sleep(50);
                }
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
                {
                    Logger.Write("Event: " + e.FullPath + "\t IN \t" + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                    Thread.Sleep(50);
                }
                if (Path.GetExtension(e.Name).ToUpper() != ".TXT")
                {
                    CustomFileHandling.WaitForFile(e.FullPath);
                    string pathToMove = DeletedCopyFolder + GetBackupFolderName(e.FullPath, true);
                    File.Copy(e.FullPath, pathToMove + AppendTimeStamp(e.Name), true);
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Created Event \r\n" + ex.Message);
                    Thread.Sleep(50);
                }
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
                {
                    Logger.Write("Event: " + e.FullPath + "\t OUT \t" + System.DateTime.Now.ToString("MMddyyyy HH:mm:ss"));
                    Thread.Sleep(50);
                }
                
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.Write("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Deleted Event \r\n" + ex.Message);
                    Thread.Sleep(50);
                }
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

    }
}
