using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace DipWatcherAndLogger
{
    /// <summary>
    /// This class archived processed folder index file and
    /// files present in backup folder. Tenure is set in configuration.
    /// </summary>
    public static class Archiving
    {
        private static string backupFolderName = ConfigurationManager.AppSettings["IndexFileBackupFolder"];
        static string backupFolderPrefix = ConfigurationManager.AppSettings["IndexFileFolderPrefix"];
        private static System.Timers.Timer timer1;
        private static Thread processedThread = null;
        private static Thread backupFolderThread = null;

        /// <summary>
        /// Archive Data
        /// </summary>
        public static void Archive()
        {
            ProcessedFolder();
            BackupFolder();
            timer1 = new System.Timers.Timer();
            timer1.Interval = Convert.ToDouble(ConfigurationManager.AppSettings["ArchivingCycleTimeInMinute"]) * 60 * 1000; // 86400000; // (Converting milliseconds into one day 24(hours) * 60(Minutes) * 60(seconds) * 1000(Milliseconds) = 86400000)
            timer1.Enabled = true;
            timer1.Elapsed += (sender, e) => timer1_Elapsed(sender, e);
            timer1.Start();
        }        
        

        /// <summary>
        /// Timer for archiving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            processedThread = new Thread(() => ProcessedFolder());
            processedThread.Start();

            backupFolderThread = new Thread(() => BackupFolder());
            backupFolderThread.Start();
        }
        

        /// <summary>
        /// Archiving processed folder.
        /// </summary>
        private static void ProcessedFolder()
        {
            string[] watchPathArray = System.Configuration.ConfigurationManager.AppSettings["WatchPaths"].ToString().Split('|');
            foreach (string watchPath in watchPathArray)
            {
                string processedDirectory = watchPath + "PROCESSED\\";
                if (Directory.Exists(processedDirectory))
                {
                    foreach (var fileInfo in new DirectoryInfo(processedDirectory).GetFiles().OrderBy(x => x.LastWriteTime))
                    {
                        int totalNumberOfDays = (System.DateTime.Now - fileInfo.LastWriteTime).Days;
                        if (totalNumberOfDays > Convert.ToInt32(ConfigurationManager.AppSettings["NumberOfDaysToKeepProcessedFile"].ToString().Trim()))
                        {
                            try
                            {
                                string folderName = watchPath + backupFolderName + "\\" + backupFolderPrefix + fileInfo.LastWriteTime.ToString("MM-yyyy") + "\\";
                                CustomFileHandling.CreateDirectoryIfDoesNotExist(folderName);
                                CustomFileHandling.WaitForFile(fileInfo.FullName);
                                File.Move(fileInfo.FullName, folderName + fileInfo.Name);
                            }
                            catch (Exception ex)
                            {
                                if (Logger.captureApplicationLogs)
                                {
                                    Logger.AddtoWritingQueue.Enqueue("Application: " + DateTime.Now.ToString("MMddyyyy HH:mm:ss") + " Exception at archiving processed folder \r\n" + ex.Message);
                                    
                                }
                            }
                        }
                    }
                }
            }            
        }

        /// <summary>
        /// Archiving backup folder.
        /// </summary>
        private static void BackupFolder()
        {
            string DeletedCopyOfTheDocumentArray = ConfigurationManager.AppSettings["DeletedCopyOfDocument"];
            string[] BackupPaths = Directory.GetDirectories(DeletedCopyOfTheDocumentArray);
            foreach (string backupPath in BackupPaths)
            {
                foreach (var backupDirectory in Directory.GetDirectories(backupPath))
                {
                    string directoryName = new DirectoryInfo(backupDirectory).Name;
                    if (IsAbleToParseInDateTime(directoryName))
                    {
                        int TotalDaysDifference = (System.DateTime.Now - DateTime.ParseExact(directoryName, "MMddyyyy", System.Globalization.CultureInfo.InvariantCulture)).Days;

                        if (TotalDaysDifference > Convert.ToInt64(System.Configuration.ConfigurationManager.AppSettings["NumberOfDaysToKeepBackup"]))
                        {
                            try
                            {
                                Directory.Delete(backupDirectory, true);
                            }
                            catch { }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Check folder name is in format of date time.
        /// </summary>
        /// <param name="OurTimeString"></param>
        /// <returns></returns>
        private static bool IsAbleToParseInDateTime(string OurTimeString)
        {
            try
            {
                var myConvertedDateTime = DateTime.ParseExact(OurTimeString, "MMddyyyy", System.Globalization.CultureInfo.InvariantCulture);
                return true;

            }
            catch
            {
                return false;
            }
        }
    }
}
