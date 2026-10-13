using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IFiles
    {
        Task<string> GetFileByRecordId(string recordId, string filePath);

        Task<FileResponse> GetFile(string Filename, string FilePath);
        Task<MultiFileResponse> GetMultipleFiles(string[] filenames, string filePath);
    }
}
