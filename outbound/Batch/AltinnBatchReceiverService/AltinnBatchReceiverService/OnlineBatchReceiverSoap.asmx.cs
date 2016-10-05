using System;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Configuration;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Diagnostics;
using AltinnBatchReceiverService.Utils;
using AltinnBatchReceiverService.Resources;
using System.Reflection;
using System.Threading;

namespace AltinnBatchReceiverService
{

    /// <summary>
    /// Summary description for OnlineBatchReceiverSoap.
    /// </summary>
    /// <remarks>
    /// This is a sample implementation of Altinn Service Receive. It receives messages and perform these steps:
    /// 1) Authenticate with username and password (just a placeholder in this sample code)
    /// 2) Verifies the Xml Batch Data vs. the Batch schema XSD file
    /// 3) Receives the message, in this example code it is saved to file, but in real solution it may pushed to MSMQ, saved to DB, sent to web service (async) or otherwise processed.
    /// 
    /// This strategy is chosen on return:
    /// 
    /// FAILED:                 When the message was not persisted. The same principle could apply for messages not saed to DB, not sent to MSMQ, failure on sending to an internal REST endpoint etc.
    ///                         The message will be resent by Altinn after a configured (default 30 seconds) time.
    ///                         This means that the message may have been received, but we could persist it or pass it on internally.
    ///                         We also set FAILED when the data load (batch) do not verify vs. schema. After 3 retries, Altinn will stop sending messages and the issue with the Schema must be corrected.
    /// 
    /// FAILED_DO_NOT_RETRY:    When the message was a duplicate. This is detected when saved to file. When other implementations are chosed such as MSMQ there should be some mechanism to detect
    ///                         that a message has been received.
    /// 
    /// Regarding point 3: 
    /// It is important to handle the reception of the message in such a way that we quickly return the result to Altinn.
    /// This means that further processing should be asynchronous with the message reception.
    /// Further processing may be done by either pulling files from the Inbox directory (using directory monitoring), pulling from MSMQ, receive an Synch stream on a REST service, pussing data from DB etc.
    /// 
    /// Regarding Schema validation strategy:
    /// We have chosen to validate the Batch schema only as this is within Altinn's responsibility.
    /// The question is: should the Request Schema also be verified (the actual data from end user).
    /// This should not be done here as part of receiving the data as it is not within Altinn's responsibility.
    /// However, we suggest that this is first step of the further (asynchronous) processing of the message.
    /// 
    /// We also validate the response schema. We suggest this is done only during development and testing, and be removed before production setting.
    /// 
    /// Regarding Logging:
    /// We have chosen a very simple logging to file in this sample code, using a Semaphore to control concurrency. The configuration contain log file limit. When log file limit is reached, it is copied and
    /// a new empty log file is created. It also logs to the Application Event Log. The real service implementation may perfectly well use this approach, but may also choose other approaches such as:
    /// * Only logging to Event Log
    /// * Using NLog
    /// 
    /// Regarding Security: 
    /// This sample service implementation does not contain any transportation or policy security, this must be implemented by the service owner.
    /// 
    /// Regarding the actual Schema Data:
    /// It would be an idea to validate the incoming data vs. the schema. This Schema is known by the Service Owner. 
    /// 
    /// </remarks>
    public class OnlineBatchReceiverSoap : System.Web.Services.WebService, IOnlineBatchReceiverSoap
    {
        private const string FAILED_DO_NOT_RETRY = "FAILED_DO_NOT_RETRY";
        private const string FAILED = "FAILED";
        private const string OK = "OK";
 
        /// <summary>
        /// This property is fetched from Web Config (key = inbox), see Web.Config file.
        /// This is where the messages will be saved.
        /// </summary>
        private static string inboxDirPath;

        /// <summary>
        /// This property is fetched from Web Config (key = batch_schema_path).
        /// This is the batch schema XSD
        /// </summary>
        private static string schFilePath;

        /// <summary>
        /// This property is fetched from Web Config (key = response_schema_path).
        /// This is the response schema XSD. Used to verify the response data vs. Schema.
        /// </summary>
        private static string rschFilePath;

        /// <summary>
        /// This static constructor is called only once, first time a message is received.
        /// </summary>
        /// <remarks>
        /// Fetches configuration information and initiates the static properties.
        /// </remarks>
        static OnlineBatchReceiverSoap()
        {
            GetConfig();
            Logger.Log("Application Started");
        }


