namespace AxUtils
{
    //public class AxInMemData
    //{
    //    public string AccessCode { get; set; }
    //    public string InMemoryKey { get; set; }
    //    public string? InMemoryValue { get; set; }
    //}
    public class AxInMemReadData
    {
        public string AccessCode { get; set; }
        public string InMemoryKey { get; set; }
        public string? InMemorySubKey { get; set; }
        public string IsBinary { get; set; }
    }

    public class AxInMemWriteData
    {
        public string AccessCode { get; set; }
        public string InMemoryKey { get; set; }
        public string? InMemorySubKey { get; set; }
        public string IsBinary { get; set; }
        public string InMemoryValue { get; set; }
        public string? KeyExpiryInMins { get; set; }
    }

    public class AxInMemDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string result { get; set; }
    }

    public class AxInMemDeleteData
    {
        public string AccessCode { get; set; }
        public string InMemoryKey { get; set; }
    }

    public class AxInMemDeleteDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
