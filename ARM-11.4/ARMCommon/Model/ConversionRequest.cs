namespace ARMCommon.Model
{
    public class ConversionRequest
    {
        public string? Source { get; set; }
        public string? Target { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string? Delimiter { get; set; }
        public string? DateFormat { get; set; }

    }

}
