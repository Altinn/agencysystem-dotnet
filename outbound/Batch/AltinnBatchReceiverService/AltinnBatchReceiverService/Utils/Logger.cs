using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace AltinnBatchReceiverService.Utils
{
    public static class Logger
    {

        /// <summary>
        /// The semaphore is used to logging.
        /// </summary>
        private static object semaphore = new object();

        /// <summary>
        /// This property is fetched from Web Config (key = appname).
        /// It is the name of the application as it will appear in the Event Log.
        /// </summary>
        private static string appName;

        /// <summary>
        /// Applog object for logging to Event Log
        /// </summary>
        private static System.Diagnostics.EventLog appLog = null;

        /// <summary>
        /// This property is fetched from Web Config (key = logdir), see Web.Config file.
        /// This is the file path where log files are saved.
        /// </summary>
        private static string logFilePath;

        /// <summary>
        /// This property is fetched from Web Config (key = log_thresholdMB).
        /// When the log file reaches the size defined (in MB), it will give the log file a new name with a time stamp and start filling up a new empty log file.
        /// </summary>
        private static long logFileThresholdMB;

        static Logger()
        {
            string curpath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

            // Event Logger
            appName = ConfigurationManager.AppSettings["appname"];
            if (appName == null)
                appName = "Unnamed";

            appLog = new System.Diagnostics.EventLog();
            appLog.Source = appName;

            // File logger
            string logDirPath = ConfigurationManager.AppSettings["logdir"];
            if (logDirPath != null)
            {
                if (!logDirPath.Contains(":"))
                    logDirPath = Path.Combine(curpath, logDirPath);
            }
            else
                logDirPath = Path.Combine(curpath, "Log");

            if (!Directory.Exists(logDirPath))
                Directory.CreateDirectory(logDirPath);

            logFilePath = Path.Combine(logDirPath, "log.txt");

            // Threshold
            string str = ConfigurationManager.AppSettings["log_thresholdMB"];
            long lstr;
            if (long.TryParse(str, out lstr))
                logFileThresholdMB = lstr;
            else
                logFileThresholdMB = 5;
        }


        /// <summary>
        /// Logs the data to file and optionally to Application event log.
        /// </summary>
        /// <param name="msg">The message to log</param>
        /// <param name="logToEvent">True if also log to event log, default false</param>
        /// <param name="logEntryType">The entry type of event log, default Error</param>
        /// <remarks>
        /// We have chosen here a simple log to file, but other logging strategies could be implemented, such as using NLog or logging to Application Event Log only.
        /// </remarks>
        public static void Log(string msg, bool logToEvent = false, EventLogEntryType logEntryType = EventLogEntryType.Error)
        {
            lock (semaphore)
            {
                if (File.Exists(logFilePath))
                {
                    FileInfo fi = new FileInfo(logFilePath);
                    if (fi.Length > logFileThresholdMB * 1024 * 1000)
                    {
                        DateTime dt = DateTime.Now;
                        File.Move(logFilePath, logFilePath.Replace("log.txt", string.Format("log - {0}-{1:00}-{2:00} {3:00}.{4:00}.{5:00}.{6:000}.txt", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond)));
                    }
                }
                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFilePath, true))
                    {
                        file.WriteLine($"{DateTime.Now}:{msg}");
                        file.WriteLine();
                    }
                    if (logToEvent)
                        appLog.WriteEntry(msg, logEntryType);
                }
                catch
                { }
            }
        }
    }
}