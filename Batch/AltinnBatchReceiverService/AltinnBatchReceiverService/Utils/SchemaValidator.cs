using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AltinnBatchReceiverService.Utils
{
    public static class SchemaValidator
    {
        /// <summary>
        /// This is a generic Schema verification method, which will verify any XML vs. the Schema.
        /// /// </summary>
        /// <param name="xml">The XML to be verified</param>
        /// <param name="xsdFilePath">File path to the XSD schema</param>
        /// <returns>True if successful verification</returns>
        /// <remarks>
        /// This could be optimized to build a SchemaSet once and reuse it.
        /// </remarks>
        public static bool VerifyVsSchema(string xml, string xsdFilePath)
        {
            XDocument xdoc;

            try
            {
                xdoc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to parse Data XML: {0} : {1}", ex.Message, xml), true);
                return false;
            }
            var schema = new XmlSchemaSet();

            try
            {
                schema.Add("", xsdFilePath);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to load XSD Schema: {0} : {1}", ex.Message, xsdFilePath), true);
                return false;
            }

            Boolean result = true;
            xdoc.Validate(schema, (sender, e) =>
            {
                result = false;
                Logger.Log(string.Format("Invalid Schema detected: {0} : {1}", e.Message, xml), true);
            });

            return result;
        }
    }
}