using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxAttachmentsRepository
    {
        Task<SQLResult> GetImageFromDB(AxAttachments attachment);
        Task<SQLResult> DeleteImagesFromDB(AxAttachments attachment);
        Task<SQLResult> WriteImagesToDB(AxAttachments attachment, byte[] chunk, int blobNo, string fileType);
        Task<SQLResult> GetHeaderAttachmentsFromDB(AxAttachments attachment);
        Task<SQLResult> DeleteHeaderAttachmentsFromDB(AxAttachments attachment);
        Task<SQLResult> InsertHeaderAttachmentsToDB(AxAttachments attachment, byte[] chunk, int blobNo, string fileType);

    }
}
