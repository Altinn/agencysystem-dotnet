using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AltinnBatchReceiverService.Resources
{
    /// <summary>
    /// The response object
    /// </summary>
    [XmlRoot(ElementName = "OnlineBatchReceipt")]
    public class Response
    {
        [XmlElement(ElementName = "Result")]
        public Result Result { get; set; }

        public static string SerializeAsXml(Response response)
        {
            var stringWriter = new StringWriter();
            XmlSerializer ser = new XmlSerializer(typeof(Response));
            ser.Serialize(stringWriter, response);
            return stringWriter.ToString();
        }
    }

   
    public class Result
    {
        [XmlAttribute(AttributeName = "resultCode")]
        public string Code { get; set; }
    }

}