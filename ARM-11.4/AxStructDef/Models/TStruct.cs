
namespace AxStructDef.Models
{
    public class TStruct
    {
        public string TransId { get; set; } = string.Empty;
        //  public string Folder {get; set;} = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string SaveControl { get; set; } = string.Empty;
        public string DelControl { get; set; } = string.Empty;
        public bool Track { get; set; } = false;
        public bool TrackChange { get; set; } = false;
        public bool TrackAllFields { get; set; } = false;
        public List<string> TrackFieldsList { get; set; } = [];
        public string NoChange { get; set; } = string.Empty;
        public string ExprSet { get; set; } = string.Empty;
        public string DocDateField { get; set; } = string.Empty;
        public string OnOpen { get; set; } = string.Empty;
        public string AfterLoadData { get; set; } = string.Empty;
        public string BeforeSave { get; set; } = string.Empty;
        public string AfterSave { get; set; } = string.Empty;
        public string BeforeDelete { get; set; } = string.Empty;
        public string AfterDelete { get; set; } = string.Empty;
        public string navigate { get; set; } = string.Empty;
        public string TreeParents { get; set; } = string.Empty;
        public string TreeTables { get; set; } = string.Empty;
        public string Tree { get; set; } = string.Empty;
        public string PrimaryField { get; set; } = string.Empty;
        public bool Attach { get; set; } = false;
        public string SearchCondition { get; set; } = string.Empty;
        public bool ListView { get; set; } = false;
        public string TrackUsers { get; set; } = string.Empty;
        public string TrackFields { get; set; } = string.Empty;
        public string BlockSize { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public bool HasWorkFlow { get; set; } = false;
        public bool SaveDraft { get; set; } = false;
        public bool Traceability { get; set; } = false;     //ch1
        public string CachingType { get; set; } = string.Empty;
        public string DcNavigation { get; set; } = string.Empty;
        public bool Pos { get; set; } = false;  
        public bool DefinitionPage { get; set; } = false;
        public string VersionNo { get; set; } = string.Empty;
        public bool CachedSave { get; set; } = false;
        public bool RunTimeMode { get; set; } = false;
        public bool RunTimeStruct { get; set; } = false;
        public DateTime ModifiedOn { get; set; } = new DateTime();
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = new DateTime();
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsPegAttached { get; set; } = false;
        public bool IsAmendmentEnabled { get; set; } = false;
        public bool HasOutBoundQueues { get; set; } = false;
        public bool HasAutoPrints { get; set; } = false;
        public bool PrintForm { get; set; } = false;
        public bool HasFormNotify { get; set; } = false;
        public bool FitToPage { get; set; } = false;
        public bool Pdf { get; set; } = false;
        public int ServerNo { get; set;} = 0;
        public int ProductId { get; set;} = 0;
        public string UpdatedUsername { get; set; } = string.Empty;
        public string ImportedOn { get; set; } = string.Empty;
        public string ImportedBy { get; set; } = string.Empty;

    }
}
