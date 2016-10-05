using System;
using System.IO;
using System.Xml;

namespace AltinnSimulator
{

    /// <summary>
    /// This Console Application simulates Altinn sending messages to the Online Batch Receiver Soap service.
    /// It reads messages from a file and sends them to the service. The service Endpoint is configured in the App.Config file.
    /// </summary>
    class Program
    {
        // Holding the command line arguments
        private static Arguments _arguments;

        // Counters for different status return

        private static int _ok_count;
        private static int _failed_count;
        private static int _failed_do_not_retry_count;

        private static string helptext =
@"
folder [-max count] [-maxerr count] folderPath
   Loads all or a max number of messages from the specified folderPath 
   and sends them to the URL defined in the configuration.
";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns>-1: fatal error, 1 there are errors, 0 OK</returns>
        static int Main(string[] args)
        {
            _arguments = new Arguments(args);


            // HELP
            if (_arguments.Command == null || _arguments.Command.Equals("help") || _arguments.Command.Equals("?"))
            {
                Console.WriteLine(helptext);
            }

            // FOLDER
            // folder [-max count] [-maxerr count] folderPath
            else if (_arguments.Command.Equals("folder"))
            {
                // Loads data from a folder - the arguments Source is the name of the folder
                if (string.IsNullOrEmpty(_arguments.Source))
                {
                    return Error("Missing the Folder-Path");
                }

                if (!Directory.Exists(_arguments.Source))
                {
                    return Error("The Folder-Path does not exists");
                }

                int max = int.MaxValue;
                string maxarg = _arguments["max"];
                if (!string.IsNullOrEmpty(maxarg))
                {
                    int m;
                    if (int.TryParse(maxarg, out m))
                        max = m;
                    else
                        return Error("-max value is not a valid number value");
                }

                int maxerr = int.MaxValue;
                string maxerrarg = _arguments["maxerr"];
                if (!string.IsNullOrEmpty(maxerrarg))
                {
                    int m;
                    if (int.TryParse(maxerrarg, out m))
                        maxerr = m;
                    else
                        return Error("-maxerr value is not a valid number value");
                }

                // Process all the files in the given folder
                string[] files = Directory.GetFiles(_arguments.Source, "*.xml");
                int successCount = 0;
                int errorCount = 0;
                foreach (string file in files)
                {
                    if (successCount == max)
                        break;
                    try
                    {
                        SendXml(file);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ErrorMessage(ex));
                        errorCount++;
                    }
                    if (errorCount >= maxerr)
                        break;
                }
                Console.WriteLine("Transmission summary:");
                Console.WriteLine("\tSuccess: {0}", successCount);
                Console.WriteLine("\tErrors:  {0}", errorCount);
                WriteRetResult();
                Console.ReadLine();
                return errorCount > 0 ? 1 : 0;
            }

            else
            {
                Console.WriteLine("Missing Command");
            }

            Console.ReadLine();

            return 0;
        }


        static private int Error(string msg)
        {
            Console.WriteLine(msg);
            Console.ReadLine();
            return -1;
        }


        private static string ErrorMessage(Exception ex)
        {
            return string.Format("EXCEPTION: {0}, Source: {1}", ex.Message, ex.Source);
        }


        private static void WriteRetResult()
        {
            Console.WriteLine("Return result summary:");
            Console.WriteLine("\tOK: {0}", _ok_count);
            Console.WriteLine("\tFAILED: {0}", _failed_count);
            Console.WriteLine("\tFAILED_DO_NOT_RETRY: {0}", _failed_do_not_retry_count);
        }

        static void SendXml(string filename)
        {
            string username = null;
            string password = null;
            string recref = null;
            long seqno = 0;
            string batch = null;
            byte[] attachments = null;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(File.ReadAllText(filename));

            var enumerator = xmldoc.GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlNode node = (XmlNode)enumerator.Current;
                if (node.LocalName == "ReceiveOnlineBatchExternalAttachment")
                {
                    var enumerator2 = node.GetEnumerator();
                    while (enumerator2.MoveNext())
                    {
                        XmlNode node2 = (XmlNode)enumerator2.Current;
                        if (node2.LocalName == "username")
                            username = node2.InnerText;
                        else if (node2.LocalName == "passwd")
                            password = node2.InnerText;
                        else if (node2.LocalName == "receiversReference")
                            recref = node2.InnerText;
                        else if (node2.LocalName == "sequenceNumber")
                        {
                            string seqnostr = node2.InnerText;
                            long sno;
                            long.TryParse(seqnostr, out sno);
                            seqno = sno;
                        }
                        else if (node2.LocalName == "batch")
                            batch = node2.InnerText;
                        else if (node2.LocalName == "attachments")
                        {
                            attachments = System.Convert.FromBase64String(node2.InnerText);
                        }

                    }

                }

            }

            using (var cli = new OnlineBatchReceiver.OnlineBatchReceiverSoapSoapClient())
            {
                string resp = cli.ReceiveOnlineBatchExternalAttachment(username, password, recref, seqno, batch, attachments);
                cli.Close();
                if (resp.Contains("FAILED_DO_NOT_RETRY"))
                    _failed_do_not_retry_count++;
                else if (resp.Contains("FAILED"))
                    _failed_count++;
                else
                    _ok_count++;
            }

        }

     }

}
