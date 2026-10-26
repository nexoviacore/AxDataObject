namespace AxList
{
    public class ADSDataRequest
    {
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string DataSource { get; set; }
        public string? ARMSessionId { get; set; }
        public Dictionary<string, string>? SQLParams { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
        public bool RefreshCache { get; set; } = false;
        public bool Trace { get; set; } = false;
        public string? SchemaName { get; set; }
        public string? Roles { get; set; }

    }

    public class ADSDataSource
    {
        public string AxDirectSQLID { get; set; }
        public string SQLText { get; set; }
        public string SQLParams { get; set; }
        public string Roles { get; set; } // Roles
        public string CacheData { get; set; } // 'T' for true, 'F' for false
        public string CacheInterval { get; set; } // in minutes
        public string SQLName { get; set; }
        public string EncryptedFlds { get; set; }
        public int FetchSize { get; set; }
    }
}
