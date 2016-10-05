using System.Linq;
using DownloadQueueExampleClient.DownloadQueueService;
using System;
using System.IO;
using System.Configuration;
using System.Reflection;

namespace DownloadQueueExampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string username = ConfigurationManager.AppSettings["username"];
            string password = ConfigurationManager.AppSettings["password"];
            string servicecode = ConfigurationManager.AppSettings["servicecode"];
            string maxdownloadstr = ConfigurationManager.AppSettings["maxdownload"];
            string langidstr = ConfigurationManager.AppSettings["languageid"];

            int maxdownload;
            if (!int.TryParse(maxdownloadstr, out maxdownload))
                maxdownload = 10;

            int langid;
            if (!int.TryParse(langidstr, out langid))
                maxdownload = 10;

            Console.WriteLine($"{DateTime.Now} - DownloadQueueClient started...");

            var downloadQueueProcessor = new DownloadQueueHandler(username, password, servicecode);
            downloadQueueProcessor.Execute(maxdownload, langid);

            Console.WriteLine($"{DateTime.Now} - DownloadQueueClient completed!");
        }
    }
}
