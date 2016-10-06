using System.IO;
using System.Xml.Serialization;

namespace OnlineBatchReceiver.Utils
{
    /// <summary>
    /// Utils for handling File
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Save XMLfile to Disk
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="filepath">the file path</param>
        /// <param name="folderName">the folder name</param>
        /// <param name="filename">the file name</param>
        /// <param name="serializer">the XMLSerializer</param>
        /// <param name="result"></param>
        public static void SaveXmlFileToDisk<T>(string filepath, string folderName, string filename, XmlSerializer serializer, T result)
        {
            var pathAndFile = GetPathWithFilename(filepath, folderName, filename, ".xml");
            var file = File.Create(pathAndFile);

            serializer.Serialize(file, result);
            file.Close();
        }

        /// <summary>
        /// Use file path and folder name to get path
        /// </summary>
        /// <param name="filepath">the file path</param>
        /// <param name="folderName">the folder name</param>
        /// <param name="filename">the file name</param>
        /// <param name="fileextention">the file extention</param>
        /// <returns>path as string</returns>
        private static string GetPathWithFilename(string filepath, string folderName, string filename, string fileextention)
        {
            var path = Path.Combine(filepath + "\\" + folderName + "\\");
            Directory.CreateDirectory(path);

            var pathAndFile = Path.Combine(path, SafeFileName(filename + fileextention));
            return pathAndFile;
        }

        /// <summary>
        /// Make path safe by removing unwanted characters
        /// </summary>
        /// <param name="path">the path</param>
        /// <returns>"trimmed" path as string</returns>
        private static string SafeFileName(string path)
        {
            return path.Replace("\\", "").Replace("/", "").Replace("\"", "").Replace("*", "").Replace(":", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "");
        }

        /// <summary>
        /// Check if files already exists
        /// </summary>
        /// <param name="filepath">the file path</param>
        /// <param name="folderName">the folder name</param>
        /// <param name="username">the username</param>
        /// <param name="receiversReference">receivers reference</param>
        /// <param name="sequenceNumber">the sequence number</param>
        /// <returns>true or false</returns>
        public static bool AlreadyExists(string filepath, string folderName, string username,
            string receiversReference, long sequenceNumber)
        {
            var path = Path.Combine(filepath + "\\" + folderName + "\\");
            var dir = new DirectoryInfo(path);
            var filesInDir = dir.GetFiles("*" + receiversReference + "*.*");

            return filesInDir.Length > 0;
        }

        /// <summary>
        /// Save attatcments as zip
        /// </summary>
        /// <param name="filepath">the file path</param>
        /// <param name="folderName">the folder name</param>
        /// <param name="filename">the file name</param>
        /// <param name="attachments">attacments as byte array</param>
        public static void SaveAttatchmentsAsZip(string filepath, string folderName, string filename, byte[] attachments)
        {
            if (attachments == null || attachments.Length == 0) return;

            File.WriteAllBytes(GetPathWithFilename(filepath, folderName, filename, ".zip"), attachments);
        }
    }
}