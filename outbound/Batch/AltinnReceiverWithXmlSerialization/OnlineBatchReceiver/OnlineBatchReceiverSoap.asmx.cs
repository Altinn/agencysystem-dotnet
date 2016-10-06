using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Services;
using System.Xml.Serialization;
using log4net;
using OnlineBatchReceiver.Utils;

namespace OnlineBatchReceiver
{
    /// <summary>
    /// Summary description for OnlineBatchReceiverSoap
    /// </summary>
    // [WebService(Namespace = "http://tempuri.org/")]
    // [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    // [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    [WebServiceBinding(Name = "OnlineBatchReceiverSoap", Namespace = "http://AltInn.no/webservices/")]
    public class OnlineBatchReceiverSoap : WebService
    {
        private readonly ILog _logger;
        // Finds the directory where the app is deployed
        private readonly string _filepath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        private readonly string _foldername = ConfigurationManager.AppSettings["RecievedXmlFolder"];

        public OnlineBatchReceiverSoap()
        {
            _logger = LogManager.GetLogger(GetType());
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapDocumentMethod("http://AltInn.no/webservices/ReceiveOnlineBatchExternalAttachment",
            RequestNamespace = "http://AltInn.no/webservices/",
            ResponseNamespace = "http://AltInn.no/webservices/",
            Use = System.Web.Services.Description.SoapBindingUse.Literal,
            ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string ReceiveOnlineBatchExternalAttachment(string username, string passwd, string receiversReference, long sequenceNumber, string batch, [XmlElement(DataType = "base64Binary")] byte[] attachments)
        {
            _logger.Info("ReceiveOnlineBatchExternalAttachment Recieved from: " + username);
            _logger.Debug("ReceiveOnlineBatchExternalAttachment Recieved from: " + username + " Batch: " + batch);

            // Authenticate username + passw
            if (!Authenticate(username, passwd))
            {
                _logger.Debug("ReceiveOnlineBatchExternalAttachment Invalid request");
                return Response(resultCodeType.FAILED_DO_NOT_RETRY);
            }
            _logger.Debug("ReceiveOnlineBatchExternalAttachment, User Aithenticated");
            // Verify batch vs. XSD (Schema verification)
            if (!XmlUtils.ValidateBatchXml(batch, _filepath, new List<string> { "/xsd/genericbatch.2013.06.xsd" }))
            {
                _logger.Debug("ReceiveOnlineBatchExternalAttachment Validation Failed");
                return Response(resultCodeType.FAILED_DO_NOT_RETRY);
            }

            try
            {
                var serializer = XmlUtils.GetXmlSerializerOfType<DataBatch>();
                // result is a DataBack object, which can be sent async to a recipient in the reciever application portfolio
                var result = XmlUtils.DeserializeXmlString<DataBatch>(serializer, batch);

                if (FileUtil.AlreadyExists(_filepath, _foldername, username, receiversReference, sequenceNumber))
                {
                    _logger.Debug("ReceiveOnlineBatchExternalAttachment Duplicate Request");
                    return Response(resultCodeType.FAILED_DO_NOT_RETRY);
                }
                // Saving payload to disk
                var filename = Guid.NewGuid() + "_" + username + "_" + receiversReference + "_" + sequenceNumber;
                FileUtil.SaveXmlFileToDisk(_filepath, _foldername, filename, serializer, result);
                FileUtil.SaveAttatchmentsAsZip(_filepath, _foldername, filename, attachments);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return ex.Message;
            }

            _logger.Debug("ReceiveOnlineBatchExternalAttachment Validated OK ");
            return Response(resultCodeType.OK);
        }

        /// <summary>
        /// Authenticate user, in this example the user is always Altinn, and password cannot be empty
        /// </summary>
        /// <param name="username">the username</param>
        /// <param name="password">the password</param>
        /// <returns>true or false</returns>
        private static bool Authenticate(string username, string password)
        {
            return username == "Altinn" && !string.IsNullOrEmpty(password);
        }

        /// <summary>
        /// return OnlineBatchReceiptResult object as a string
        /// </summary>
        /// <param name="code">the resultCodeType</param>
        /// <returns>object as string</returns>
        private string Response(resultCodeType code)
        {
            var receiptResult = new OnlineBatchReceiptResult
            {
                resultCode = code,
                resultCodeSpecified = true,
                Value = ""
            };

            return XmlUtils.SerializeXmlObjectToString(receiptResult);
        }
    }
}
