using System;
using System.IO;

namespace DipWatcherAndLogger
{
    /// <summary>
    /// This class is used to handle file operations
    /// </summary>
    public static class CustomFileHandling
    {
        /// <summary>
        /// Create Directory if not exist
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>boolean</returns>
        public static bool CreateDirectoryIfDoesNotExist(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        /// <summary>
        /// Create File if not exist
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns>boolean</returns>
        public static bool CreateFileIfDoesNotExist(string fullFilePath)
        {
            try
            {
                if (!File.Exists(fullFilePath))
                {
                    FileStream fs = File.Create(fullFilePath);
                    fs.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if file is ready to read
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception )
            {
                return false;
            }
        }
        /// <summary>
        /// Wait for the file getting ready.
        /// </summary>
        /// <param name="filename"></param>
        public static void WaitForFile(string filename)
        {
            //This will lock the execution until the file is ready
            while (!IsFileReady(filename)) { }
        }
        /// <summary>
        /// Create log file one per day - file name Example - Logs_MMyyyy.txt (Logs_062019.txt)
        /// </summary>
        /// <param name="path"></param>
        /// <returns>returns full path of created file name</returns>
        public static string GetOrCreateLogFile(string path)
        {
            try
            {
                CreateDirectoryIfDoesNotExist(path);
                string logFileName = path + "Logs_" + System.DateTime.Now.ToString("MMyyyy") + ".txt";
                CreateFileIfDoesNotExist(logFileName);
                return logFileName;
            }
            catch
            {
                return null;
            }

        }
    }
}
