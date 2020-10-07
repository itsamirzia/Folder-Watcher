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
                PrepareFolderStucture(watchPath);
                if (Directory.Exists(watchPath))
                {
                    MakeEqualLevelFolderForSourceAndBackup(watchPath);
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = watchPath;
                    watcher.Filter = "*.*";
                    watcher.InternalBufferSize = Convert.ToInt32(ConfigurationManager.AppSettings["InternalBufferSize"]);
                    watcher.Created += new FileSystemEventHandler(OnCreated);
                    watcher.Error += new ErrorEventHandler(LogBufferError);
                    watcher.EnableRaisingEvents = true;
                }
                else
                {
                    if (Logger.captureApplicationLogs)
                    {
                        Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Given ingestion folder (" + watchPath + ") path is not found");                        
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Execute Function -" + ex);
                }
            }
        }

        private static void LogBufferError(object sender, ErrorEventArgs e)
        {
            if (Logger.captureApplicationLogs)
            {
                Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " - " + e.GetException());
            }
        }

        /// <summary>
        /// Method to launch multiple folder watcher
        /// </summary>
        public static void Run()
        {
            if (ConfigurationManager.AppSettings["backup"].ToString().ToUpper() == "TRUE")
            {
                string[] watchPathArray = System.Configuration.ConfigurationManager.AppSettings["WatchPaths"].ToString().Split('|');
                foreach (string watchPath in watchPathArray)
                {
                    Execute(watchPath);
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
                    if (Path.GetExtension(filename).ToUpper() != ".TXT" && Path.GetExtension(filename).ToUpper() != ".DIP")
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
                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at MakeEqualLevelFolderForSourceAndBackup Function \r\n" + ex.Message);
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
                Thread thread = new Thread(() => CreateACopyOfDocument(e.FullPath, e.Name));
                thread.Start();
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Created Event \r\n" + ex.Message);
                }
            }
        }
        private static void CopyAFileThread(string from, string to)
        {
            try
            {
                File.Copy(from, to, true);
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Problem in copying file+ "+from+"  \r\n" + ex.Message);
                }
            }
        }
        private static void CreateACopyOfDocument(string fullPath, string filename, string oldName="", bool isRenamed = false)
        {
            try
            {
                if (Path.GetExtension(filename).ToUpper() != ".TXT" && Path.GetExtension(filename).ToUpper() != ".DIP")
                {
                    CustomFileHandling.WaitForFile(fullPath);
                    string pathToMove = DeletedCopyFolder + GetBackupFolderName(fullPath, true);
                    CustomFileHandling.CreateDirectoryIfDoesNotExist(pathToMove);

                        if (isRenamed)
                        {
                                File.Copy(fullPath, pathToMove + "Renamed(" + Path.GetFileNameWithoutExtension(oldName) + ") to_" + filename);                            
                        }
                        else
                        {    
                                File.Copy(fullPath, pathToMove + AppendTimeStamp(filename), true);
                        }
                    }
                
            }
            catch (Exception ex)
            {
                if (Logger.captureApplicationLogs)
                {
                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at Created Event \r\n" + ex.Message);
                }
            }
        }

        
        /// <summary>
        /// Append time stamp to a filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
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
            //Debugger.Launch();
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
