namespace AxStructDef.Models
{
    [Serializable]
    public class TConnection
    {
        public string ConnectionName { get; set; }
        public string ProjectName { get; set; }
        public string DbType { get; set; }
        public string Driver { get; set; }
        public string DbName { get; set; }
        public string PWord { get; set; }
        public string MsDBverno { get; set; }
        public string DbPwd { get; set; }
        public string DBDriver { get; set; }
        public string URL { get; set; }
        public int ConnectNo { get; set; }
        public int SiteNo { get; set; }
        public int DbTimeout { get; set; }
        public double LastNo { get; set; } // Extended in Delphi maps to double in C#
        public TSQLConnection Dbx { get; set; } // Assuming TSQLConnection is defined elsewhere
        public TADOConnection Ado { get; set; } // Assuming TADOConnection is defined elsewhere
        public TOnReConnectToDatabase ReConnectToDatabase { get; set; } // Assuming TOnReConnectToDatabase is a delegate
        public TXServerDate ServerDateTime { get; set; } // Assuming TXServerDate is defined elsewhere
    }

    // Assuming these are defined elsewhere, placeholders for clarity:
    [Serializable]
    public class TSQLConnection
    { /* Implementation details */
    }

    [Serializable]
    public class TADOConnection
    { /* Implementation details */
    }

    public delegate void TOnReConnectToDatabase(); // Assuming this is an event handler or delegate

    [Serializable]
    public class TXServerDate
    { /* Implementation details */
    }
}