        /// <summary>
        /// The receive endpoint fo the service
        /// </summary>
        /// <returns>The response XML (OnlineBatchReceipt.xsd)</returns>
        public string ReceiveOnlineBatchExternalAttachment(string username, string passwd, string receiversReference, long sequenceNumber, string batch, byte[] attachments)
        {

            //1. Authenticate username + passw: if not authenticated: log, ignore message and return failed do not retry
            //   The Authenticate method must be implemented by Service owner.
            if (!Authenticate(username, passwd))
            {
                Logger.Log(string.Format("User not authenticated: {0}", username), true);
                return BuildResponse(FAILED_DO_NOT_RETRY);
            }


            //2. Verify batch vs. XSD (Schema verification) - if fails: log the error and return failed do not retry
            if (!VerifyBatchSchema(batch))
            {
                return BuildResponse(FAILED_DO_NOT_RETRY);
            }

            // We have chosen to capture all data within an object in order to serialize it, get hash code etc.
            // The service owner may for example choose just to keep the Batch and Attachments and pass them on for further asynchronous processing
            Payload payload = new Payload()
            {
                Username = username,
                Password = passwd,
                ReceiverReference = receiversReference,
                SequenceNumber = sequenceNumber,
                Batch = batch,
                Attachments = attachments
            };


            //3. Save or add to queue - if fail, tell to resend or if duplicate - tell to ignore
            int ret = ReceiveMessage(payload);
            if (ret == 1)
                return BuildResponse(FAILED);
            else if (ret == 2)
                return BuildResponse(FAILED_DO_NOT_RETRY);

            return BuildResponse(OK);

        }


        /// <summary>
        /// This builds a response object which will be serialized as XML, and verified
        /// </summary>
        /// <param name="respMessage">The response message</param>
        /// <returns>The serialized XML</returns>
        /// <remarks>
        /// respMessage must be one of:
        /// OK
        /// FAILED
        /// FAILED_DO_NOT_RETRY
        /// </remarks>
        private string BuildResponse(string respMessage)
        {
            var response = new Response
            {
                Result = new Result
                {
                    Code = respMessage
                }
            };

            var xmlResponse = Response.SerializeAsXml(response);
            SchemaValidator.VerifyVsSchema(xmlResponse, rschFilePath);
            return Response.SerializeAsXml(response);
        }


        /// <summary>
        /// A place holder where the service owner authenticates the message and return TRUE if authenticated.
        /// </summary>
        /// <param name="username">The username as received from Altinn</param>
        /// <param name="password">The password as received from Altinn</param>
        /// <returns>True if authenticated</returns>
        private bool Authenticate(string username, string password)
        {
            // TODO: Add authentication, log if authentication fails
            return true;
        }

        /// <summary>
        /// This will verify the Batch data vs. the batch schema.
        /// </summary>
        /// <param name="batchXML">Batch data (as XML)</param>
        /// <returns>True if verified</returns>
        /// <remarks>
        /// It will not be verified if the configuration does not contain any reference to the Schema.
        /// </remarks>
        private bool VerifyBatchSchema(string batchXML)
        {
            return schFilePath != null ? SchemaValidator.VerifyVsSchema(batchXML, schFilePath) : true;
        }

        /// <summary>
        /// This is where the logic goes for processing the received message.
        /// </summary>
        /// <param name="payload">The received data is placed in the Payload object</param>
        /// <returns>
        /// 0 for OK
        /// 1 to Retry (Failed)
        /// 2 to Ignore (Failed do not retry)
        /// </returns>
        /// <remarks>
        /// This is where the service owner can choose how to process the incoming message.
        /// We have chosed here to save to file, but other strategies could be implemented, 
        /// as long as it is asynchronous with the further processing of user data.
        /// Example of other strategies:
        /// - Send to MSMQ. Receiving the message is asynchronous from sending.
        /// - Send to REST uri as a POST without return (fire and forget).
        /// - Save to DB. Another program will poll data and process it.
        /// - Save to File as done here, but in addition pass on to for example an internal Queue which is asynchronously picked up by another thread for further processing.
        /// </remarks>
        private int ReceiveMessage(Payload payload)
        {
            string recref = payload.ReceiverReference != null ? payload.ReceiverReference : "";
            string filepath = Path.Combine(inboxDirPath, "pl_" + payload.GetHashCode().ToString() + "_" + recref + ".xml");

            // Here we perform a duplicate check of the file. 
            // If we receive the message before, we inform Altinn of this by returning Failed do not retry.
            // Note that, for this work 100%, a receiverReference (GUID) should be used 
            if (File.Exists(filepath))
            {
                Logger.Log(string.Format("Duplicate message received {0}", filepath), true, EventLogEntryType.Warning);
                return 2;
            }

            try
            {
                if (payload.Attachments != null && payload.Attachments.Length > 0)
                {
                    string attfilepath = filepath.Replace(".xml", ".zip");
                    File.WriteAllBytes(attfilepath, payload.Attachments);
                    payload.Attachments = null;
                }
                Payload.SerializeAsXml(payload, filepath);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, true);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Fetches all the configuraiton settings.
        /// See the Web.config file for how the configuration data is represented.
        /// </summary>
        private static void GetConfig()
        {
            string curpath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

            schFilePath = ConfigurationManager.AppSettings["batch_schema_path"];
            rschFilePath = ConfigurationManager.AppSettings["response_schema_path"];
            inboxDirPath = ConfigurationManager.AppSettings["inbox"];

            if (!schFilePath.Contains(":"))
                schFilePath = Path.Combine(curpath, schFilePath);

            if (!rschFilePath.Contains(":"))
                rschFilePath = Path.Combine(curpath, rschFilePath);

            if (!inboxDirPath.Contains(":"))
                inboxDirPath = Path.Combine(curpath, inboxDirPath);

            if (!Directory.Exists(inboxDirPath))
                Directory.CreateDirectory(inboxDirPath);
        }
    }
}
