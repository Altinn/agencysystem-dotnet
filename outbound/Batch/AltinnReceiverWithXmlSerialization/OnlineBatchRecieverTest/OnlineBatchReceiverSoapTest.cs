using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OnlineBatchReceiver;

namespace OnlineBatchRecieverTest
{
    [TestFixture]
    public class OnlineBatchReceiverSoapTest
    {
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Delete files generated in the test
            var path = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            var files = GetFiles(path, ".xml", ".zip");
            foreach (var file in files)
            {
                try
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static IEnumerable<FileInfo> GetFiles(string path, params string[] extensions)
        {
            var list = new List<FileInfo>();
            foreach (var ext in extensions)
                list.AddRange(new DirectoryInfo(path).GetFiles("*" + ext).Where(p =>
                      p.Extension.Equals(ext, StringComparison.CurrentCultureIgnoreCase))
                      .ToArray());
            return list;
        }

        [Test]
        public void ReceiveOnlineBatchExternalAttachment_Returns_FAILED_DO_NOT_RETRY_when_InvalidUser_and_InvalidPassword_and_InvalidXml()
        {
            var target = new OnlineBatchReceiverSoap();

            var result = target.ReceiveOnlineBatchExternalAttachment("BogusUser", "", "", 0, "<bogus><xml>test</xml></bogus>", null);
            Assert.IsTrue(result.Contains(resultCodeType.FAILED_DO_NOT_RETRY.ToString()));
        }

        [Test]
        public void ReceiveOnlineBathExternalAttachment_Returns_FAILED_DO_NOT_RETRY_when_InvalidUser_and_InvalidPassword_and_ValidXml()
        {
            var target = new OnlineBatchReceiverSoap();

            var result = target.ReceiveOnlineBatchExternalAttachment("BogusUser", "", "", 0,
                "<DataBatch schemaVersion=\"1.0\" batchReference=\"0\" previousReference=\"0\" receiverReference=\"f5c7892e-e4ff-4820-98eb-38641fb983b3\" timeStamp=\"2015-09-15T13:30:51\" formTasksInBatch=\"1\"><DataUnits><DataUnit reportee=\"03033140044\" archiveReference=\"AR98330\" archiveTimeStamp=\"2015-09-15T13:30:48.91\"><Approvers><Approver approverId=\"03033140044\" approvedTimeStamp=\"2015-09-15T13:30:48.833\" securityLevel=\"lessSensitive\" /></Approvers><FormTask><ServiceCode>3903</ServiceCode><ServiceEditionCode>150512</ServiceEditionCode><Form><DataFormatId>1243</DataFormatId><DataFormatVersion>10656</DataFormatVersion><Reference>39815</Reference><ParentReference>0</ParentReference><FormData><![CDATA[<Skjema xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:brreg=\"http://www.brreg.no/or\" xmlns:dfs=\"http://schemas.microsoft.com/office/infopath/2003/dataFormSolution\" xmlns:tns=\"http://www.altinn.no/services/ServiceEngine/ServiceMetaData/2009/10\" xmlns:q1=\"http://schemas.altinn.no/services/ServiceEngine/ServiceMetaData/2009/10\" xmlns:ns1=\"http://www.altinn.no/services/Register/ER/2009/10\" xmlns:ns2=\"http://schemas.altinn.no/services/Register/2009/10\" xmlns:ns3=\"http://www.altinn.no/services/2009/10\" xmlns:q3=\"http://www.altinn.no/services/common/fault/2009/10\" xmlns:ns4=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:my=\"http://schemas.microsoft.com/office/infopath/2003/myXSD/2010-02-11T08:42:27\" xmlns:xd=\"http://schemas.microsoft.com/office/infopath/2003\" skjemanummer=\"1243\" spesifikasjonsnummer=\"10656\" blankettnummer=\"RF-1117\" tittel=\"Klage på likningen\" gruppeid=\"5800\" etatid=\"NoAgency\"><Skattyterinfor-grp-5801 gruppeid=\"5801\"><info-grp-5802 gruppeid=\"5802\"></info-grp-5802><Kontakt-grp-5803 gruppeid=\"5803\"><KontaktpersonEPost-datadef-27688 orid=\"27688\">xxx@yyy.no</KontaktpersonEPost-datadef-27688><Samtykke_Skatt-datadef-30001>2</Samtykke_Skatt-datadef-30001><Samtykke_Folkeregister-datadef-30002>2</Samtykke_Folkeregister-datadef-30002></Kontakt-grp-5803><klagefrist-grp-5804 gruppeid=\"5804\"><KlageGjeldendeInntektsar-datadef-25455 orid=\"25455\">2014</KlageGjeldendeInntektsar-datadef-25455><KlagemeldingSendtInnenKlagefrist-datadef-25454 orid=\"25454\">Ja</KlagemeldingSendtInnenKlagefrist-datadef-25454></klagefrist-grp-5804></Skattyterinfor-grp-5801><klage-grp-5805 gruppeid=\"5805\"><spesifisering-grp-5836 gruppeid=\"5836\"><KlageSpesifisering-datadef-25457 orid=\"25457\">3t5q5</KlageSpesifisering-datadef-25457></spesifisering-grp-5836></klage-grp-5805></Skjema>]]></FormData></Form></FormTask></DataUnit></DataUnits></DataBatch>", null);
            Assert.IsTrue(result.Contains(resultCodeType.FAILED_DO_NOT_RETRY.ToString()));
        }

        [Test]
        public void ReceiveOnlineBatchExternalAttachment_Returns_FAILED_DO_NOT_RETRY_When_ValidUser_and_ValidPassword_and_InvalidXml()
        {
            var target = new OnlineBatchReceiverSoap();

            var result = target.ReceiveOnlineBatchExternalAttachment("Altinn", "GotPassword", "", 0, "<bogus><xml>test</xml></bogus>", null);
            Assert.IsTrue(result.Contains(resultCodeType.FAILED_DO_NOT_RETRY.ToString()));
        }

        [Test]
        public void ReceiveOnlineBatchExternalAttachment_Returns_OK_When_ValidUser_and_ValidPassword_and_ValidXml()
        {
            var target = new OnlineBatchReceiverSoap();

            var result = target.ReceiveOnlineBatchExternalAttachment("Altinn", "GotPassword", Guid.NewGuid().ToString(), 0,
                "<DataBatch schemaVersion=\"1.0\" batchReference=\"0\" previousReference=\"0\" receiverReference=\"f5c7892e-e4ff-4820-98eb-38641fb983b3\" timeStamp=\"2015-09-15T13:30:51\" formTasksInBatch=\"1\"><DataUnits><DataUnit reportee=\"03033140044\" archiveReference=\"AR98330\" archiveTimeStamp=\"2015-09-15T13:30:48.91\"><Approvers><Approver approverId=\"03033140044\" approvedTimeStamp=\"2015-09-15T13:30:48.833\" securityLevel=\"lessSensitive\" /></Approvers><FormTask><ServiceCode>3903</ServiceCode><ServiceEditionCode>150512</ServiceEditionCode><Form><DataFormatId>1243</DataFormatId><DataFormatVersion>10656</DataFormatVersion><Reference>39815</Reference><ParentReference>0</ParentReference><FormData><![CDATA[<Skjema xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:brreg=\"http://www.brreg.no/or\" xmlns:dfs=\"http://schemas.microsoft.com/office/infopath/2003/dataFormSolution\" xmlns:tns=\"http://www.altinn.no/services/ServiceEngine/ServiceMetaData/2009/10\" xmlns:q1=\"http://schemas.altinn.no/services/ServiceEngine/ServiceMetaData/2009/10\" xmlns:ns1=\"http://www.altinn.no/services/Register/ER/2009/10\" xmlns:ns2=\"http://schemas.altinn.no/services/Register/2009/10\" xmlns:ns3=\"http://www.altinn.no/services/2009/10\" xmlns:q3=\"http://www.altinn.no/services/common/fault/2009/10\" xmlns:ns4=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:my=\"http://schemas.microsoft.com/office/infopath/2003/myXSD/2010-02-11T08:42:27\" xmlns:xd=\"http://schemas.microsoft.com/office/infopath/2003\" skjemanummer=\"1243\" spesifikasjonsnummer=\"10656\" blankettnummer=\"RF-1117\" tittel=\"Klage på likningen\" gruppeid=\"5800\" etatid=\"NoAgency\"><Skattyterinfor-grp-5801 gruppeid=\"5801\"><info-grp-5802 gruppeid=\"5802\"></info-grp-5802><Kontakt-grp-5803 gruppeid=\"5803\"><KontaktpersonEPost-datadef-27688 orid=\"27688\">xxx@yyy.no</KontaktpersonEPost-datadef-27688><Samtykke_Skatt-datadef-30001>2</Samtykke_Skatt-datadef-30001><Samtykke_Folkeregister-datadef-30002>2</Samtykke_Folkeregister-datadef-30002></Kontakt-grp-5803><klagefrist-grp-5804 gruppeid=\"5804\"><KlageGjeldendeInntektsar-datadef-25455 orid=\"25455\">2014</KlageGjeldendeInntektsar-datadef-25455><KlagemeldingSendtInnenKlagefrist-datadef-25454 orid=\"25454\">Ja</KlagemeldingSendtInnenKlagefrist-datadef-25454></klagefrist-grp-5804></Skattyterinfor-grp-5801><klage-grp-5805 gruppeid=\"5805\"><spesifisering-grp-5836 gruppeid=\"5836\"><KlageSpesifisering-datadef-25457 orid=\"25457\">3t5q5</KlageSpesifisering-datadef-25457></spesifisering-grp-5836></klage-grp-5805></Skjema>]]></FormData></Form></FormTask></DataUnit></DataUnits></DataBatch>", null);
            Assert.IsTrue(result.Contains(resultCodeType.OK.ToString()));
        }
    }
}
