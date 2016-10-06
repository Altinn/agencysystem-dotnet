using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace OnlineBatchReceiver.Utils
{
    /// <summary>
    /// Utils for handling XML
    /// </summary>
    public static class XmlUtils
    {
        /// <summary>
        /// Creating XmlSerializer object 
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <returns>an object of type T</returns>
        public static XmlSerializer GetXmlSerializerOfType<T>()
        {
            var serializer = new XmlSerializer(typeof(T));

            return serializer;
        }

        /// <summary>
        /// Deserializing an XMLstring
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="serializer">The XmlSerializer object</param>
        /// <param name="batch">batch as string</param>
        /// <returns>T</returns>
        public static T DeserializeXmlString<T>(XmlSerializer serializer, string batch)
        {
            T result;

            using (TextReader reader = new StringReader(batch))
            {
                result = (T)serializer.Deserialize(reader);
            }

            return result;
        }

        /// <summary>
        /// XML valitation
        /// </summary>
        /// <param name="batchXml">batch as string</param>
        /// <param name="filepath">the location of the file</param>
        /// <param name="xmlSchemas">a list of schemas</param>
        /// <returns>true or false</returns>
        public static bool ValidateBatchXml(string batchXml, string filepath, List<string> xmlSchemas)
        {
            try
            {
                var doc = new XmlDocument();

                foreach (var xmlSchema in xmlSchemas)
                {
                    doc.Schemas.Add("", filepath + xmlSchema);
                }

                doc.LoadXml(batchXml);
                doc.Validate(xmlValidationEventHandler);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// XML Validation EventHandler
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">validation event argument</param>
        private static void xmlValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    throw new XmlSchemaValidationException();
                case XmlSeverityType.Error:
                    throw new XmlSchemaValidationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Serializing an XML object to string
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="receiptResult">the OnlineBatchReceiptResult</param>
        /// <returns>XML as string</returns>
        public static string SerializeXmlObjectToString<T>(T receiptResult)
        {
            var stringWriter = new StringWriter();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stringWriter, receiptResult);
            return stringWriter.ToString();
        }
    }
}