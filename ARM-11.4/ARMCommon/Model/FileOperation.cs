namespace ARMCommon.Model
{
    public class FileRequest
    {
        public string RecordId { get; set; }
        public string FilePath { get; set; }


    }


    public class GetFilerequest
    {
        public string FilePath { get; set; }

        public string Filename { get; set; }
    }

    public class MultiFileResponse
    {
        public bool Success { get; set; }
        public List<FileResult> Result { get; set; }


    }


    public class MultiFiles
    {
        public string[] Filenames { get; set; }

        public string FilePath { get; set; }
    }

    public class FileResult
    {
        public bool Success { get; set; }
        public string Data { get; set; }

        public string filename { get; set; }
    }
    public class FileResponse
    {
        public bool Success { get; set; }
        public string Filename { get; set; }
        public string Base64 { get; set; }
        public string Error { get; set; }
    }
}
