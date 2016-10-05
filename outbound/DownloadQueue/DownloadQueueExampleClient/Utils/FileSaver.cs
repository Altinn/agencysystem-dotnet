using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DownloadQueueExampleClient.Utils
{

    /// <summary>
    /// A generic file saver using the app config inbox and storing files (string or byte[]) relative to the inbox.
    /// </summary>
    public static class FileSaver
    {

        /// <summary>
        /// This property is fetched from Web Config (key = inbox), see Web.Config file.
        /// This is where the messages will be saved.
        /// </summary>
        private static string inboxDirPath = null;


        /// <summary>
        /// Saves content (text or byte[]) to a file relative to the inbox app configuration.
        /// </summary>
        /// <param name="file">The file content to be saved. This must be either a string or byte[]</param>
        /// <param name="folder">The folder where file will be saved as a subfolder to inbox. If null, the file is saved on inbox folder</param>
        /// <param name="filename">The name of the file with or without extension</param>
        /// <param name="extension">An optional extension</param>
        public static void SaveFile(object file, string folder, string filename, string extension = null)
        {
            GetConfiguration();
            string folderPath;

            if (!string.IsNullOrEmpty(folder))
            {
                folderPath = Path.Combine(inboxDirPath, folder);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
            else
                folderPath = inboxDirPath;

            string filePath = Path.Combine(folderPath, filename);

            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith("."))
                    filePath += ".";
                filePath += extension;
            }

            if (file is byte[])
                File.WriteAllBytes(filePath, (byte[])file);
            else if (file is string)
                File.WriteAllText(filePath, (string)file);
        }


        private static void GetConfiguration()
        {
            if (inboxDirPath != null)
                return;
            try
            {
                inboxDirPath = ConfigurationManager.AppSettings["inbox"];
            }
            catch
            {
                inboxDirPath = "/inbox";
            }

            if (!inboxDirPath.Contains(":"))
            {
                string curpath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                inboxDirPath = Path.Combine(curpath, inboxDirPath);
            }

            if (!Directory.Exists(inboxDirPath))
                Directory.CreateDirectory(inboxDirPath);
        }
    }
}
