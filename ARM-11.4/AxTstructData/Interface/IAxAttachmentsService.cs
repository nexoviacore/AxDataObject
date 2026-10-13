using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxAttachmentsService
    {
        AxAttachmentResponse GetImage(AxAttachments attachment, bool saveImageInDB = false);
        AxAttachmentResponse GetImageFromDB(AxAttachments attachment);
        AxAttachmentResponse GetImageFromFolder(AxAttachments attachment);
        List<AxAttachmentResponse> GetAttachmentsFromFolder(AxAttachments attachment);
        List<AxAttachmentResponse> GetAttachmentsFromDB(AxAttachments attachment);
        List<AxAttachmentResponse> GetAttachments(AxAttachments attachment);
        List<AxAttachmentResponse> GetAxpFilesFromFolder(AxAttachments attachment);

        bool SaveImages(AxAttachments attachment, bool saveImageInDB = false, bool deleteExistingImage = false);
        bool WriteImagesToDB(AxAttachments attachment, bool deleteExistingImage = false);
        bool WriteImagesToFolder(AxAttachments attachment);
        bool SaveAttachments(AxAttachments attachment);
        bool WriteAttachmentsToFolder(AxAttachments attachment);
        bool WriteAttachmentsToDB(AxAttachments attachment);
        bool WriteAxpFilesToFolder(AxAttachments attachment);
    }
}
