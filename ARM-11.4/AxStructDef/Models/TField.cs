using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Xml;

namespace AxStructDef.Models
{
    public class TField
    {
        public string FieldName { get; set; } = string.Empty;
        public string Tablename { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string ModeofEntry { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string LinkField { get; set; } = string.Empty;
        public string SourceField { get; set; } = string.Empty;
        public string Sourcetable { get; set; } = string.Empty;
        public string SourceTransid { get; set; } = string.Empty;
        public string CField { get; set; } = string.Empty;
        public string Sgwidth { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string fromListValues { get; set; } = string.Empty;
        public string SelType { get; set; } = string.Empty;
        public List<string> SQL { get; set; } = new List<string>();
        public string SQLText { get; set; } = string.Empty;
        public List<string> SearchSQL { get; set; } = new List<string>();
        public List<string> Displaydetail { get; set; } = new List<string>();
        public List<string> Prefixflds { get; set; } = new List<string>();
        public List<string> Dependents { get; set; } = new List<string>();
        public string DispName { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string DetTransid { get; set; } = string.Empty;
        public string DetCondition { get; set; } = string.Empty;
        public string DepFrames { get; set; } = string.Empty;
        public string DependentTypes { get; set; } = string.Empty;
        public string RefOnChngFlds { get; set; } = string.Empty;
        public int FrameNo { get; set; } = 0;
        public int Width { get; set; } = 0;
        public int Dec { get; set; } = 0;
        public int CurDec { get; set; } = 0;
        public int DBDec { get; set; } = 0;
        public int Exprn { get; set; } = 0;
        public int ValExprn { get; set; } = 0;
        public int OrderNo { get; set; } = 0;
        public int Searchcol { get; set; } = 0;
        public int Sgheight { get; set; } = 0;
        public int PopIndex { get; set; } = -1;
        public int Contorderno { get; set; } = 0;
        public int AxRule_ValExprn { get; set; } = 0;
        public int AxRule_ValOnSave_ValExprn { get; set; } = 0;
        public bool SourceKey { get; set; } = false;
        public bool AsGrid { get; set; } = false;
        public bool NoDuplicate { get; set; } = false;
        public bool Empty { get; set; } = false;
        public bool Suggestive { get; set; } = false;
        public bool Hidden { get; set; } = false;
        public bool Autoselect { get; set; } = false;
        public bool Readonly { get; set; } = false;
        public bool Multiline { get; set; } = false;
        public bool ClientValidation { get; set; } = false;
        public bool HasGridParents { get; set; } = false;
        public bool HasHyperLink { get; set; } = false;
        public bool AllowChange { get; set; } = false;
        public bool DisplayTotal { get; set; } = false;
        public bool HasParams { get; set; } = false;
        public bool SetCarry { get; set; } = false;
        public bool Refresh { get; set; } = false;
        public bool FromList { get; set; } = false;
        public bool ApplyComma { get; set; } = false;
        public bool OnlyPositive { get; set; } = false;
        public bool DynamicParams { get; set; } = false;
        public bool DispField { get; set; } = false;
        public bool IsBtnField { get; set; } = false;
        public bool IsParentField { get; set; } = false;
        public bool RefreshOnChange { get; set; } = false;
        public bool Cl_multiselect { get; set; } = false;
        public string Cexp { get; set; } = string.Empty;
        public string Cvalexp { get; set; } = string.Empty;
        public string AxRule_cvalexp { get; set; } = string.Empty;
        public string AxRule_ValOnSave_cvalexp { get; set; } = string.Empty;
        [JsonIgnore]
        public char Pwchar { get; set; } = '\0';
        public List<string> Listvals { get; set; } = new List<string>();
        public List<string> Deps { get; set; } = new List<string>();
        public List<string> Detmap { get; set; } = new List<string>();
        public List<string> Pickfields { get; set; } = new List<string>();
        public List<string> Pickcaptions { get; set; } = new List<string>();
        public List<string> DataRows { get; set; } = new List<string>();
        public bool CommaSelection { get; set; } = false;
        public bool TxtSelection { get; set; } = false;
        public bool Combobox { get; set; } = false;
        public bool SaveValue { get; set; } = false;
        public bool EncryptValue { get; set; } = false;
        public bool CustomDecimal { get; set; } = false;
        [JsonIgnore]
        public XmlNode SequenceNode { get; set; }
        public bool Tabstop { get; set; } = false;
        public string FontStr { get; set; } = string.Empty;
        public string ColorStr { get; set; } = string.Empty;
        public string CustType { get; set; } = string.Empty;
        public string Separator { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public bool UsedQuotedStr { get; set; } = false;
        public double DepOrdNo { get; set; } = 0;
        public TPickListMode PickListMode { get; set; } = TPickListMode.plmNone;
        public string PickListDef { get; set; } = string.Empty;
        public bool PickRecFound { get; set; } = false;
        public int PickListLevel { get; set; } = 0;
        public bool WordSearch { get; set; } = false;
        public Dictionary<string, string> FilterCols { get; set; } = new Dictionary<string, string>();
        public List<string> FldUX { get; set; } = new List<string>();
        public string Tlhw { get; set; } = string.Empty;
        public string Lbltlhw { get; set; } = string.Empty;
        public int Parser_Idx { get; set; } = -1;
        public string FieldValue { get; set; } = string.Empty;
        public string OldFieldValue { get; set; } = string.Empty;
        public Dictionary<string, string> FieldValList { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> OldFieldValList { get; set; } = new Dictionary<string, string>();
        public delegate bool TEnterFieldProc(int rowNo, bool fieldEmpty);
        public TEnterFieldProc EnterFieldProc { get; set; } = null;
        public Int64 IDValue { get; set; } = 0;
        public Int64 OldIDValue { get; set; } = 0;
        public Int64 RecId { get; set; } = 0;
        public List<string> IDValList { get; set; } = new List<string>();
        public List<string> OldIDValList { get; set; } = new List<string>();
        public List<string> RecIDList { get; set; } = new List<string>();
        public List<string> OldRecIDList { get; set; } = new List<string>();
        public List<string> QParamvalues { get; set; } = new List<string>();
        public List<string> QParamnames { get; set; } = new List<string>();
        public List<string> LRUList { get; set; } = new List<string>();
        public int RapidQSelectCDSIndex { get; set; } = -1;
        public bool ValueSubmitted { get; set; } = false;
        public string DefCaption { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;

        [JsonProperty(nameof(Pwchar))]
        public string PwcharString
        {
            get => Pwchar == '\0' ? null : Pwchar.ToString();
            set => Pwchar = string.IsNullOrEmpty(value) ? '\0' : value[0];
        }
    }
}
