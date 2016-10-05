using DownloadQueueExampleClient.DownloadQueueService;
using DownloadQueueExampleClient.Utils;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DownloadQueueExampleClient
{

    /// <summary>
    /// Handler for donwloading schema files with attachments from an altinn download queue for a specific service owner and for a specific service.
    /// </summary>
    public class DownloadQueueHandler
    {
        private string systemUserName;
        private string systemPassword;
        private string serviceCode;
        private int languageId;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="systemUserName">Service Owner</param>
        /// <param name="systemPassword">Password in clear text</param>
        /// <param name="serviceCode">Specifies the specific service</param>
        public DownloadQueueHandler(string systemUserName, string systemPassword, string serviceCode)
        {
            this.systemUserName = systemUserName;
            this.systemPassword = systemPassword;
            this.serviceCode = serviceCode;
        }


        /// <summary>
        /// Runs the download, maximum given items, and saves to the configured inbox (one folder with attachments for each schema)
        /// </summary>
        /// <param name="maxItemsToProcess">Downloads a maximum given or if not given, all schemas.</param>
        /// <remarks>
        /// Performs the following steps for each item:
        /// 1. Fetces the item from queue
        /// 2. Saves the schema and attachments
        /// 3. Purges the item (remove from queue)
        /// </remarks>
        public void Execute(int maxItemsToProcess = int.MaxValue, int languageId = 1044)
        {
            int it = 0;
            this.languageId = languageId;
            DownloadQueueItemBE item;
            do
            {
                //1. Get item
                try
                {
                    item = GetDownloadQueueItem();
                    if (item == null)
                    {
                        Logger.Log("Download queue empty!");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to get download queue item. {e.Message}");
                    return;
                }

                //2. Persist/process item - download formset data as pdf & save attachments
                try
                {
                    PersistItem(item);
                }
                catch (Exception exceptionWhileProcessingItem)
                {
                    Logger.Log($"Error while processing queue item: {exceptionWhileProcessingItem.Message}", true);
                    continue;
                }

                //3. Purge item
                try
                {
                    PurgeItem(item.ArchiveReference);
                    Logger.Log($"Successfully processed queue item and removed from download queue: {item.ArchiveReference}");
                }
                catch (Exception exceptionOnPurgeItem)
                {
                    Logger.Log($"Error while removing item from download queue: {exceptionOnPurgeItem.Message}", true);
                }

                it++;
            }
            while (item != null && it < maxItemsToProcess);
        }

        private DownloadQueueItemBE GetDownloadQueueItem()
        {
            using (var client = new DownloadQueueExternalBasicClient())
            {
                var queueItems = client.GetDownloadQueueItems(systemUserName, systemPassword, serviceCode);
                if (queueItems != null)
                    Logger.Log($"Download queue contains {queueItems.Count} elements.");
                return queueItems.FirstOrDefault();
            }
        }

        private void PurgeItem(string archiveReference)
        {
            using (var client = new DownloadQueueExternalBasicClient())
            {
                client.PurgeItem(systemUserName, systemPassword, archiveReference);
            }
        }

        private void PersistItem(DownloadQueueItemBE item)
        {
            //save formset
            DownloadFormSet(item.ArchiveReference);

            //save attachments
            DownloadAttachments(item.ArchiveReference);
        }

        private void DownloadFormSet(string archiveReference)
        {
            byte[] formData;
            try
            {
                using (var client = new DownloadQueueExternalBasicClient())
                {
                    formData = client.GetFormSetPdfBasic(systemUserName, systemPassword, archiveReference, languageId);
                }
            }
            catch(Exception e)
            {
                Logger.Log($"Failed to download form set for item {archiveReference}. {e.Message}");
                throw;
            }

            Logger.Log($"Successfully downloaded form set for item {archiveReference}.");

            SaveFile(formData, archiveReference, archiveReference, ".pdf");
        }

        private void DownloadAttachments(string archiveReference)
        {
            using (var client = new DownloadQueueExternalBasicClient())
            {
                var formTask = client.GetArchivedFormTaskBasicDQ(systemUserName, systemPassword, archiveReference, languageId);
                var attachments = formTask.Attachments;

                foreach(var attachment in attachments)
                {
                    //attachment > 30MB, use streaming service
                    if (attachment.AttachmentData == null)
                    {
                        SaveStreamedAttachment(attachment.AttachmentId, attachment.FileName, archiveReference);
                    }
                    else //save attachment data to file
                    {
                        SaveFile(attachment.AttachmentData, archiveReference, attachment.FileName);
                    }
                }
            }
        }

        private void SaveStreamedAttachment(int attachmentId, string fileName, string archiveReference)
        {
            byte[] data;
            try
            {
                using (var client = new ServiceOwnerArchiveExternalStreamed.ServiceOwnerArchiveExternalStreamedBasicClient())
                {
                    data = client.GetAttachmentDataStreamedBasic(systemUserName, systemPassword, attachmentId);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed download streamed attachment for attachmentId: {attachmentId}. {e.Message}");
                throw;
            }

            Logger.Log($"Successfully downloaded streamed attachment with id {attachmentId}.");

            SaveFile(data, archiveReference, fileName);
        }


        private static void SaveFile(object file, string folder, string filename, string extension = null)
        {
            try
            {
                FileSaver.SaveFile(file, folder, filename, extension);
            }
            catch (Exception e)
            {
                Logger.Log($"Error while saving file {filename}. {e.Message}");
                throw;
            }
        }


    }
}
