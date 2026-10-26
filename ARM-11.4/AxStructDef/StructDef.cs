using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxStructDef.Helpers;
using AxStructDef.Models;
using Newtonsoft.Json;
using Pipelines.Sockets.Unofficial.Arenas;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace AxStructDef
{
    /// <summary>
    /// Represents a structure definition for handling form structures, fields, frames, and data containers.
    /// Provides functionality for loading structures, managing fields, and handling data operations.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class StructDef : IDisposable
    {
        #region Serialized Private Members

        [JsonProperty] private List<string> _oldFieldsList;
        [JsonProperty] private bool _depCall;
        [JsonProperty] private bool _clientNotify;
        [JsonProperty] private List<string> _autogenTransid;
        [JsonProperty] private List<string> _prefixes;
        [JsonProperty] private List<string> _prefixFields;
        [JsonProperty] private List<string> _autoGenVals;
        [JsonProperty] private string _transType;
        [JsonProperty] private bool _disposed;
        [JsonProperty] private bool _onLoad;
        //[JsonProperty] private List<StructDef> _structDefList;
        [JsonProperty] private List<string> _genFieldValueList;
        [JsonProperty] private List<Int64> _genIdValueList;
        [JsonProperty] private string _genFieldValue;
        [JsonProperty] private Int64 _genIdValue;
        [JsonProperty] private int _parentRowNo;
        [JsonProperty] private bool _rowExists;
        [JsonProperty] private string _oldFieldValue;
        [JsonProperty] private List<string> _invalidRows;
        [JsonProperty] private List<string> _fieldsAdded;
        [JsonProperty] private List<string> _rowsAdded;
        [JsonProperty] private List<string> _groupFieldValues;

        #endregion

        #region Non-Serialized Private Members

        private XmlNode _map;
        private TFrame? _frm;
        //private TPopgrid _popGrid;
        private int _fieldNo;
        //private int _priorFrmNo;
        private int _contFieldNo;
        //private List<string> _ceatedFlds;
        //private List<string>? _createdDcs;
        //private List<string>? _fldCreaedOrder;
        //private bool _forDep;
        //private List<string>? _rowCountList;
        //private int _parentControl;
        //private string _tableName;
        //private string _firstTableName;
        //private string _masterTableName;
        //private string _sUserName;
        //private string _sSiteNo;
        //private string _sModifiedOn;
        //private bool _loading;
        //private bool _webData;
        private Utils? _common;
        private AxStructDefUtils? _utils;
        private IARMLogTraceService? _traceService;
        public StructDef? _parentStructDef;
        private static readonly Dictionary<Type, DbType> _typeMap = new()
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(byte[]), DbType.Binary },
            { typeof(byte?), DbType.Byte },
        };
        private XmlDocument? _xml;
        private Dictionary<string, TField>? _fieldLookup;
        private bool _lookupInitialized;
        private List<GenMapField>? _currentGenMappings;

        #endregion

        #region Serialized Public Members

        [JsonProperty(Order = -12)] public TStruct? Struct { get; private set; }
        [JsonProperty(Order = -11)] public List<TFrame>? Frames { get; private set; }
        [JsonProperty(Order = -10)] public List<TField>? Flds { get; private set; }
        [JsonProperty(Order = -9)] public List<TFg>? Fgs { get; private set; }
        [JsonProperty(Order = -8)] public List<TMdMap>? MdMaps { get; private set; }
        [JsonProperty(Order = -7)] public List<TGenMap>? GenMaps { get; private set; }
        [JsonProperty(Order = -6)] public List<TScript>? Scripts { get; set; }
        [JsonProperty(Order = -5)] public List<TButton> Buttons { get; set; }
        [JsonProperty(Order = -4)] public List<TIframe>? Iframes { get; set; }
        [JsonProperty(Order = -3)] public TTemp Temp { get; set; }
        [JsonProperty(Order = -2)] public TAxpConfig AxpConfig { get; set; }
        public List<TPopgrid>? PopGrids { get; private set; }
        public string AutoGen_Prefix { get; set; }
        public string AutoGen_Lastno { get; set; }
        public List<string> ExprSetList { get; private set; }
        public List<string> ParentList { get; private set; }
        public List<string> PopParentList { get; private set; }
        public List<string> ImgFldList { get; private set; }
        public List<FieldRec>? FieldList { get; set; }
        public string UserName { get; set; }
        public List<string> FrameNames { get; set; }
        //public string GridString { get; set; }
        public string PrimaryTable { get; set; }
        public List<string> Tables { get; set; }
        public List<string> PrimaryFields { get; set; }
        public int FieldCount { get; set; }
        //public int FrameCount { get; set; }
        //public int ActiveRow { get; set; }
        //public int FailCount { get; set; }
        //public int ActiveFrame { get; set; }
        //public int ActiveField { get; set; }
        //public int NormalFrames { get; set; }
        //public bool HasDetStruct { get; set; }
        //public int LastFieldNo { get; set; }
        //public string MastFrameNos { get; set; }
        public string Stype { get; set; }
        public bool HasViewImgFlds { get; set; }
        public bool HasImage { get; set; }
        public bool HasImgPath { get; set; }
        public bool SaveImageInDB { get; set; }
        public string FileUploadLimit { get; set; }
        public List<string> NoEmptyImgFlds { get; set; }
        //public List<string> Oldfields { get; private set; }
        //public List<string> TableList { get; private set; }
        public string DynamicLabels { get; set; }
        public bool HasAutoGenFields { get; set; }
        public bool GetDependants { get; set; }
        public bool GetPList { get; set; }
        public bool HasPickListFlds { get; set; }
        //public string VisibleDCs { get; set; }
        //public string LDSUniqueFld { get; set; }
        //public string LDSPrefixFld { get; set; }
        //public string LDSAutoGenDigitFld { get; set; }
        //public string HidedDCs { get; set; }
        //public bool FldLoadedDuringStructLoad { get; set; }
        //public List<string> QueryStrList { get; private set; }
        //public double Timezone_diff { get; set; }
        //public bool IsCreatedOnDefined { get; set; }
        //public bool IsModifiedOnDefined { get; set; }
        //public bool IsUserNameDefined { get; set; }
        //public bool IsSiteNoDefined { get; set; }
        //public bool IsMapNameDefined { get; set; }
        //public bool IsCancelDefined { get; set; }
        //public bool IsTransIdDefined { get; set; }
        //public bool IsAppDescDefined { get; set; }
        public Int64 PrimaryTableId { get; set; }
        public Int64 Recid { get; set; }
        public Int64 NewRecid { get; set; }
        public Int64 LastSavedRecordid { get; set; }
        public List<string> AutogenFields { get; private set; }
        public List<string> SdAutogenFields { get; private set; }
        public bool NewTrans { get; set; }
        public List<string> RecordIdList { get; private set; }
        //public bool EntryDocLoaded { get; set; }
        //public bool Cancelled { get; set; }
        //public bool Deleted { get; set; }
        public bool CancelledTrans { get; set; }
        public string CancelRemarks { get; set; }
        public bool ChildTrans { get; set; }
        //public string ChildDoc { get; set; }
        public string CompanyName { get; set; }
        public string MapName { get; set; }
        public Int64 SourceId { get; set; }
        public Int64 PublicSourceId { get; set; }
        public Int64 MapParentRecordId { get; set; }
        public Int64 SourcePrimaryId { get; set; }
        public bool BStoreDataAsCSVFile { get; set; }
        public string NoAutoGenStr { get; set; }
        public string PrimaryTableName { get; set; }
        public List<string> SubFrames { get; set; }
        public List<string> Ins_del_rows { get; set; }
        public string LocThousandSeparator = ",";
        public string clientDateFormat = "dd/MM/yyyy";
        //public List<StructDef> StructDefList
        //{
        //    get { return _structDefList; }
        //    set { _structDefList = value; }
        //}
        public int axdecimal = -1;
        public List<SQLCommandBuilder> PreparedSqlOperations { get; private set; }
        public Dictionary<string, string>? FieldWiseDCs { get; private set; }

        #endregion

        #region Non-Serialized Public Members

        [JsonIgnore]
        public IARMLogTraceService TraceService
        {
            get { return _traceService; }
            set { _traceService = value; }
        }
        public static DateTimeFormatInfo ShortDateFormat { get; set; } = new();
        [JsonIgnore]
        public IAxParser AxParser { get; set; }

        #endregion


        #region Constructors and Factory Methods

        private StructDef(string pName, Utils common, IARMLogTraceService traceService):this()
        {
            _common = common;
            _traceService = traceService;
            _utils = new AxStructDefUtils(_common);
            _transType = pName;
        }

        [JsonConstructor]
        public StructDef()
        {
            _oldFieldsList = new List<string>();
            _depCall = false;
            _clientNotify = false;
            _autogenTransid = new List<string>();
            _prefixes = new List<string>();
            _prefixFields = new List<string>();
            _autoGenVals = new List<string>();
            _transType = string.Empty;
            _disposed = false;
            _onLoad = false;
            _genFieldValueList = new List<string>();
            _genIdValueList = new List<Int64>();
            _genFieldValue = string.Empty;
            _genIdValue = 0;
            _parentRowNo = 0;
            _rowExists = false;
            _oldFieldValue = string.Empty;
            _invalidRows = new List<string>();
            _fieldsAdded = new List<string>();
            _rowsAdded = new List<string>();
            _groupFieldValues = new List<string>();
            _lookupInitialized = false;

            // Lists are initialized with capacity to reduce resizing overhead. 
            // If the number of items exceeds the initial capacity, the list grows automatically, 
            // reallocating internal storage as needed.
            Struct = new TStruct();
            Flds = new List<TField>(15);
            AutoGen_Prefix = "";
            AutoGen_Lastno = "";
            Frames = new List<TFrame>(5);
            Fgs = new List<TFg>(3);
            PopGrids = new List<TPopgrid>(3);
            GenMaps = new List<TGenMap>(3);
            MdMaps = new List<TMdMap>(3);
            Scripts = new List<TScript>(2);
            Iframes = new List<TIframe>(2);
            Temp = new TTemp();
            AxpConfig = new TAxpConfig();
            ExprSetList = new List<string>();
            ParentList = new List<string>();
            PopParentList = new List<string>();
            ImgFldList = new List<string>();
            FieldList = new List<FieldRec>(10);
            UserName = "";
            FrameNames = new List<string>();
            PrimaryTable = "";
            Tables = new List<string>();
            PrimaryFields = new List<string>();
            FieldCount = 0;
            Stype = "";
            HasViewImgFlds = false;
            HasImage = false;
            SaveImageInDB = false;
            FileUploadLimit = "";
            HasImgPath = false;
            NoEmptyImgFlds = new List<string>();
            DynamicLabels = "";
            HasAutoGenFields = false;
            GetDependants = true;
            GetPList = false;
            HasPickListFlds = false;
            PrimaryTableId = 0;
            Recid = 0;
            NewRecid = 0;
            LastSavedRecordid = 0;
            AutogenFields = new List<string>();
            SdAutogenFields = new List<string>();
            NewTrans = false;
            RecordIdList = new List<string>();
            CancelledTrans = false;
            CancelRemarks = "";
            ChildTrans = false;
            CompanyName = "";
            MapName = "";
            SourceId = 0;
            PublicSourceId = 0;
            MapParentRecordId = 0;
            SourcePrimaryId = 0;
            BStoreDataAsCSVFile = false;
            NoAutoGenStr = "";
            PrimaryTableName = "";
            SubFrames = new List<string>();
            Ins_del_rows = new List<string>();
            PreparedSqlOperations = new List<SQLCommandBuilder>(2);
            FieldWiseDCs = new Dictionary<string, string>();
        }

        public static async Task<StructDef> CreateAsync(
            string pName,
            Utils common,
            IARMLogTraceService traceService
        )
        {
            if (common == null)
                throw new ArgumentNullException(nameof(common));

            var instance = new StructDef(pName, common, traceService);
            try
            {
                await instance.InitAsync(pName);
                return instance;
            }
            catch
            {
                instance.Dispose();
                throw;
            }
        }

        private async Task InitAsync(string pName)
        {
            try
            {
                _xml = await GetStructure("tstructs", pName);
                if (_xml != null)
                {
                    SetXML(_xml);
                }
                else
                {
                    throw new EntryPointNotFoundException("Structure not found");
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Structure not found: {ex.Message}");
            }
        }

        public async Task ReInitializeHelpers(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _utils = new AxStructDefUtils(_common);
            _traceService = traceService;
        }

        #endregion

        #region Disposal Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    //_xml?.Dispose();

                    // Null out all references
                    Struct = null;
                    Frames = null;
                    Flds = null;
                    Fgs = null;
                    GenMaps = null;
                    MdMaps = null;
                    PopGrids = null;
                    FieldList = null;
                    Scripts = null;
                    Buttons = null;
                    Iframes = null;
                    Temp = null;
                    AxpConfig = null;
                    _frm = null;
                    _common = null;
                    _utils = null;
                    _traceService = null;
                    _parentStructDef = null;
                    _fieldLookup = null;
                    _xml = null;
                    FieldWiseDCs = null;

                }
                catch (Exception ex)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogError($"Dispose error: {ex.Message}");
                }
            }

            _disposed = true;
        }

        private void DisposeCollection<T>(IList<T> collection)
            where T : class
        {
            if (collection == null)
                return;

            foreach (var item in collection.OfType<IDisposable>())
            {
                try
                {
                    item?.Dispose();
                }
                catch (Exception ex)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogError(
                        $"Error disposing collection item: {ex.Message}"
                    );
                }
            }
            collection.Clear();
        }

        #endregion

        #region Load Structure

        private void SetXML(XmlDocument fXML)
        {
            //FrameCount = 0;
            //_contFieldNo = 1;
            FrameNames = [];
            //GridString = "";
            //Tables = "";
            //MastFrameNos = "";

            XLoadStruct(fXML);

            for (int i = 1; i < fXML.DocumentElement.ChildNodes.Count; i++)
            {
                XmlNode n = fXML.DocumentElement.ChildNodes[i];
                string cat = n.Attributes["cat"]?.Value;

                switch (cat)
                {
                    case "dc":
                        XLoadDc(n);
                        break;
                    case "field":
                        XLoadField(n);
                        break;
                    case "fillgrid":
                        XLoadFillGrid(n);
                        break;
                    case "genmap":
                        XLoadGenMap(n);
                        break;
                    case "mdmap":
                        XLoadMdMap(n);
                        break;
                    case "actions":
                        XLoadScripts(n);
                        break;
                    case "comps":
                        XLoadButtons(n);
                        break;
                    case "iframes":
                        XLoadIframes(n);
                        break;
                    case "config":
                        XLoadConfig(n);
                        break;
                };

                if (n.Name == "temp")
                    XLoadTemp(n);

            }
            // [discuss]: Load AMDMap from expression set list
            LoadAdvancedMdMap(ExprSetList);

            //if (PrimaryFields.StartsWith(","))
            //    PrimaryFields = PrimaryFields.Substring(1);

            InitializeFldLookup();

            //GetTableList();
            //UpdateHasGridParents();

            //if (_oldFieldsList != "") CreateOldFiledsList();

            //if (FrameNames.StartsWith(","))
            //    FrameNames = FrameNames.Substring(1);
            //if (MastFrameNos.StartsWith(","))
            //    MastFrameNos = MastFrameNos.Substring(1);

            //if (Axprovider._utils.IsRapidPost)
            //    CheckFields();



            //LDSUniqueFld = LDSGetUniqueField(Transid);
            //LDSPrefixFld = LDSGetField(Transid, "ldsprefix");
            //LDSAutoGenDigitFld = LDSGetField(Transid, "ldsautogendigits");

            GetTstructSpecificDevOptions(Struct.TransId).GetAwaiter().GetResult();
        }

        private void XLoadStruct(XmlDocument rootNode)
        {
            var structNode = rootNode.DocumentElement.ChildNodes[0];
            Struct.NoChange = GetNodeValue(structNode, Consts.xml_NoChange);
            Struct.DocDateField = GetNodeValue(structNode, Consts.xml_DateField);

            XmlNode trackNode = structNode.SelectSingleNode(Consts.xml_Track);
            //Struct.Track = false;
            Struct.Attach = GetNodeValue(structNode, Consts.xml_Attach).Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.HasWorkFlow = GetNodeValue(structNode, Consts.xml_WorkFlow).Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.SaveDraft = GetNodeValue(structNode, Consts.xml_SaveDraft).Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.CachingType = GetNodeValue(structNode, Consts.xml_CachingType);

            Struct.TrackChange = GetAttributeValue(trackNode, "tchng").Equals("Y", StringComparison.OrdinalIgnoreCase);

            // TODO: To look in to later
            //string? uname = _axProvider._utils.username?.ToLower();
            //string? grpname = _axProvider._utils.usergroup?.ToLower();

            if (trackNode?.Attributes["track"] != null)
            {
                if (GetAttributeValue(trackNode, "track").Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    if (GetAttributeValue(trackNode, "ausers").Equals("T", StringComparison.OrdinalIgnoreCase))
                    {
                        Struct.Track = true;
                    }
                    else
                    {
                        XmlNode cnode = trackNode.SelectSingleNode(Consts.xml_TrackUsers);
                        if (cnode != null)
                        {
                            foreach (XmlNode child in cnode.ChildNodes)
                            {
                                string nodeValue = child.InnerText?.ToLower();
                                //if (nodeValue == uname || nodeValue == grpname)
                                if (nodeValue == UserName)
                                {
                                    Struct.Track = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (Struct.Track)
            {
                if (GetAttributeValue(trackNode, "aflds").Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    Struct.TrackAllFields = true;
                }
                else
                {
                    XmlNode cnode = trackNode.SelectSingleNode(Consts.xml_TrackFields);
                    Struct.TrackAllFields = false;
                    if (cnode != null)
                    {
                        foreach (XmlNode child in cnode.ChildNodes)
                        {
                            Struct.TrackFieldsList.Add(child.InnerText);
                        }
                    }
                }
            }

            Struct.DelControl = GetNodeValue(structNode, Consts.xml_DelControl);
            Struct.SaveControl = GetNodeValue(structNode, Consts.xml_SaveControl);
            Struct.TransId = GetNodeValue(structNode, Consts.xml_Name);
            Struct.Caption = GetNodeValue(structNode, Consts.xml_Caption);
            Struct.Purpose = GetNodeValue(structNode, Consts.xml_Purpose);
            Struct.TreeParents = GetNodeValue(structNode, Consts.xml_TreeParents);
            Struct.TreeTables = Struct.TransId + Struct.TreeParents;
            Struct.SchemaName = GetNodeValue(structNode, Consts.xml_Schema)?.Trim();
            Struct.DcNavigation = GetNodeValue(structNode, Consts.xml_DcNavigation);

            XmlNode snode = structNode.SelectSingleNode(Consts.xml_SearchCondition);
            if (snode != null)
            {
                XmlNode lNode = snode.SelectSingleNode("l");
                if (lNode != null)
                {
                    Struct.SearchCondition = GetNodeValue(lNode, "l")?.Trim();
                }
            }

            Struct.ListView = GetNodeValue(structNode, Consts.xml_ListView).Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.Traceability = GetNodeValue(structNode, Consts.xml_Traceability).Equals("True", StringComparison.OrdinalIgnoreCase);

            Struct.Tree = GetAttributeValue(rootNode.DocumentElement, "tree");
            Struct.PrimaryField = GetAttributeValue(rootNode.DocumentElement, "primaryfield");
            Struct.IsPegAttached = GetAttributeValue(rootNode.DocumentElement, "peg").Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.IsAmendmentEnabled = GetAttributeValue(rootNode.DocumentElement, "amendment").Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.HasOutBoundQueues = GetAttributeValue(rootNode.DocumentElement, "axoutboundqueues").Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.HasAutoPrints = GetAttributeValue(rootNode.DocumentElement, "autoprints").Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.HasFormNotify = GetAttributeValue(rootNode.DocumentElement, "formnotify").Equals("True", StringComparison.OrdinalIgnoreCase);
            Struct.CreatedBy = GetAttributeValue(rootNode.DocumentElement, "createdby");
            Struct.ModifiedBy = GetAttributeValue(rootNode.DocumentElement, "updatedby");
            Struct.UpdatedUsername = GetAttributeValue(rootNode.DocumentElement, "updusername");
            Struct.ImportedOn = GetAttributeValue(rootNode.DocumentElement, "importedon");
            Struct.ImportedBy = GetAttributeValue(rootNode.DocumentElement, "importedby");
            Struct.ServerNo = StrToIntZ(GetAttributeValue(rootNode.DocumentElement, "sverno"));
            Struct.ProductId = StrToIntZ(GetAttributeValue(rootNode.DocumentElement, "pid"));
            Struct.VersionNo = GetAttributeValue(rootNode.DocumentElement, "vno");
            Struct.Pdf = GetAttributeValue(rootNode.DocumentElement, "pdf").Equals("Yes", StringComparison.OrdinalIgnoreCase);
            Struct.FitToPage = GetAttributeValue(rootNode.DocumentElement, "fittopage").Equals("Y", StringComparison.OrdinalIgnoreCase);
            Struct.PrintForm = GetAttributeValue(rootNode.DocumentElement, "pform").Equals("Yes", StringComparison.OrdinalIgnoreCase);
            Struct.RunTimeMode = GetAttributeValue(rootNode.DocumentElement, "runtimemod").Equals("Y", StringComparison.OrdinalIgnoreCase);
            Struct.RunTimeStruct = GetAttributeValue(rootNode.DocumentElement, "runtimetstruct").Equals("Y", StringComparison.OrdinalIgnoreCase);
            Struct.CachedSave = GetAttributeValue(rootNode.DocumentElement, "cachedsave").Equals("Y", StringComparison.OrdinalIgnoreCase);
            Struct.DefinitionPage = GetAttributeValue(rootNode.DocumentElement, "defpage").Equals("Y", StringComparison.OrdinalIgnoreCase);
            Struct.Pos = GetAttributeValue(rootNode.DocumentElement, "pos").Equals("Y", StringComparison.OrdinalIgnoreCase);

            //[Note-discuss]: different date format on CreatedOn & updatedOn
           String[] dateFormat = ["dd/MM/yyyy h:mm:ss tt", "dd-MM-yyyy HH:mm:ss"];

            try
            {
                string crtOn = GetAttributeValue(rootNode.DocumentElement, "createdon");
                if (!string.IsNullOrEmpty(crtOn))
                {
                    if (DateTime.TryParseExact(crtOn, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var resultDate))
                        Struct.CreatedOn = resultDate;
                }
                string mdfyOn = GetAttributeValue(rootNode.DocumentElement, "updatedon");
                if (!string.IsNullOrEmpty(mdfyOn))
                {
                    if (DateTime.TryParseExact(mdfyOn, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var resultDate))
                        Struct.ModifiedOn = resultDate;
                }
                //Struct.CreatedOn = DateTime.ParseExact(
                //    GetAttributeValue(rootNode.DocumentElement, "createdon"),
                //    "dd/MM/yyyy hh:mm:ss tt",
                //    CultureInfo.InvariantCulture
                //);

                //Struct.ModifiedOn = DateTime.TryParseExact(
                //    GetAttributeValue(rootNode.DocumentElement, "updatedon"),
                //    "dd/MM/yyyy HH:mm:ss",
                //    CultureInfo.InvariantCulture, out DateTime result
                //);
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error while parsing DateTime: {ex.Message}");
            }
        }

        private void XLoadDc(XmlNode x)
        {
            TFrame frame = new TFrame();

            frame.FrameNo = int.Parse(GetNodeValue(x, Consts.xml_FrameNo));
            frame.Parent = 0;
            frame.PageNo = -1;
            frame.PopParent = -1;
            frame.JsonStr = "";
            frame.MustLoadSql = "";
            frame.TableName = GetNodeValue(x, Consts.xml_Table);

            string s = GetNodeValue(x, Consts.xml_AsGrid);
            //GridString += s.ToLower()[0].ToString();
            frame.AsGrid = GetNodeValue(x, Consts.xml_AsGrid).Equals("True", StringComparison.OrdinalIgnoreCase);
            frame.Popup = GetAttributeValue(x.SelectSingleNode(Consts.xml_PopUp), "pop").Equals("t", StringComparison.OrdinalIgnoreCase);
            frame.AllowEmpty = GetNodeValue(x, Consts.xml_DcAllowEmpty).Equals("True", StringComparison.OrdinalIgnoreCase);
            frame.AllowChange = GetNodeValue(x, Consts.xml_AllowChange).Equals("True", StringComparison.OrdinalIgnoreCase);

            s = GetNodeValue(x, Consts.xml_AddDcRows);
            if (string.IsNullOrEmpty(s?.Trim()))
                frame.AllowAddRow = true;
            else
                frame.AllowAddRow = s.Equals("True", StringComparison.OrdinalIgnoreCase);

            s = GetNodeValue(x, Consts.xml_DeleteDcRows);
            if (string.IsNullOrEmpty(s?.Trim()))
                frame.AllowDeleteRow = true;
            else
                frame.AllowDeleteRow = s.Equals("True", StringComparison.OrdinalIgnoreCase);

            frame.ReadOnly = true;
            //FrameCount++;
            FrameNames.Add(x.Name);

            if (string.IsNullOrEmpty(Struct.SchemaName))
                Tables.Add(frame.TableName);
            else
            {
                CompanyName = Struct.SchemaName;
                Tables.Add(Struct.SchemaName + "." + frame.TableName);
            }

            if (frame.FrameNo == 1)
                PrimaryTable = frame.TableName;

            frame.Caption = GetNodeValue(x, Consts.xml_Caption);
            //frame.Comp = null;
            frame.ActRowDeps = GetAttributeValue(x, "rdf");
            frame.PopIndex = -1;

            //if (frame.Popup)
            //    frame.PopIndex = CreatePopGrid(x);

            frame.StartIndex = -1;
            frame.FieldCount = 0;
            frame.RowCount = 0;
            frame.HasDataRows = false;

            Frames.Add(frame);
            _frm = frame;
            _fieldNo = 1;
        }

        private void XLoadField(XmlNode x)
        {
            if (x.Attributes["dcno"] != null)
                x.Attributes["dcno"].Value = _frm.FrameNo.ToString();

            TField field = new TField
            {
                //Pickfields = null,
                //Pickcaptions = null,
                // PickListMode = TPickListMode.plmNone,
                //PickListDef = "",
                //PickRecFound = false,
                //PickListLevel = 0,
                //WordSearch = false,
                //UsedQuotedStr = false,
                //DataRows = new List<string>(),
                //QParamvalues = new List<string>(),
                //QParamnames = new List<string>(),
                //FieldValList = new Dictionary<string, string>(),
                //OldFieldValList = new Dictionary<string, string>(),
                //IDValList = new List<string>(),
                //OldIDValList = new List<string>(),
                //RecIDList = new List<string>(),
                //OldRecIDList = new List<string>(),
                //LRUList = new List<string>(),
                //RapidQuery = null,
                //RapidQSelectCDSIndex = -1,
                //IDValue = 0,
                //OldIDValue = 0,
                //RecId = 0,
                //HasHyperLink = false,
                //ValueSubmitted = false,
                //SQLText = "",
                //DataType = "",
            };

            // Basic field properties
            field.FieldName = GetNodeValue(x, Consts.xml_Name);

            string saveValue = GetNodeValue(x, Consts.xml_Save);
            if (saveValue == "True")
                field.Tablename = _frm.TableName;
            else
                field.Tablename = "";

            field.SaveValue = saveValue == "True";

            if (field.SaveValue)
            {
                if (x.Attributes["encrypted"] != null)
                    field.EncryptValue = GetAttributeValue(x, "encrypted") == "T";
                else
                    field.EncryptValue = false;
            }
            else
            {
                field.EncryptValue = false;
            }

            field.DataType = GetNodeValue(x, Consts.xml_DataType)?.ToLower();
            if (!string.IsNullOrEmpty(field.DataType))
                field.DataType = field.DataType[0].ToString();

            if (field.DataType == "trackNode")
            {
                if (x.Attributes["customdecimal"] != null)
                    field.CustomDecimal = GetAttributeValue(x, "customdecimal") == "T";
                else
                    field.CustomDecimal = false;
            }

            // Parse width and decimal places
            string widthStr = GetNodeValue(x, Consts.xml_DataWidth);
            if (!string.IsNullOrEmpty(widthStr) && int.TryParse(widthStr, out int width))
                field.Width = width;

            string decStr = GetNodeValue(x, Consts.xml_Dec);
            if (!string.IsNullOrEmpty(decStr) && int.TryParse(decStr, out int dec))
                field.Dec = dec;

            field.CurDec = field.Dec;

            if (field.CustomDecimal && axdecimal != -1)
                field.Dec = axdecimal;

            if (x.Attributes["dbdecimal"] != null)
            {
                if (int.TryParse(GetAttributeValue(x, "dbdecimal"), out int dbDec))
                {
                    field.DBDec = dbDec;
                    if (field.DBDec < field.Dec)
                        field.DBDec = field.Dec;
                }
            }

            field.Caption = GetNodeValue(x, Consts.xml_Caption);
            field.DefCaption = field.Caption;

            if (field.Caption?.Contains("{") == true)
                AddToDynamicLabel("lbl" + field.FieldName);

            field.FrameNo = _frm.FrameNo;
            field.FldUX = new List<string>();

            if (x.Attributes["customdatatype"] != null && GetAttributeValue(x, "customdatatype") != "NA" && GetAttributeValue(x, "customdatatype") != "")
            {
                if (!string.IsNullOrEmpty(GetNodeValue(x, "a69")))
                    field.CustType = GetNodeValue(x, "a69");
                else
                {
                    if (x.Attributes["fieldtype"] != null)
                        field.CustType = GetAttributeValue(x, "fieldtype");
                }
            }
            else
            {
                if (x.Attributes["fieldtype"] != null)
                    field.CustType = GetAttributeValue(x, "fieldtype");
            }

            field.Empty = GetNodeValue(x, Consts.xml_Empty) == "True";

            if (!field.Empty && !string.IsNullOrEmpty(field.Caption))
                field.Caption += "*";

            // Image field handling
            if (field.DataType == "i")
            {
                ImgFldList.Add(field.FieldName + "=i");
                if (!field.Empty)
                {
                    NoEmptyImgFlds.Add(field.FieldName);
                }
                HasImage = true;
            }
            else if (field.FieldName?.ToLower() == "dc" + _frm.FrameNo.ToString().Trim() + "_image")
            {
                ImgFldList.Add(field.FieldName + "=c");
                HasViewImgFlds = true;
            }

            if (field.FieldName?.ToLower() == "dc" + _frm.FrameNo.ToString().Trim() + "_imagepath")
                HasImgPath = true;

            field.NoDuplicate = GetNodeValue(x, Consts.xml_Duplicate) != "True";

            if (field.NoDuplicate)
                PrimaryFields.Add(field.FieldName);

            field.AsGrid = _frm.AsGrid;
            field.ValExprn = -1;

            // Validation expression
            try
            {
                field.Cvalexp = GetNodeValue(x, Consts.xml_VExp);
            }
            catch (Exception)
            {
                XmlNode vexpNode = x.SelectSingleNode(Consts.xml_VExp);
                if (vexpNode?.Attributes["expr"] != null)
                    field.Cvalexp = vexpNode.Attributes["expr"].Value;
            }

            field.OrderNo = _fieldNo;
            field.Contorderno = _contFieldNo;
            _fieldNo++;
            _contFieldNo++;

            field.Hidden = GetNodeValue(x, Consts.xml_Hide) == "True";
            field.Readonly = GetNodeValue(x, Consts.xml_ReadOnly) == "True";

            if (_frm.ReadOnly)
                _frm.ReadOnly = field.Readonly;

            string tabstop = GetNodeValue(x, Consts.xml_Tabstop);
            if (!string.IsNullOrEmpty(tabstop))
            {
                if (tabstop.ToLower() == "true")
                    field.Tabstop = true;
                else if (tabstop.ToLower() == "false")
                    field.Tabstop = false;
            }
            else
            {
                field.Tabstop = !field.Readonly;
            }

            field.AllowChange = _frm.AllowChange;
            field.SetCarry = GetNodeValue(x, Consts.xml_SetCarry) == "True";
            field.ApplyComma = GetNodeValue(x, Consts.xml_ApplyComma) == "True";
            field.OnlyPositive = GetNodeValue(x, Consts.xml_OnlyPositive) == "True";
            field.Searchcol = 1;

            if (string.IsNullOrEmpty(field.Caption))
                field.DispName = field.FieldName;
            else
                field.DispName = field.Caption;

            field.DisplayTotal = GetNodeValue(x, Consts.xml_DispTot) == "True";
            field.ClientValidation = GetNodeValue(x, Consts.xml_CValidate) == "True";
            //field.Listvals = null;
            //field.Deps = null;
            //field.Detmap = null;
            //field.LinkField = "";
            //field.SourceField = "";
            //field.Sourcetable = "";
            //field.SourceTransid = "";
            //field.Suggestive = false;
            //field.Multiline = false;
            //field.FromList = false;
            //field.Refresh = false;
            //field.Autoselect = false;
            //field.HasParams = false;
            //field.DynamicParams = false;
            //field.DetTransid = "";
            //field.DetCondition = "";
            //field.PopIndex = -1;
            //field.DispField = false;
            //field.IsBtnField = false;
            //field.IsParentField = false;
            field.Mask = GetNodeValue(x, Consts.xml_Mask);
            //field.HasGridParents = false;
            //field.Dependents = null;
            //field.DependentTypes = "";
            //field.RefreshOnChange = false;
            //field.Cl_multiselect = false;
            //field.RefOnChngFlds = "";
            //field.FilterCols = new Dictionary<string, string>();
            //field.Parser_Idx = -1;
            //field.FieldValue = "";
            //field.OldFieldValue = "";

            // Get dependencies and detail map
            //GetDeps(ref field, x);
            //GetDetMap(ref field, x);

            string pattern = GetNodeValue(x, Consts.xml_Pattern);
            //if (!_axProvider.bForRapidPost &&
            //    (pattern?.ToLower() == "isalpha" || pattern?.ToLower() == "isnumeric" ||
            //     pattern?.ToLower() == "isemail" || pattern?.ToLower() == "isalphanumeric" ||
            //     pattern?.ToLower() == "isphone" || pattern?.ToLower() == "isurl"))
            //{
            //    field.Pattern = "";
            //}
            //else
            //{
            field.Pattern = pattern;
            //}

            field.Hint = GetNodeValue(x, Consts.xml_Hint);

            string pchar = GetNodeValue(x, Consts.xml_PWordChar);
            if (string.IsNullOrEmpty(pchar))
                field.Pwchar = '\0';
            else
                field.Pwchar = pchar[0];

            field.ModeofEntry = GetNodeValue(x, Consts.xml_MOE)?.ToLower();
            field.SourceKey = GetNodeValue(x, Consts.xml_SourceKey) == "True";

            ConvertMOE(ref field);

            field.Exprn = -1;
            field.Cexp = GetNodeValue(x, Consts.xml_Expr);

            // Process details node
            XmlNode dnode = x.SelectSingleNode(Consts.xml_Details);
            field.SequenceNode = null;
            field.Prefixflds = null;

            if (field.ModeofEntry == "autogenerate")
            {
                HasAutoGenFields = true;
                AutogenFields.Add(field.FieldName + "=" + FieldCount);

                if (dnode != null)
                {
                    field.SequenceNode = dnode.SelectSingleNode(Consts.xml_Sequence);
                    if (field.SequenceNode != null)
                    {
                        field.Prefixflds = new List<string>();
                        foreach (XmlNode child in field.SequenceNode.ChildNodes)
                        {
                            string prefixField = GetNodeValue(child, Consts.xml_PrefixField)?.Trim();
                            if (!string.IsNullOrEmpty(prefixField))
                                field.Prefixflds.Add(prefixField);
                        }
                    }
                }
            }

            if (dnode != null)
            {
                XmlNode sgWidthNode = dnode.SelectSingleNode(Consts.xml_SGWidth);
                if (sgWidthNode != null)
                    field.Sgwidth = sgWidthNode.InnerText;

                string sgHeight = GetNodeValue(dnode, Consts.xml_SGHeight);
                if (!string.IsNullOrEmpty(sgHeight) && int.TryParse(sgHeight, out int sgHeightVal))
                    field.Sgheight = sgHeightVal;
            }

            field.CField = GetNodeValue(dnode, Consts.xml_CField);

            //Stype = "";

            // Mode of entry specific processing
            if (field.ModeofEntry == "select")
            {
                field.SourceField = GetNodeValue(dnode, Consts.xml_Source);

                XmlNode sourceNode = dnode.SelectSingleNode(Consts.xml_Source);
                if (sourceNode?.Attributes["cap"] != null)
                    field.SourceTransid = sourceNode.Attributes["cap"].Value;

                if (
                    field.AsGrid
                    && !string.IsNullOrEmpty(field.SourceField)
                    && !string.IsNullOrEmpty(field.SourceTransid)
                )
                    field.HasHyperLink = true;

                if (sourceNode?.Attributes["stype"] != null)
                {
                    Stype = sourceNode.Attributes["stype"].Value.ToLower();
                    Stype = Stype == "sql" ? "s" : "t";
                }

                if (field.SourceKey && sourceNode?.Attributes["table"] != null)
                {
                    field.Sourcetable = sourceNode.Attributes["table"].Value;
                }
                else if (
                    sourceNode?.Attributes["scol"] != null
                    && !string.IsNullOrEmpty(sourceNode.Attributes["scol"].Value)
                )
                {
                    if (int.TryParse(sourceNode.Attributes["scol"].Value, out int searchCol))
                        field.Searchcol = searchCol;
                }

                // List values
                XmlNode lnode = dnode.SelectSingleNode(Consts.xml_List);
                if (lnode != null && lnode.ChildNodes.Count > 0)
                {
                    Stype = "l";
                    //field.QSelect = _axProvider._utils.GetXDS(null);
                    //field.QSelect.Buffered = true;

                    //if (CheckForNoLDS(field))
                    //    field.QSelect.SetCDSName("nolds");
                    //else
                    //    field.QSelect.SetCDSName(Transid + "_" + field.FieldName);

                    //string filePath = Path.Combine(_axProvider._utils.startpath, "Structures",
                    //                             _axProvider._utils.AppName, Transid + "_" + field.FieldName + ".cds");

                    //if (!File.Exists(filePath) || _axProvider._utils.IsService)
                    //{
                    //    field.QSelect.CDS.CommandText = _axProvider._utils.nullcds;
                    //    field.QSelect.Open();

                    //    if (field.QSelect.CDS.RecordCount > 0)
                    //        field.QSelect.CDS.Delete();

                    //    foreach (XmlNode listItem in lnode.ChildNodes)
                    //        field.QSelect.CDS.AppendRecord(new object[] { listItem.InnerText });

                    //    if (!_axProvider._utils.IsService)
                    //        field.QSelect.CDS.SaveToFile(filePath);
                    //}
                    //else
                    //{
                    //    field.QSelect.CDS.LoadFromFile(filePath);
                    //}

                    foreach (XmlNode listItem in lnode.ChildNodes)
                    {
                        //field.QSelect.CDS.AppendRecord(new object[] { listItem.InnerText });
                        field.fromListValues = field.fromListValues + "," + listItem.InnerText;
                    }

                    // Remove the First comma if it exists
                    if (field.fromListValues.StartsWith(","))
                    {
                        field.fromListValues = field.fromListValues.Substring(1);
                    }

                    field.Searchcol = 0;
                    field.FromList = true;
                    field.Autoselect = GetNodeValue(dnode, Consts.xml_AutoSelect) == "True";
                }
            }
            else if (field.ModeofEntry == "fill")
            {
                field.LinkField = GetNodeValue(dnode, Consts.xml_FParent);
                field.Sourcetable = GetNodeValue(dnode, Consts.xml_SourceTable);
                field.SourceField = GetNodeValue(dnode, Consts.xml_FSource);
                field.Suggestive = GetNodeValue(dnode, Consts.xml_Suggestive) == "True";
            }
            else if (field.ModeofEntry == "accept")
            {
                field.Suggestive = GetNodeValue(dnode, Consts.xml_Suggestive) == "True";
            }

            // SQL processing
            XmlNode lnodeSql = dnode?.SelectSingleNode(Consts.xml_SQL);
            field.SQL = null;
            field.TxtSelection = false;
            field.Combobox = true;

            if (lnodeSql != null)
            {
                // Multi-select handling
                if (lnodeSql.Attributes["mulselect"] != null)
                    field.Cl_multiselect = lnodeSql.Attributes["mulselect"].Value == "true";

                if (lnodeSql.Attributes["mulsel"] != null)
                    field.Separator = lnodeSql.Attributes["mulsel"].Value;
                if (string.IsNullOrEmpty(field.Separator))
                    field.Separator = ",";

                // SQL content
                if (!lnodeSql.HasChildNodes && !string.IsNullOrEmpty(lnodeSql.InnerText?.Trim()))
                {
                    field.SQL = new List<string> { lnodeSql.InnerText.Trim() };
                }
                else if (lnodeSql.HasChildNodes)
                {
                    field.SQL = new List<string>();
                    foreach (XmlNode sqlNode in lnodeSql.ChildNodes)
                    {
                        if (!string.IsNullOrEmpty(sqlNode.InnerText?.Trim()))
                            field.SQL.Add(sqlNode.InnerText.Trim());
                    }
                }

                if (field.SQL != null && field.SQL.Count > 0)
                {
                    //if (_axProvider._utils.AxpLocalDB && lnodeSql.Attributes["fcols"] != null)
                    //    field.FilterCols = new NameValueCollection();
                    //    foreach (var col in lnodeSql.Attributes["fcols"].Value.Split(','))
                    //    {
                    //        if (!string.IsNullOrWhiteSpace(col))
                    //            field.FilterCols.Add(col.Trim(), "");
                    //    }

                    //field.QSelect = _axProvider._utils.GetXDS(null);
                    //field.QSelect.Buffered = true;

                    //if (CheckForNoLDS(field))
                    //    field.QSelect.SetCDSName("nolds");
                    //else
                    //    field.QSelect.SetCDSName(Transid + "_" + field.FieldName);

                    field.SQLText = string.Join(Environment.NewLine, field.SQL);

                    //string sqlText = field.QSelect.CDS.CommandText;
                    if (field.SQLText.Contains("{") || field.SQLText.Contains("(:"))
                    {
                        field.HasParams = true;
                        field.DynamicParams = true;
                    }
                    else
                    {
                        //field.HasParams = field.QSelect.CDS.Params.Count > 0;
                        field.DynamicParams = false;
                    }

                    field.Autoselect = GetNodeValue(dnode, Consts.xml_AutoSelect) == "True";
                    field.Refresh = GetNodeValue(dnode, Consts.xml_Refresh) == "True";

                    field.TxtSelection =
                        lnodeSql.Attributes["txt"] != null
                        && lnodeSql.Attributes["txt"].Value == "t";
                    field.Combobox =
                        lnodeSql.Attributes["txt"] != null
                        && lnodeSql.Attributes["txt"].Value != "t";

                    if (field.TxtSelection)
                    {
                        HasPickListFlds = true;
                        Stype = "p";
                        GetPickLists(ref field);
                    }
                    else if (field.ModeofEntry == "select")
                    {
                        GetDropDownPickLists(ref field);
                    }
                }
            }

            field.SelType = Stype;

            // Search SQL
            XmlNode lnodeSearch = x.SelectSingleNode(Consts.xml_SearchSQL);
            field.SearchSQL = null;
            if (lnodeSearch != null && lnodeSearch.HasChildNodes)
            {
                field.SearchSQL = new List<string>();
                foreach (XmlNode searchNode in lnodeSearch.ChildNodes)
                {
                    if (!string.IsNullOrEmpty(searchNode.InnerText?.Trim()))
                        field.SearchSQL.Add(searchNode.InnerText.Trim());
                }
            }

            // Display detail
            XmlNode lnodeDisplay = x.SelectSingleNode(Consts.xml_displaydetail);
            field.Displaydetail = null;
            if (lnodeDisplay != null && lnodeDisplay.HasChildNodes)
            {
                field.Displaydetail = new List<string>();
                foreach (XmlNode displayNode in lnodeDisplay.ChildNodes)
                {
                    if (!string.IsNullOrEmpty(displayNode.InnerText?.Trim()))
                        field.Displaydetail.Add(displayNode.InnerText.Trim());
                }
                //GetDispHyperDet(ref field);
            }

            // GUI properties
            XmlNode cnode = x.SelectSingleNode(Consts.xml_gui);
            field.Kind = "";
            if (cnode != null)
            {
                //if (cnode.Attributes[Consts.xml_ctype] != null)
                if (cnode.Attributes["ctype"] != null)
                {
                    //string ctype = cnode.Attributes[Consts.xml_ctype].Value;
                    string ctype = cnode.Attributes["ctype"]?.Value;

                    if (ctype == "Check list")
                    {
                        field.ComponentType = "cl";
                        field.Cl_multiselect = true;
                    }
                    else if (ctype == "Check box")
                        field.ComponentType = "cb";
                    else if (ctype == "Radio group")
                    {
                        field.ComponentType = "rg";
                        field.Kind = cnode.Attributes["type"]?.Value ?? "";
                    }
                    else if (ctype == "Radio button")
                        field.ComponentType = "rb";
                    else
                        field.ComponentType = "";

                    field.Separator = ",";
                    //if (ctype == "Check list" && cnode.Attributes[Consts.xml_sep] != null)
                    //    field.Separator = cnode.Attributes[Consts.xml_sep].Value;
                    if (ctype == "Check list" && cnode.Attributes["sep"] != null)
                        field.Separator = cnode.Attributes["sep"].Value;

                    field.CommaSelection = ctype == "Check list";
                }
                else
                {
                    field.CommaSelection = false;
                }

                //field.FontStr = cnode.Attributes[Consts.xml_Font]?.Value ?? "";
                field.FontStr = cnode.Attributes["font"]?.Value ?? "";
                if (field.FontStr == ",,,") field.FontStr = "";

                //field.ColorStr = cnode.Attributes[Consts.xml_color]?.Value ?? "";
                field.ColorStr = cnode.Attributes["color"]?.Value ?? "";
                field.Tlhw = cnode.Attributes[Consts.xml_tlhw]?.Value ?? "";
                //field.Lbltlhw = cnode.Attributes[Consts.xml_lbltlhw]?.Value ?? "";
                field.Lbltlhw = cnode.Attributes["lbltlhw"]?.Value ?? "";
            }
            else
            {
                field.CommaSelection = false;
                field.FontStr = "";
                field.ColorStr = "";
                field.Tlhw = "";
                field.Lbltlhw = "";
            }

            // Set AxRules properties if available
            //if (_axProvider._utils.AxRuleNode != null)
            //    SetAxRulesProps(field.FieldName);

            // Add to frame and update counters
            if (_frm.StartIndex == -1)
                _frm.StartIndex = FieldCount;

            _frm.FieldCount++;

            if (field.FieldName?.ToLower().StartsWith("old") == true)
                _oldFieldsList.Add(field.FieldName);

            Flds.Add(field);
            FieldWiseDCs[field.FieldName] = "dc" + field.FrameNo.ToString();

            if (!field.Readonly && !field.Hidden && field.DataType != "i")
            {
                //LastFieldNo = Flds.Count;
                _frm.LastFieldNo = Flds.Count;
            }

            FieldCount++;
        }

        private void XLoadFillGrid(XmlNode x)
        {
            if (x == null)
                return;

            TFg fillGrid = new TFg
            {
                Fname = x.Name,
                Name = GetNodeValue(x, Consts.xml_FGCaption)
                //Map = new List<string>(),
                //Caps = new List<string>(),
            };

            XmlNode n = x.SelectSingleNode(Consts.xml_FGSQL);
            List<string> sl = null;

            if (n != null && n.HasChildNodes)
            {
                sl = new List<string>();
                foreach (XmlNode child in n.ChildNodes)
                {
                    sl.Add(child.InnerText);
                }
            }
            else if (n != null && !string.IsNullOrEmpty(n.InnerText?.Trim()))
            {
                sl = new List<string> { n.InnerText.Trim() };
            }

            if (sl != null && !string.IsNullOrEmpty(string.Join("", sl).Trim()))
            {
                //fillGrid.Q = _axProvider._utils.GetXDS(null);
                //fillGrid.Q.SetCDSName("nolds_" + Transid + "_" + fillGrid.Fname);
                //fillGrid.Q.Buffered = true;
                //fillGrid.Q.CDS.CommandText = string.Join(Environment.NewLine, sl);

                //string sqlText = fillGrid.Q.CDS.CommandText;
                string sqlText = string.Join(Environment.NewLine, sl);

                if (sqlText.Contains("{") || sqlText.Contains("(:"))
                {
                    fillGrid.SQLText = sqlText;
                    fillGrid.HasParams = true;
                    fillGrid.DynamicParams = true;
                }
                else
                {
                    fillGrid.SQLText = "";
                    //fillGrid.HasParams = fillGrid.Q.cds.Params.Count > 0;
                    fillGrid.DynamicParams = false;
                }

                if (n.Attributes["plist"] != null)
                    fillGrid.ParamNames = n.Attributes["plist"].Value;
            }
            //else
            //{
            //    //fillGrid.Q = null;
            //}

            sl?.Clear();

            fillGrid.FromIView = GetNodeValue(x, Consts.xml_IView) == "True";
            fillGrid.MultiSelect = GetNodeValue(x, Consts.xml_Multi) == "True";
            fillGrid.AutoShow = GetNodeValue(x, Consts.xml_AutoShow) == "True";
            fillGrid.ValidateRow = GetNodeValue(x, Consts.xml_FGValidate) == "True";
            fillGrid.ExecuteOnSave = GetNodeValue(x, Consts.xml_ExeOnSave) == "True";
            fillGrid.Firmbind = GetNodeValue(x, Consts.xml_FirmBind) == "True";
            fillGrid.SelectAllRows = GetNodeValue(x, Consts.xml_SelectAllRows) == "True";

            string target = GetNodeValue(x, Consts.xml_FGTarget);
            fillGrid.TargetDcName = target;
            if (target.Length > 2)
                target = target.Substring(2);

            fillGrid.TargetFrame = StrToIntZ(target);

            string source = GetNodeValue(x, Consts.xml_SourceDC);
            fillGrid.SourceDcName = source;
            if (source.Length > 2)
                source = source.Substring(2);

            fillGrid.SourceFrame = StrToIntZ(source);

            fillGrid.SelectOn = GetNodeValue(x, Consts.xml_SelectOn);

            string addRows = GetNodeValue(x, Consts.xml_AddRows)?.Trim();
            if (string.IsNullOrEmpty(addRows))
                addRows = "Add only when grid is empty";

            fillGrid.CAddRows = addRows;

            if (string.IsNullOrEmpty(addRows))
            {
                fillGrid.AddRows = 1;
            }
            else
            {
                if (addRows == "Add only when grid is empty")
                    fillGrid.AddRows = 1;
                else if (addRows == "Initialize grid and add")
                    fillGrid.AddRows = 2;
                else if (
                    addRows == "Append rows to grid"
                    || addRows == "Append selected rows to grid"
                )
                    fillGrid.AddRows = 3;
                else
                    fillGrid.AddRows = 1;
            }

            string footerStr = GetNodeValue(x, Consts.xml_FooterStr);
            if (!string.IsNullOrEmpty(footerStr?.Trim()))
                fillGrid.FooterStr = footerStr;

            fillGrid.VExp = GetNodeValue(x, Consts.xml_FGVExp)?.Trim();

            n = x.SelectSingleNode(Consts.xml_FMap);
            fillGrid.Colwidths = "";

            if (n != null)
            {
                foreach (XmlNode child in n.ChildNodes)
                {
                    string mapItem = child.Name + "=" + child.InnerText;
                    fillGrid.Map.Add(mapItem);

                    if (child.Attributes["width"] != null)
                        fillGrid.Colwidths += child.Attributes["width"].Value + ",";

                    if (child.Attributes["cap"] != null)
                        fillGrid.Caps.Add(child.InnerText + "=" + child.Attributes["cap"].Value);
                }
            }

            if (n?.Attributes["gfld"] != null)
                fillGrid.Groupfield = n.Attributes["gfld"].Value;

            Fgs.Add(fillGrid);
            //fg = fillGrid;
        }

        private void XLoadGenMap(XmlNode x)
        {
            if (GetNodeValue(x, Consts.xml_Active) == "True")
            {
                TGenMap genMap = new TGenMap
                {
                    Name = GetNodeValue(x, Consts.xml_Name),
                    Caption = GetNodeValue(x, Consts.xml_Caption),
                    TargetTransId = GetNodeValue(x, Consts.xml_GenTarget),
                    TargetCaption = GetAttributeValue(x.SelectSingleNode(Consts.xml_GenTarget), "tcap") ?? "",
                    SourceDC = GetNodeValue(x, Consts.xml_GenParent),
                    GenControlField = GetNodeValue(x, Consts.xml_GenControl),
                    //AllowEmpty = GetNodeValue(x, Consts.xml_Active) == "True",
                    AllowEmpty = true,
                    Condition = GetNodeValue(x, Consts.xml_GenCField),
                    OnPostEvent = GetNodeValue(x, Consts.xml_OnPost),
                    GenTransId = GetNodeValue(x, Consts.xml_GenTransId),
                    OnApproveEvent = GetNodeValue(x, Consts.xml_OnApprove),
                    OnRejectEvent = GetNodeValue(x, Consts.xml_OnReject),
                    AllowAddRow = GetNodeValue(x, Consts.xml_AddDcRows) == "True",
                    AllowDeleteRow = GetNodeValue(x, Consts.xml_DeleteDcRows) == "True",
                    Purpose = GetNodeValue(x, Consts.xml_Purpose)
                };

                // Load field mappings from GMap node
                XmlNode fieldMappingsNode = x.SelectSingleNode(Consts.xml_GMap);
                if (fieldMappingsNode != null)
                {
                    foreach (XmlNode fieldNode in fieldMappingsNode.ChildNodes)
                    {
                        GenMapField fieldMapping = new GenMapField
                        {
                            //TargetField = fieldNode.InnerText,
                            TargetField = fieldNode.Name,
                            TargetCaption = GetAttributeValue(fieldNode, "tcap"),
                            //SourceField = GetAttributeValue(fieldNode, "scap"),
                            SourceField = fieldNode.InnerText,
                            SourceCaption = GetAttributeValue(fieldNode, "scap"),
                            TargetRow = int.TryParse(GetAttributeValue(fieldNode, "trow"), out int trow) ? trow : 1,
                            SourceRow = int.TryParse(GetAttributeValue(fieldNode, "srow"), out int srow) ? srow : 1,
                            Control = GetAttributeValue(fieldNode, "control"),
                            ControlCaption = GetAttributeValue(fieldNode, "controlcap"),
                            Group = GetAttributeValue(fieldNode, "group"),
                            Type = GetAttributeValue(fieldNode, "type") ?? "field",
                            Order = GetAttributeValue(fieldNode, "ord"),
                            IsID = GetAttributeValue(fieldNode, "id") == "True",
                            SourceAsGrid = GetAttributeValue(fieldNode, "sasgrid") == "true",
                            IdValue = GetAttributeValue(fieldNode, "idvalue") ?? "0",
                        };

                        genMap.FieldMappings.Add(fieldMapping);
                    }
                }

                GenMaps.Add(genMap);
            }
        }

        private void XLoadMdMap(XmlNode x)
        {
            try
            {
                var extended = GetNodeValue(x, Consts.xml_Extended).ToLower();
                _map = x;

                if (extended == "false")
                {
                    LoadRegularMdMap(x);
                }
                else
                {
                    LoadExtendedMdMap(x);
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"Error loading XloadMdMap: {ex.Message}");
            }
        }

        private async Task LoadRegularMdMap(XmlNode map)
        {
            var mdMap = new TMdMap();
            int sourceFrameNo = GetSourceFrameNo(map);
            string masterTable = GetNodeValue(map, Consts.xml_MTable).ToLower();
            //mdMap.CompanyName = !string.IsNullOrEmpty(masterTable) ? await GetCompanyNameAsync(masterTable) : "";

            // Handle company prefix
            if (!string.IsNullOrEmpty(mdMap.CompanyName))
            {
                int cPos = masterTable.IndexOf('.');
                if (cPos > 0)
                {
                    masterTable = masterTable.Substring(cPos + 1).Trim();
                }
            }

            string msField = GetNodeValue(map, Consts.xml_MSField).ToLower();
            if (msField == "default")
            {
                msField = masterTable + "id";
            }

            string dsField = GetNodeValue(map, Consts.xml_DSField).ToLower();
            string controlField = GetNodeValue(map, Consts.xml_MDControl);

            // Parse decimal places
            int dec = 0;
            if (int.TryParse(GetNodeValue(map, Consts.xml_MasterFieldDec), out int parsedDec))
            {
                dec = parsedDec;
            }

            // Parse operation type
            OperationType operationType = ParseOperationType(GetNodeValue(map, Consts.xml_Type));

            // Check if similar MDMap already exists
            int existingIndex = GetMdMapIndex(Struct.TransId.ToLower(), masterTable, msField, dsField, controlField);

            if (existingIndex == -1)
            {
                mdMap.TransId = Struct.TransId.ToLower();
                mdMap.Table = masterTable;
                mdMap.CompanyName = CompanyName;
                mdMap.ControlField = controlField;
                mdMap.InitOnDel = GetNodeValue(map, Consts.xml_InitOnDel).ToLower() == "true";
                mdMap.Append = GetNodeValue(map, Consts.xml_Append).ToLower() == "true";
                mdMap.PostOnApprove = GetNodeValue(map, Consts.xml_OnApprove).ToLower() == "true";
                mdMap.PostOnReject = GetNodeValue(map, Consts.xml_OnReject).ToLower() == "true";
                mdMap.MasterSearchField = msField;
                mdMap.DetailSearchField = dsField;
                mdMap.SourceFrame = sourceFrameNo;

                MdMaps.Add(mdMap);
            }
            else
            {
                mdMap = MdMaps[existingIndex];
            }

            // Add field mapping
            string mField = GetNodeValue(map, Consts.xml_MField).ToLower();
            string dField = GetNodeValue(map, Consts.xml_DField).ToLower();

            var fieldMapping = new MdMapField
            {
                SourceField = dField,
                TargetField = mField,
                Operation = operationType,
                DecimalPlaces = dec
            };

            mdMap.FieldMappings.Add(fieldMapping);
        }

        private void LoadExtendedMdMap(XmlNode map)
        {
            XmlNode mtext = map.SelectSingleNode(Consts.xml_MapText);
            if (mtext == null) return;

            List<string> mapLines = new List<string>();

            foreach (XmlNode child in mtext.ChildNodes)
            {
                mapLines.Add(child.InnerText);
            }

            if (!string.IsNullOrWhiteSpace(string.Join("", mapLines)))
            {
                mapLines.Insert(0, "{mdmap}");
                LoadAdvancedMdMap(mapLines);
            }
        }

        private void LoadAdvancedMdMap(List<string> expList)
        {
            bool inMdMapBlock = false;
            TMdMap currentMap = null;

            foreach (string s in expList)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (s.StartsWith("//")) continue;

                if (inMdMapBlock)
                {
                    if (s.StartsWith("{"))
                    {
                        inMdMapBlock = false;
                    }
                    else
                    {
                        ParseMdMap(s, currentMap);
                    }
                }

                if (s.StartsWith("{mdmap"))
                {
                    inMdMapBlock = true;
                    currentMap = new TMdMap
                    {
                        //Append = false,
                        //InitOnDel = false,
                        //Tree = false,
                        PostOnApprove = GetNodeValue(_map, Consts.xml_OnApprove).ToLower() == "true",
                        PostOnReject = GetNodeValue(_map, Consts.xml_OnReject).ToLower() == "true"
                    };
                    MdMaps.Add(currentMap);
                }
            }
        }

        private void XLoadScripts(XmlNode x)
        {
            for(int i = 0; i < x.ChildNodes.Count; i++)
            {
                XmlNode scriptNode = x.ChildNodes[i];

                var script = new TScript
                {
                    Key = scriptNode.Name,
                    Caption = GetAttributeValue(scriptNode, "cap"),
                    Type = GetAttributeValue(scriptNode, "type"),
                    Apply = GetAttributeValue(scriptNode, "apply"),
                    Applyon = GetAttributeValue(scriptNode, "applyon"),
                    SName = GetAttributeValue(scriptNode, "sname"),
                    Scripts = GetAttributeValue(scriptNode, "scripts").Equals("yes", StringComparison.OrdinalIgnoreCase),
                    Description = GetAttributeValue(scriptNode, "desc"),
                    FailMessage = GetAttributeValue(scriptNode, "failmessage"),
                    Validate = GetAttributeValue(scriptNode, "validate").Equals("yes", StringComparison.OrdinalIgnoreCase),
                    FormControl = GetAttributeValue(scriptNode, "formcontrol").Equals("true", StringComparison.OrdinalIgnoreCase),
                    BreakOnError = GetAttributeValue(scriptNode, "breakonerror").Equals("yes", StringComparison.OrdinalIgnoreCase),
                    LocalNumber = StrToIntZ(GetAttributeValue(scriptNode, "localnum"))
                };

                for (int j = 0; j < scriptNode.ChildNodes.Count; j++)
                {
                    XmlNode ruleNode = scriptNode.ChildNodes[j];

                    var rule = new ScriptRule
                    {
                        Key = ruleNode.Name,
                        Id = GetAttributeValue(ruleNode, "id"),
                        Op = StrToIntZ(GetAttributeValue(ruleNode, "op")),
                        Task = GetAttributeValue(ruleNode, "task"),
                        Type = GetAttributeValue(ruleNode, "type"),
                        SName = GetAttributeValue(ruleNode, "sname"),
                        Apply = GetAttributeValue(ruleNode, "apply"),
                        ApplyOn = GetAttributeValue(ruleNode, "applyon")
                    };
                    
                    for (int k = 0; k < ruleNode.ChildNodes.Count; k++)
                    {
                        XmlNode paramNode = ruleNode.ChildNodes[k];

                        var param = new ScriptParam
                        {
                            Key = paramNode.Name,
                            Op = StrToIntZ(GetAttributeValue(ruleNode, "op")),
                            Task = GetAttributeValue(ruleNode, "task")
                        };

                        foreach (XmlNode exp in paramNode.ChildNodes)
                        {
                            param.ExprSet.Add(exp.InnerText);
                        }

                        rule.Params.Add(param);
                    }

                    script.Rules.Add(rule);
                }
                Scripts.Add(script);
            }
        }

        private void XLoadButtons(XmlNode compNode)
        {
            int capacity = StrToIntZ(GetAttributeValue(compNode, "btn"));

            if (capacity > 0) 
                Buttons = new List<TButton>(capacity);
            else 
                Buttons = new List<TButton>(20);

            for(int i = 0; i < compNode.ChildNodes.Count; i++)
            {
                if(compNode.ChildNodes[i].Attributes["cat"]?.Value == "btn")
                {
                    XmlNode buttonNode = compNode.ChildNodes[i];

                    var button = new TButton
                    {
                        Key = GetAttributeValue(buttonNode, "key"),
                        Caption = GetAttributeValue(buttonNode, "caption"),
                        Parent = GetAttributeValue(buttonNode, "parent"),
                        SName = GetAttributeValue(buttonNode, "sname"),
                        Tlhw = GetAttributeValue(buttonNode, "tlhw"),
                        Flat = GetAttributeValue(buttonNode, "flat").Equals("True", StringComparison.OrdinalIgnoreCase),
                        Image = GetAttributeValue(buttonNode, "img"),
                        Position = GetAttributeValue(buttonNode, "position"),
                        Container = GetAttributeValue(buttonNode, "container"),
                        Hint = GetAttributeValue(buttonNode, "hint"),
                        Action = GetAttributeValue(buttonNode, "action"),
                        Task = GetAttributeValue(buttonNode, "task"),
                        Scut = GetAttributeValue(buttonNode, "scut"),
                        Definition = GetAttributeValue(buttonNode, "def"),
                        Font = GetAttributeValue(buttonNode, "font"),
                        Footer = GetAttributeValue(buttonNode, "footer").Equals("True", StringComparison.OrdinalIgnoreCase),
                        Visible = GetAttributeValue(buttonNode, "visible").Equals("True", StringComparison.OrdinalIgnoreCase),
                        Api = GetAttributeValue(buttonNode, "api")
                    };

                    foreach (XmlNode bnode in buttonNode)
                    {
                        if (bnode.Name == "icon")
                        {
                            button.Icon.Text = GetNodeValue(bnode, "text");
                            button.Icon.AddClass = GetNodeValue(bnode, "addClass");
                        }
                        if (bnode.Name.StartsWith("pop"))
                        {
                            var pop = new ButtonPop
                            {
                                Caption = GetAttributeValue(bnode, "caption"),
                                Action = GetAttributeValue(bnode, "action"),
                                Task = GetAttributeValue(bnode, "task")
                            };

                            button.Pops.Add(pop);
                        }
                       
                    }

                    Buttons.Add(button);
                }
            }
        }

        private void XLoadIframes(XmlNode x)
        {
            string layout = GetAttributeValue(x, "layout");

            foreach (XmlNode iframeNode in x.ChildNodes)
            {
                var iframe = new TIframe
                {
                    Layout = layout,
                    Key = iframeNode.Name,
                    Align = GetAttributeValue(iframeNode, "align"),
                    Tlhw = GetAttributeValue(iframeNode, "tlhw"),
                    Hp = StrToIntZ(GetAttributeValue(iframeNode, "hp")),
                    Pop = GetAttributeValue(iframeNode, "pop").Equals("Yes", StringComparison.OrdinalIgnoreCase)
                };

                var pages = iframeNode.SelectSingleNode("pages");
                if (pages != null)
                {
                    foreach (XmlNode page in pages)
                    {
                        iframe.Pages.Add($"{page.Name}:{page.InnerText}");
                    }
                }

                Iframes.Add(iframe);
            }
        }
        
        private void XLoadTemp(XmlNode tempNode)
        {
            Temp.MenuPosition = GetNodeValue(tempNode, "menupos");
            Temp.MenuList = GetNodeValue(tempNode, "menulist")?.Split(",").ToList();
            Temp.Listview = GetNodeValue(tempNode, "listview").Equals("true", StringComparison.OrdinalIgnoreCase);
            Temp.Layout = GetNodeValue(tempNode, "layout");
            Temp.FormCaption = GetNodeValue(tempNode, "fcaption");
            Temp.DefinitionPage = GetNodeValue(tempNode, "defpage").Equals("true", StringComparison.OrdinalIgnoreCase);
            Temp.IncludePage = GetNodeValue(tempNode, "includepage").Equals("T", StringComparison.OrdinalIgnoreCase);
            Temp.ChangedDc = GetNodeValue(tempNode, "changeddc")?.Split(",").ToList();

            Temp.MenuList?.RemoveAll(m => string.IsNullOrEmpty(m));
            Temp.ChangedDc?.RemoveAll(m => string.IsNullOrEmpty(m));
        }
        
        private void XLoadConfig(XmlNode configNode)
        {
            AxpConfig.ConfigName = GetNodeValue(configNode, "CONFIGNAME");
            AxpConfig.ConfigValue = GetNodeValue(configNode, "CVALUE");
            AxpConfig.FormName = GetNodeValue(configNode, Consts.xml_Name);
            AxpConfig.FormCaption = GetNodeValue(configNode, Consts.xml_Caption);            
        }

        public static async Task<StructDef> LoadStructure(Utils _common, string transId, List<string> roles, IARMLogTraceService traceService, bool refreshCache = false)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                // Add all necessary converters
                //Converters = new List<JsonConverter>
                //{
                    //new XmlNodeUniversalConverter(),
                    //new Newtonsoft.Json.Converters.XmlNodeConverter()
                //}
            };
            try
            {
                var cacheKey = _common.GenerateRedisKeyString(new string[] { _common.GetSchemaName(), transId, "ARM-AX-STRUCTDEF" });
                
                if (!refreshCache)
                {
                    var structDef = await _common.GetStructDefObjFromCache(cacheKey);
                    if (!string.IsNullOrEmpty(structDef))
                    {
                        var cachedStructDefObj = JsonConvert.DeserializeObject<StructDef>(structDef, settings);
                        cachedStructDefObj.ReInitializeHelpers(_common, traceService);
                        return cachedStructDefObj;
                    }
                }

                if (_common._dbHelper == null)
                    await _common.OpenDBConnectionAsync();

                var structDefObj = await StructDef.CreateAsync(transId, _common, traceService);

                var sqlList = await GetDCWiseSQLsFromDB(_common, transId);
                if (sqlList == null || sqlList.data == null || sqlList.data.Rows.Count == 0)
                {
                    return null;
                }
                else
                {
                    for (int i = 0; i < structDefObj.Frames.Count; i++)
                    {
                        var dcName = "dc" + structDefObj.Frames[i].FrameNo.ToString();
                        var (sql, dcGroup) = GetDCSQLProperties(dcName, sqlList.data);
                        structDefObj.Frames[i].DcSQL = sql;
                        structDefObj.Frames[i].DcSQLGroup = dcGroup;
                    }
                }

                await _common.SetPageTitleToCache("Tstruct", transId, structDefObj.Struct.Caption);
                await _common.SetStructDefObjToCache(cacheKey, JsonConvert.SerializeObject(structDefObj, settings));
                return structDefObj;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static async Task<SQLResult> GetDCWiseSQLsFromDB(Utils _common, string transId)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            string sql = string.Empty;

            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.AXGET_DBGET_MSSQL;
                string[] paramName = { "@ptransid", "@precordid" };
                DbType[] paramType = { DbType.String, DbType.Int64 };
                object[] paramValue = { transId, 0 };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
            else
            {
                sql = Constants_SQL.AXGET_DBGET.ToString();
                
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.AXGET_DBGET_ORACLE.ToString();

                string[] paramName = { "@transid", "@recordid" };
                DbType[] paramType = { DbType.String, DbType.Int64 };
                object[] paramValue = { transId, 0 };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
        }

        private static (string, List<string>) GetDCSQLProperties(string dcName, DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                var dcNames = row["dcno"].ToString().Split(',').ToList();
                foreach (var dc in dcNames)
                {
                    if (dc.Trim().Equals(dcName, StringComparison.OrdinalIgnoreCase))
                    {
                        return (row["sqltext"].ToString(), dcNames);
                    }
                }
            }

            return ("", new List<string>());
        }

        #endregion

        #region Public Methods

        /// <param name="fieldname">Name of the field to find (case-insensitive)</param>
        /// <returns>TField object if found, null if field doesn't exist</returns>
        /// <example>GetField("CustomerName") returns CustomerName field definition</example>
        public TField? GetField(string fieldName)
        {
            InitializeFldLookup();
            _fieldLookup.TryGetValue(fieldName, out var field);
            return field;
        }

        /// <param name="fieldName">Field name to update value for</param>
        /// <param name="rowNo">Row number (1 for non-grid fields)</param>
        /// <param name="value">New field value to set</param>
        /// <param name="oldValue">Previous field value for change tracking</param>
        /// <param name="idValue">New ID value for the field</param>
        /// <param name="oldIdValue">Previous ID value for change tracking</param>
        /// <param name="recordId">Record identifier for data mapping</param>
        /// <example>SubmitValue("Amount", 1, "100.50", "90.00", 123, 122, 1001)</example>
        public void SubmitValue(
            string fieldName,
            int rowNo,
            string value,
            string oldValue,
            Int64 idValue,
            Int64 oldIdValue,
            Int64 recordId,
            bool convertClientDateToDbFormat = false
        ) //, string encryptValue = "")
        {
            var field = GetField(fieldName);

            if (field is null)
            {
                //Check if it's a company-specific field
                //var companyField = $",{(CompanyName + fieldName[..^2]).ToLower()},";

                //if (!Tables.Contains(companyField, StringComparison.CurrentCultureIgnoreCase))
                //    return;

                SetFieldValue(fieldName, "", "", "", value, "", rowNo, 0, 0, 0, 0, false, convertClientDateToDbFormat);
                return;
            }

            // Use pattern matching and simplified null check
            rowNo = field is { AsGrid: false } ? 1 : rowNo;

            if (field.EncryptValue)
            {
                value = AXPEncryptData.Encrypt(value);
            }

            SetFieldValue(
                fieldName,
                field.DataType,
                field.CustType,
                field.Tablename,
                //value, oldValue, rowNo, idValue, oldIdValue,
                value,
                field.OldFieldValue,
                rowNo,
                idValue,
                field.OldIDValue, // onSubmitValue should not change the oldField value
                field.FrameNo,
                recordId,
                field.SourceKey,
                convertClientDateToDbFormat
            ); // encryptValue);
        }

        public void SubmitValue(string fieldName, int rowNo, string value, Int64 idvalue = 0)
        {
            var field = GetField(fieldName);

            if (field is null)
            {
                SetFieldValue(fieldName, "", "", "", value, "", rowNo, idvalue, 0, 0, 0, false);
                return;
            }

            rowNo = field is { AsGrid: false } ? 1 : rowNo;

            if (field.EncryptValue)
            {
                value = AXPEncryptData.Decrypt(value);
            }

            SetFieldValue(
                fieldName,
                field.DataType,
                field.CustType,
                field.Tablename,
                value,
                field.OldFieldValue,
                rowNo,
                idvalue,
                field.OldIDValue, // onSubmitValue should not change the oldField value
                field.FrameNo,
                0,
                field.SourceKey
            ); // encryptValue);
        }

        /// <param name="FieldName">Name of the field to set value for</param>
        /// <param name="DataType">Data type of the field (n=numeric, d=date, etc.)</param>
        /// <param name="TableName">Database table name for the field</param>
        /// <param name="Value">New value to set for the field</param>
        /// <param name="OldValue">Previous value for change tracking</param>
        /// <param name="RowNo">Row number in the data container</param>
        /// <param name="IdValue">ID value associated with the field</param>
        /// <param name="OldIdValue">Previous ID value for change tracking</param>
        /// <param name="FrameNo">Frame number containing the field</param>
        /// <param name="RecordId">Unique record identifier</param>
        /// <param name="SourceKey">Whether field is a source key/normalized</param>
        /// <example>SetFieldValue("CustomerCode", "s", "Customers", "CUST001", "", 1, 1001, 0, 1, 5001, true)</example>
        public void SetFieldValue(
            string FieldName,
            string DataType,
            string CustomDataType,
            string TableName,
            string Value,
            string OldValue,
            int RowNo,
            Int64 IdValue,
            Int64 OldIdValue,
            int FrameNo,
            Int64 RecordId,
            bool SourceKey,
            bool convertClientDateToDbFormat = false
        ) //, string encrypt_value = "")
        {
            DataType = DataType.ToLower();

            // Handle date validation
            if (DataType == "d")
            {
                if (!string.IsNullOrEmpty(Value) && convertClientDateToDbFormat)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat(CustomDataType : {CustomDataType}, Value : {Value}, clientDateFormat: {clientDateFormat})");

                    try
                    {
                        Value = _common.ConvertClientDateToDBDateFormat(CustomDataType, Value, clientDateFormat);

                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"ConvertClientDateToDBDateFormat response, Value : {Value}");
                    }
                    catch (Exception ex)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"ConvertClientDateToDBDateFormat Exception, {JsonConvert.SerializeObject(ex)}");

                        //Value = "";
                        throw new Exception(ex.Message);
                    }
                }

                if (!string.IsNullOrEmpty(OldValue) && convertClientDateToDbFormat)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat(CustomDataType : {CustomDataType}, OldValue : {OldValue}, clientDateFormat: {clientDateFormat})");

                    try
                    {
                        OldValue = _common.ConvertClientDateToDBDateFormat(CustomDataType, OldValue, clientDateFormat);
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"ConvertClientDateToDBDateFormat response, Value : {OldValue}");
                    }
                    catch (Exception ex)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"ConvertClientDateToDBDateFormat Exception, {JsonConvert.SerializeObject(ex)}");
                        OldValue = "";
                    }
                }
            }

            //if (DataType == "i")
            //    return;

            int p;
            if (RowNo == -1)
                p = -1;
            else
                p = GetIndex(FieldName, TableName, RowNo);

            FieldRec frec;
            string ov;

            if (p == -1)
            {
                frec = new FieldRec
                {
                    DataType = DataType,
                    FrameNo = FrameNo,
                    //RecordId = RecordId,
                    FieldName = FieldName,
                    TableName = TableName.ToUpper(),
                    RowNo = RowNo,
                    SourceKey = SourceKey,
                    AutoValue = false,
                    //OldIdValue = OldIdValue,
                    ParentRowNo = 0,
                    ZeroValue = true,
                    OldRow = 0,
                };

                // Set PrimaryTable
                if (TableName.ToLower() == PrimaryTableName.ToLower())
                    frec.PrimaryTable = "a";
                else
                    frec.PrimaryTable = SubFrames.Contains(FrameNo.ToString()) ? "x" : "b";

                if (_onLoad)
                {
                    frec.OldValue =
                        (DataType == "n" && string.IsNullOrEmpty(OldValue)) ? "0" : OldValue;
                    frec.OldIdValue = OldIdValue;
                    frec.RecordId = RecordId;
                }

                // Get field reference
                TField fp = GetField(FieldName);
                frec.Fld = fp;
                frec.Orders = fp?.OrderNo ?? 0;

                FieldList.Add(frec);
                p = FieldList.Count - 1;

                // Handle grid insertion tracking
                if (fp != null && fp.AsGrid)
                {
                    //if (Quickload)
                    //{
                    //    if (
                    //        !string.IsNullOrEmpty(Value)
                    //        || (DataType == "n" && Value != "0" && Value != "0.00")
                    //    )
                    //    {
                    //        InsertedRowNoInfo(fp.FrameNo, RowNo);
                    //    }
                    //}
                    //else
                    {
                        InsertedRowNoInfo(fp.FrameNo, RowNo);
                    }
                }
            }
            else
            {
                frec = FieldList[p];
            }

            ov = frec.Value;

            // Set the actual value with type-specific handling
            if (DataType == "d" && IsDateEmpty(Value))
            {
                frec.Value = "";
            }
            else if (DataType == "n")
            {
                if (string.IsNullOrEmpty(Value))
                {
                    frec.Value = "0";
                    frec.ZeroValue = false;
                }
                else
                {
                    frec.Value = RemoveCommas(Value);
                    frec.ZeroValue = true;
                }
            }
            else
            {
                frec.Value = Value;
            }

            // Handle auto value tracking
            if (frec.AutoValue && ov != frec.Value)
                frec.AutoValue = false;

            // Web data handling
            //if (WebData)
            //{
            //    frec.WebValue = Value;
            //    frec.EncryptValue = encrypt_value;
            //}

            frec.ClientNotify = _clientNotify;
            frec.IdValue = IdValue;

            // Update field data rows
            if (frec.Fld != null && !frec.Fld.DataRows.Contains(p.ToString()))
                frec.Fld.DataRows.Add(p.ToString());

            if (frec.Fld != null)
            {
                SetFieldValue(frec.Fld, RowNo, Value, OldValue, IdValue, OldIdValue, RecordId);
            }

            // Currency decimal handling
            //if (!string.IsNullOrWhiteSpace(Value) && RowNo > 0 &&
            //    FieldName.ToLower() == "axpcurrencydec")
            //{
            //    SetCurrencyDec(Value, RowNo);
            //}
        }

        public string RemoveCommas(string s)
        {
            while (true)
            {
                int k = s.IndexOf(LocThousandSeparator);
                if (k == -1)
                    break;
                s = s.Remove(k, 1);
            }
            return s;
        }

        /// <param name="tempFld">TField object to update values for</param>
        /// <param name="rowNo">Row number to set value for</param>
        /// <param name="fVal">New field value to set</param>
        /// <param name="fOldVal">Previous field value for tracking</param>
        /// <param name="idValue">New ID value for the field</param>
        /// <param name="oldIdValue">Previous ID value for tracking</param>
        /// <param name="recordId">Record identifier for data mapping</param>
        /// <example>SetFieldValue(customerField, 1, "John Doe", "Jane Smith", 1001, 1000, 5001)</example>
        public void SetFieldValue(
            TField tempFld,
            int rowNo,
            string fVal,
            string fOldVal,
            Int64 idValue,
            Int64 oldIdValue,
            Int64 recordId
        )
        {
            if (tempFld.FieldName == null)
                return;

            tempFld.FieldValue = fVal;
            tempFld.IDValue = idValue;

            // It's handled through SubmitValue & OnSubmitValue
            if (_onLoad)
            {
                tempFld.OldFieldValue = fOldVal;
                tempFld.OldIDValue = oldIdValue;
                tempFld.RecId = recordId;
            }

            tempFld.ValueSubmitted = true;

            if (tempFld.AsGrid)
            {
                string rowKey = rowNo.ToString();
                int index = FindIndexInList(tempFld.FieldValList, rowKey);

                if (index < 0)
                {
                    //tempFld.FieldValList.Add($"{rowKey}={fVal}");
                    tempFld.FieldValList.Add(rowKey, fVal);
                    //tempFld.OldFieldValList.Add($"{rowKey}={fOldVal}");
                    tempFld.OldFieldValList.Add(rowKey, fVal);
                    tempFld.IDValList.Add($"{rowKey}={idValue}");
                    tempFld.OldIDValList.Add($"{rowKey}={oldIdValue}");
                    tempFld.RecIDList.Add($"{rowKey}={recordId}");
                    tempFld.OldRecIDList.Add($"{rowKey}={recordId}");
                }
                else
                {
                    //tempFld.FieldValList[index] = $"{rowKey}={fVal}";
                    tempFld.FieldValList[rowKey] = fVal;
                    tempFld.IDValList[index] = $"{rowKey}={idValue}";

                    if (_onLoad)
                    {
                        //tempFld.OldFieldValList[index] = $"{rowKey}={fOldVal}";
                        tempFld.OldFieldValList[rowKey] = fOldVal;
                        tempFld.OldIDValList[index] = $"{rowKey}={oldIdValue}";
                        tempFld.RecIDList[index] = $"{rowKey}={recordId}";
                        tempFld.OldRecIDList[index] = $"{rowKey}={recordId}";
                    }
                }

                if (tempFld.FieldValList.Count > Frames[tempFld.FrameNo - 1].RowCount)
                    Frames[tempFld.FrameNo - 1].RowCount = tempFld.FieldValList.Count;
            }
            else
            {
                Frames[tempFld.FrameNo - 1].RowCount = 1;
            }

            // Update the field in the list
            int fieldIndex = GetFieldIndex(tempFld.FieldName);

            if (fieldIndex >= 0)
                Flds[fieldIndex] = tempFld;
                _lookupInitialized = false;
        }

        /// <param name="fieldname">Field name to find index for</param>
        /// <returns>Zero-based index of field in Flds list, -1 if not found</returns>
        /// <example>GetFieldIndex("CustomerName") returns 5</example>
        public int GetFieldIndex(string fieldName)
        {
            for (int i = 0; i < Flds.Count; i++)
            {
                if (Flds[i].FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public int GetFieldIndex(string fieldName, int rowNo)
        {
            for (int i = 0; i < FieldList.Count; i++)
            {
                if (FieldList[i].FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) && FieldList[i].RowNo == rowNo)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <param name="s">Display name to search for</param>
        /// <returns>TField with matching display name, null if not found</returns>
        /// <example>SearchDisplayName("Customer Name") returns CustomerName field</example>
        public TField? SearchDisplayName(string disName)
        {
            return Flds.FirstOrDefault(f => f.DispName.Equals(disName,StringComparison.OrdinalIgnoreCase));
        }

        /// <param name="tableName">Frame table name to find</param>
        /// <returns>TFrame with matching table name, null if not found</returns>
        /// <example>GetFrame("custd1") returns customer details frame</example>
        public TFrame? GetFrame(string tableName)
        {
            string tbName = tableName.ToLower();
            return Frames?.FirstOrDefault(f => f.TableName.Equals(tbName, StringComparison.OrdinalIgnoreCase));
        }

        public TFrame? GetFrameFromName(string? frameName)
        {
            if (string.IsNullOrWhiteSpace(frameName) || frameName.Length < 2)
                return default;

            if (!int.TryParse(frameName.AsSpan(2), out var frameNo))
                return default;

            return Frames?.FirstOrDefault(f => f.FrameNo == frameNo);
        }

        /// <param name="Frameno">Frame number to search for</param>
        /// <returns>Index of first field in specified frame, -1 if no fields</returns>
        /// <example>GetFirstFieldIndex(1) returns 0</example>
        public int GetFirstFieldIndex(int Frameno)
        {
            return Flds.FindIndex(f => f.FrameNo == Frameno);
        }

        /// <param name="Frameno">Frame number to search for</param>
        /// <returns>Index of last field in specified frame, -1 if no fields</returns>
        /// <example>GetLastFieldIndex(1) returns 15</example>
        public int GetLastFieldIndex(int Frameno)
        {
            return Flds.FindLastIndex(f => f.FrameNo == Frameno);
        }

        /// <param name="Frameno">Frame number to search in</param>
        /// <returns>Name of first enabled field in frame, empty string if none</returns>
        /// <example>GetFirstEnabledFldName(1) returns "CustomerCode"</example>
        public string GetFirstEnabledFldName(int Frameno)
        {
            var field = Flds.FirstOrDefault(f => f.FrameNo == Frameno && !f.Readonly && !f.Hidden);
            return field?.FieldName ?? "";
        }

        /// <param name="fldname">Field name to find frame for</param>
        /// <returns>Frame number containing the field, -1 if not found</returns>
        /// <example>GetFrameNo("CustomerName") returns 1</example>
        public int GetFrameNo(string fldName)
        {
            var field = GetField(fldName);
            if (field?.FrameNo >= 1)
                return field.FrameNo;
            else
                return 0;
        }

        /// <param name="lblName">Label name to add to dynamic labels</param>
        /// <example>AddToDynamicLabel("lblCustomerName")</example>
        public void AddToDynamicLabel(string lbName)
        {
            if (!DynamicLabels.Contains(lbName))
                DynamicLabels += lbName + ",";
        }

        /// <param name="tempFld">TField object to get value from</param>
        /// <param name="rowNo">Row number to retrieve value for</param>
        /// <returns>Field value as string, decrypted if encrypted</returns>
        /// <example>GetFieldValue(customerField, 1) returns "John Doe"</example>
        public string GetFieldValue(TField tempFld, int rowNo)
        {
            string value = string.Empty;
            if (tempFld == null)
                return value;

            if (!tempFld.AsGrid)
            {
                if (tempFld.FieldValue != null)
                {
                    //if (tempFld.EncryptValue)
                    //    value = AXPEncryptData.Decrypt(tempFld.FieldValue);
                    //else
                    //    value = tempFld.FieldValue;

                    value = tempFld.FieldValue;

                    if (tempFld.CustType?.ToUpper() == "DATE")
                        return value.Split(' ')[0];
                    else
                        return value;
                }
                else
                    return value;
            }

            value = GetValueFromList(tempFld.FieldValList, rowNo.ToString()) ?? string.Empty;

            if (!string.IsNullOrEmpty(value))
            {
                //if (tempFld.EncryptValue)
                //    value = AXPEncryptData.Decrypt(tempFld.FieldValue);
                //else
                //    value = tempFld.FieldValue;

                //value = tempFld.FieldValue;

                if (tempFld.CustType?.ToUpper() == "DATE")
                    return value.Split(' ')[0];
                else
                    return value;
            }
            return value;
        }

        /// <param name="fieldName">Field name to get value for</param>
        /// <param name="rowNo">Row number to retrieve value for</param>
        /// <returns>Field value as string, empty if field not found</returns>
        /// <example>GetFieldValue("CustomerName", 1) returns "John Doe"</example>
        public string GetFieldValue(string fieldName, int rowNo)
        {
            var tempFld = GetField(fieldName);
            return GetFieldValue(tempFld, rowNo);
        }

        /// <param name="fieldName">Field name to get value for</param>
        /// <param name="rowNo">Row number to retrieve value for</param>
        /// <returns>Field value as string, empty if field not found</returns>
        /// <example>GetFieldValue("CustomerName", 1) returns "John Doe"</example>
        public string GetFieldDataType(string fieldName)
        {
            var tempFld = GetField(fieldName);
            return tempFld.DataType;
        }

        // public DbType GetFieldDataTypeForDB(string fieldName)
        // {
        //     var tempFld = GetField(fieldName);
        //     var dataType = DbType.String;
        //     switch (tempFld.DataType)
        //     {
        //         case "i":
        //             dataType = DbType.Binary;
        //             break;
        //         case "s":
        //         case "c":
        //             dataType = DbType.String;
        //             break;
        //         case "n":
        //             if (tempFld.CustomDecimal)
        //                 dataType = DbType.Decimal;
        //             else
        //                 dataType = DbType.Int64;
        //             break;
        //         case "d":
        //             dataType = DbType.DateTime;
        //             break;
        //         default:
        //             dataType = DbType.String;
        //             break;
        //     }
        //     return dataType;
        // }

        public DbType GetFieldDBType(string fieldName)
        {
            var tempFld = GetField(fieldName);
            return GetFieldDBType(tempFld);
        }

        public DbType GetFieldDBType(TField tempFld)
        {
            var dataType = DbType.String;

            if (tempFld.SourceKey)
                return DbType.Int64;

            switch (tempFld.DataType.ToUpper())
            {
                case "D":
                    if(tempFld.CustType.ToLower().Equals("timestamp"))
                    {
                        dataType = DbType.DateTime;
                    }
                    else
                    {
                    dataType = DbType.Date;
                    }
                    break;
                case "N":
                    if (tempFld.Dec > 0)
                        dataType = DbType.Decimal;
                    else
                        dataType = DbType.Int64;
                    break;
                case "T":
                case "C":
                    if (tempFld.CustType.ToUpper() == "JSON")
                        dataType = DbType.Object;
                    else if (tempFld.CustType.ToUpper() == "GUID")
                        dataType = DbType.Guid;
                    else
                        dataType = DbType.String;
                    break;
                default:
                    dataType = DbType.String;
                    break;
            }

            return dataType;
        }

        /// <param name="fieldIdx">Field index in Flds list</param>
        /// <param name="rowNo">Row number to retrieve value for</param>
        /// <returns>Field value as string, empty if invalid index</returns>
        /// <example>GetFieldValue(5, 1) returns "John Doe"</example>
        public string GetFieldValue(int fieldIdx, int rowNo)
        {
            if (fieldIdx < 0 || fieldIdx >= Flds.Count)
                return string.Empty;
            return GetFieldValue(Flds[fieldIdx], rowNo);
        }

        /// <param name="tempOldFldName">Field name to get old value from</param>
        /// <param name="rowNo">Row number to retrieve old value for</param>
        /// <returns>Previous field value as string, decrypted if encrypted</returns>
        /// <example>GetOldFieldValue(customerFieldname, 1) returns "Jane Smith"</example>
        public string GetOldFieldValue(string tempOldFldName, int rowNo)
        {
            TField tempOldFld = GetField(tempOldFldName);
            return GetOldFieldValue(tempOldFld, rowNo);
        }

        /// <param name="tempOldFld">TField object to get old value from</param>
        /// <param name="rowNo">Row number to retrieve old value for</param>
        /// <returns>Previous field value as string, decrypted if encrypted</returns>
        /// <example>GetOldFieldValue(customerField, 1) returns "Jane Smith"</example>
        public string GetOldFieldValue(TField tempOldFld, int rowNo)
        {
            if (tempOldFld == null)
                return string.Empty;

            return !tempOldFld.AsGrid
                ? tempOldFld.OldFieldValue ?? string.Empty
                : GetValueFromList(tempOldFld.OldFieldValList, rowNo.ToString()) ?? string.Empty;
        }

        /// <param name="tempFld">TField object to get ID value from</param>
        /// <param name="rowNo">Row number to retrieve ID for</param>
        /// <returns>ID value as Int64, 0 if not found</returns>
        /// <example>GetIDValue(customerField, 1) returns 1001.0</example>
        public Int64 GetIDValue(TField tempFld, int rowNo)
        {
            if (tempFld.FieldName == null)
                return 0;

            if (!tempFld.AsGrid)
                return tempFld.IDValue;
            else
            {
                string value = GetValueFromList(tempFld.IDValList, rowNo.ToString());
                return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
            }
        }

        /// <param name="fieldName">Field name to get ID value for</param>
        /// <param name="rowNo">Row number to retrieve ID for</param>
        /// <returns>ID value as Int64, 0 if field not found</returns>
        /// <example>GetIDValue("CustomerCode", 1) returns 1001.0</example>
        public Int64 GetIDValue(string fieldName, int rowNo)
        {
            var Field = GetField(fieldName);

            if (Field == null)
                return 0;
            return GetIDValue(Field, rowNo);
        }

        /// <param name="tempFld">TField object to get ID value from</param>
        /// <param name="rowNo">Row number to retrieve ID for</param>
        /// <returns>ID value as Int64, 0 if not found</returns>
        /// <example>GetIDValue(customerField, 1) returns 1001.0</example>
        public Int64 GetOldIDValue(TField tempFld, int rowNo)
        {
            if (tempFld.FieldName == null)
                return 0;

            if (!tempFld.AsGrid)
                return tempFld.OldIDValue;
            else
            {
                string value = GetValueFromList(tempFld.OldIDValList, rowNo.ToString());
                return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
            }
        }

        /// <param name="fieldName">Field name to get ID value for</param>
        /// <param name="rowNo">Row number to retrieve ID for</param>
        /// <returns>ID value as Int64, 0 if field not found</returns>
        /// <example>GetIDValue("CustomerCode", 1) returns 1001.0</example>
        public Int64 GetOldIDValue(string fieldName, int rowNo)
        {
            var Field = GetField(fieldName);

            if (Field == null)
                return 0;
            return GetOldIDValue(Field, rowNo);
        }

        /// <param name="tempFld">TField object to get record ID from</param>
        /// <param name="rowNo">Row number to retrieve record ID for</param>
        /// <returns>Record ID as Int64, 0 if not found</returns>
        /// <example>GetRecordId(customerField, 1) returns 5001.0</example>
        public Int64 GetRecordId(TField tempFld, int rowNo)
        {
            if (tempFld.FieldName == null)
                return 0;

            if (!tempFld.AsGrid)
                return tempFld.RecId;
            else
            {
                string value = GetValueFromList(tempFld.RecIDList, rowNo.ToString());
                return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
            }
        }

        /// <param name="frameno">Frame number where row was inserted</param>
        /// <param name="RowNo">Row number that was inserted</param>
        /// <example>InsertedRowNoInfo(1, 5) tracks row 5 insertion in frame 1</example>
        public void InsertedRowNoInfo(int frameno, int RowNo)
        {
            if (RowNo == -1) // || !_axProvider._utils.IsService)
                return;
            if (_depCall)
                return;

            string key = "DC" + frameno;
            int ind = Ins_del_rows.FindIndex(s => s.StartsWith(key + "="));

            if (ind >= 0)
            {
                string currentValue = Ins_del_rows[ind]
                    .Substring(Ins_del_rows[ind].IndexOf('=') + 1);
                string searchPattern = ",i" + RowNo + ",";

                if (!currentValue.Contains(searchPattern))
                {
                    Ins_del_rows[ind] = key + "=" + currentValue + "i" + RowNo + ",";
                }
            }
            else
            {
                Ins_del_rows.Add(key + "=i" + RowNo + ",");
            }
        }

        /// <param name="FieldName">Field name to find index for</param>
        /// <param name="TableName">Table name for the field</param>
        /// <param name="RowNo">Row number in the data container</param>
        /// <returns>Index in FieldList, -1 if not found</returns>
        /// <example>GetIndex("CustomerName", "Customers", 1) returns 10</example>
        public int GetIndex(string FieldName, string TableName, int RowNo)
        {
            for (int i = 0; i < FieldList.Count; i++)
            {
                FieldRec field = FieldList[i];
                if (
                    field.FieldName.Equals(FieldName, StringComparison.OrdinalIgnoreCase)
                    && field.RowNo == RowNo
                    && field.TableName.Equals(TableName, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return i;
                }
            }
            return -1;
        }

        /// <param name="d">Date string to check for emptiness</param>
        /// <returns>True if date is empty/invalid, false otherwise</returns>
        /// <example>IsDateEmpty("") returns true, IsDateEmpty("2023-12-31") returns false</example>
        public bool IsDateEmpty(string d)
        {
            if (string.IsNullOrEmpty(d))
                return true;

            string dateSeparator = ShortDateFormat.DateSeparator;

            // Replace all date separators with a space
            string modified = d.Replace(dateSeparator, " ");

            // Check if the trimmed result is empty
            return string.IsNullOrWhiteSpace(modified);
        }

        public async Task<bool> UpdateLastNo(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            fieldName = fieldName.ToLower();

            int i = SdAutogenFields.IndexOf(fieldName);
            if (i == -1)
                return false;

            string transId = _autogenTransid[i];
            string seqTbl = (GetFieldValue("Axp_SeqTable", 1)?.Trim());
            if (string.IsNullOrEmpty(seqTbl))
                seqTbl = "Sequence";

            string wstr;
            string prefix = string.Empty;

            if (!string.IsNullOrEmpty(_prefixFields[i]))
            {
                wstr =
                    $"lower(TRANSTYPE) = {QuotedStr(transId.ToLower())} "
                    + $"and lower(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                    + $"and lower(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";
            }
            else
            {
                wstr =
                    $"lower(TRANSTYPE) = {QuotedStr(transId.ToLower())} "
                    + $"and lower(FIELDNAME) = {QuotedStr(fieldName.ToLower())} ";

                prefix = AutoGen_Prefix;

                if (string.IsNullOrEmpty(prefix))
                {
                    wstr += " and ACTIVESEQUENCE = 'T'";
                }
                else
                {
                    wstr += $" and lower(prefix) = {QuotedStr(prefix.ToLower())}";
                }
            }

            string lastNo = AutoGen_Lastno;

            // Convert to int safely
            if (!int.TryParse(lastNo, out int lastNoInt))
                lastNoInt = 0;

            string sql = $"UPDATE {seqTbl} SET lastno = {lastNoInt + 1} WHERE {wstr}";

            // Execute the SQL
            try
            {
                var sqlCommands = new List<SQLCommandBuilder>();
                var cmdObj = new SQLCommandBuilder(sql, null, null, null, Constants.DATA_ACTION.UPDATE);
                sqlCommands.Add(cmdObj);

                var updateResult = await _common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands, false);
                if (updateResult.success)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // Log or handle exception as needed
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error updating last number: {ex.Message}");
                return false;
            }
        }

        //public void CheckAutoGen()
        //{
        //    if (!NewTrans) return;

        //    bool flag = true;
        //    int x = 10;

        //    while (flag)
        //    {
        //        flag = false;
        //        string agenstr = ",";

        //        foreach (string fname in AutogenFields)
        //        {
        //            int k = GetFieldIndex(fname, 1);
        //            if (k == -1) continue;

        //            string tname = FieldList[k].TableName;
        //            if (!string.Equals(tname, TableName, StringComparison.OrdinalIgnoreCase)) continue;
        //            if (agenstr.Contains($",{fname},")) continue;

        //            agenstr += fname + ",";
        //            string value = FieldList[k].Value;

        //            work.buffered = true;
        //            work.CDS.CommandText = $"SELECT {fname} FROM {tname} WHERE {fname} = @value AND {tname}id <> @t";
        //            work.AssignParam(0, NewRecid.ToString(), "n"); // Assuming Newrecid is numeric
        //                                                           // Alternative parameter assignment if needed:
        //                                                           // work.ParamsByName("t").Value = Newrecid;

        //            work.Open();
        //            if (!work.CDS.IsEmpty)
        //            {
        //                //structdef.axprovider.dbm.gf.DoDebug.Msg($"Changed value of {fname}");
        //                FieldList[k].Value = GetLastNo(fname, false);
        //                ModTable.Submit(fname, FieldList[k].Value, "c");
        //                //structdef.axprovider.dbm.gf.DoDebug.Msg($"to {FieldList[k].Value}");
        //                flag = true;
        //            }
        //            work.Close();
        //        }

        //        if (x == 1)
        //        {
        //            throw new Exception("Unable to get unique number. Please Save again");
        //        }
        //        x--;
        //    }
        //}

        /// <param name="fieldName">Field name to generate last number for</param>
        /// <param name="onlyGet">True to only retrieve without incrementing</param>
        /// <returns>Generated last number as string</returns>
        /// <example>GetLastNo("InvoiceNo") returns "INV-10025"</example>
        public async Task<string> GetLastNo(string fieldName, bool onlyGet = false)
        {
            string result = "";
            string sqlWoUpd = "";

            await SetAutoGenList();

            string compName = !string.IsNullOrEmpty(CompanyName)
                ? CompanyName.Substring(0, CompanyName.Length - 1)
                : "";

            fieldName = fieldName.ToLower();
            int i = SdAutogenFields.IndexOf(fieldName);

            if (i == -1)
                return result;

            string transid = _autogenTransid[i];
            string seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();

            if (string.IsNullOrEmpty(seqtbl))
                seqtbl = "Sequence";

            string sql = "";
            string wstr = "";
            bool bActive = false;
            string sqlLower = GetSqlLower();

            if (!string.IsNullOrEmpty(_prefixFields[i]))
            {
                sql =
                    $"SELECT * FROM {compName.ToUpper()}{seqtbl} "
                    + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                    + $"AND {sqlLower}(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";

                wstr =
                    $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                    + $"AND {sqlLower}(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";
            }
            else
            {
                string prefix = await GetUserActivePrefix(transid, fieldName);
                string DBType = _common._dbType;
                AutoGen_Prefix = prefix;

                if (!onlyGet && DBType?.Replace(" ", "") == "mssql")
                {
                    string tmpupdstr = " ";
                    //if (!PostAutoGen)  // You'll need to implement PostAutoGen property
                    //    tmpupdstr += GetForUpdate();

                    sql =
                        $"SELECT * FROM {compName.ToUpper()}{seqtbl}{tmpupdstr} "
                        + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                        + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";
                }
                else
                {
                    sql =
                        $"SELECT * FROM {compName.ToUpper()}{seqtbl} "
                        + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                        + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";
                }

                wstr =
                    $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";

                if (string.IsNullOrEmpty(prefix))
                {
                    sql += " AND ACTIVESEQUENCE = 'T'";
                    wstr += " AND ACTIVESEQUENCE = 'T'";
                    bActive = true;
                }
                else
                {
                    sql += $" AND {sqlLower}(prefix) = {QuotedStr(prefix.ToLower())}";
                    wstr += $" AND {sqlLower}(prefix) = {QuotedStr(prefix.ToLower())}";
                    bActive = false;
                }
            }

            try
            {
                // Execute the query using your DBHelper
                var queryResult = await _common._dbHelper?.ExecuteSQLAsync(sql);

                if (queryResult.data.Rows.Count > 0)
                {
                    DataRow row = queryResult.data.Rows[0];
                    string lastno = GetRowValue(row, "lastno");
                    string prefixfield = GetRowValue(row, "prefixfield")?.Trim() ?? "";
                    string prefix = GetRowValue(row, "prefix")?.Trim() ?? "";
                    int digits = Convert.ToInt32(GetRowValue(row, "noofdigits"));

                    string dstr = new string('0', digits);

                    AutoGen_Prefix = prefix;
                    AutoGen_Lastno = lastno;

                    if (!string.IsNullOrEmpty(prefixfield))
                    {
                        string sval = GetFieldValue(prefixfield, 1);
                        if (sval.StartsWith(":"))
                        {
                             result = await GetPrefixFieldValue(fieldName, sval, transid, onlyGet, digits);
                            return result;
                        }
                        else
                        {
                            result = sval + dstr.Substring(0, digits - lastno.Length) + lastno;
                        }
                    }
                    else
                    {
                        result = prefix + dstr.Substring(0, digits - lastno.Length) + lastno;
                    }

                    if (!onlyGet)
                    {
                        await UpdateLastNo(fieldName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error in GetLastNo: {ex.Message}");
            }
            
            return result;
        }

        public async Task<string>  GetPrefixFieldValue(
                string fieldname,
                string sval,
                string transid,
                bool OnlyGet,
                int digits)
        {
            int j, done;
            string pval, sql = "", wstr, dstr, s, lastno = "", fval, seqtbl;
            double id;
            bool GetVal = false;
            TField fd;
            string aTransid, aFName;
            bool openforupdate = false;
            bool postAutoGen = false;
            string result = string.Empty;
            string sqlLower = _common._dbVars["sqllower"];

            try
            {

                s = GetNthString(sval, 1);
                fval = s.Length > 1 ? s.Substring(1) : string.Empty;

                fd = GetField(fval);

                if (fd != null)
                    pval = GetFieldValue(fval, 1);
                else
                    pval = fval;

                s = GetNthString(sval, 4);

                if (!string.IsNullOrEmpty(s))
                    aTransid = s;
                else
                    aTransid = transid;

                s = GetNthString(sval, 5);

                if (!string.IsNullOrEmpty(s))
                    aFName = s;
                else
                    aFName = fieldname;

                seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();

                if (string.IsNullOrEmpty(seqtbl))
                    seqtbl = "Sequence";

                sql = "Select * FROM " + CompanyName.ToUpperInvariant() + seqtbl +
                      " WHERE " +
                      sqlLower +
                      "(TRANSTYPE)=" +
                      sqlLower + "(" +
                      QuotedStr(aTransid) + ")" +
                      " and " +
                      sqlLower +
                      "(FIELDNAME)=" +
                      sqlLower + "(" +
                      QuotedStr(aFName) + ")" +
                      " and " +
                      sqlLower +
                      "(prefix)=" +
                      sqlLower + "(" +
                      QuotedStr(pval) + ")";

                wstr = sqlLower +
                       "(TRANSTYPE) = " +
                       sqlLower + "(" +
                       QuotedStr(aTransid) + ") and " +
                       sqlLower +
                       "(FIELDNAME) = " +
                       sqlLower + "(" +
                       QuotedStr(aFName) + ") and " +
                       sqlLower +
                       "(prefix) = " +
                       sqlLower + "(" +
                       QuotedStr(pval) + ")";

                var queryResult = await _common._dbHelper?.ExecuteSQLAsync(sql);

                if (queryResult.data.Rows.Count == 0)
                {
                    s = GetNthString(sval, 2);

                    lastno = !string.IsNullOrEmpty(s) ? s : "1";

                    s = GetNthString(sval, 3);

                    if (!string.IsNullOrEmpty(s))
                        digits = Convert.ToInt32(s);

                    if (OnlyGet)
                    {
                        GetVal = false;
                    }
                    else
                    {
                        var recIdsResult = await GetRecordIdsFromDB(1);

                        if (!string.IsNullOrEmpty(recIdsResult.error))
                            throw new Exception($"Error fetching new Record Ids: {recIdsResult.error}");

                        id = recIdsResult.data.AsEnumerable()
                                 .Where(row => row["recordid"] != DBNull.Value)
                                 .Select(row => Convert.ToInt64(row["recordid"]))
                                 .FirstOrDefault();

                        s = "insert into " +
                            CompanyName.ToUpperInvariant() + seqtbl +
                            "(sequenceid, prefix,transtype,fieldname,activesequence," +
                            "description,prefixfield,lastno,noofdigits) values(";

                        s += id.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + ",";
                        s += QuotedStr(pval) + ",";
                        s += QuotedStr(aTransid) + ",";
                        s += QuotedStr(aFName) + ",";
                        s += QuotedStr("F") + ",";
                        s += QuotedStr("~dynamic prefix") + ",";
                        s += QuotedStr("'") + ",";
                        s += (Convert.ToInt32(lastno) + 1).ToString() + ",";
                        s += digits.ToString() + ")";

                        try
                        {
                            await _common._dbHelper.ExecuteScalarAsync(s);

                            GetVal = false;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.ToLowerInvariant().Contains("unique"))
                            {
                                GetVal = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                else
                {
                    GetVal = true;
                }

                try
                {
                    if (GetVal)
                    {
                        if (!OnlyGet)
                        {
                            if (_common._dbType == "ms sql")
                            {
                                sql = "Select * FROM " +
                                      CompanyName.ToUpperInvariant() + seqtbl +
                                      " " + _common._dbVars["forupdate"] +
                                      " WHERE " +
                                      sqlLower +
                                      "(TRANSTYPE)=" +
                                      sqlLower + "(" +
                                      QuotedStr(aTransid) + ")" +
                                      " and " +
                                      sqlLower +
                                      "(FIELDNAME)=" +
                                      sqlLower + "(" +
                                      QuotedStr(aFName) + ")" +
                                      " and " +
                                      sqlLower +
                                      "(prefix)=" +
                                      sqlLower + "(" +
                                      QuotedStr(pval) + ")";
                            }
                            else
                            {
                                sql = "Select * FROM " +
                                      CompanyName.ToUpperInvariant() + seqtbl +
                                      " WHERE " +
                                      sqlLower +
                                      "(TRANSTYPE)=" +
                                      sqlLower + "(" +
                                      QuotedStr(aTransid) + ")" +
                                      " and " +
                                      sqlLower +
                                      "(FIELDNAME)=" +
                                      sqlLower + "(" +
                                      QuotedStr(aFName) + ")" +
                                      " and " +
                                      sqlLower +
                                      "(prefix)=" +
                                      sqlLower + "(" +
                                      QuotedStr(pval) + ")";
                            }
                        }
                        else
                        {
                            sql = "Select * FROM " +
                                  CompanyName.ToUpperInvariant() + seqtbl +
                                  " WHERE " +
                                  sqlLower +
                                  "(TRANSTYPE)=" +
                                  sqlLower + "(" +
                                  QuotedStr(aTransid) + ")" +
                                  " and " +
                                  sqlLower +
                                  "(FIELDNAME)=" +
                                  sqlLower + "(" +
                                  QuotedStr(aFName) + ")" +
                                  " and " +
                                  sqlLower +
                                  "(prefix)=" +
                                  sqlLower + "(" +
                                  QuotedStr(pval) + ")";
                        }

                        var XmlDoc = await _common._dbHelper?.ExecuteSQLAsync(sql);
                        openforupdate = false;

                        if (!postAutoGen)
                        {
                            if (!OnlyGet)
                            {
                                if (_common._dbType == "mysql")
                                {
                                    {
                                        try
                                        {

                                            var t = sql + _common._dbVars["forupdate"];

                                            var query = await _common._dbHelper?.ExecuteSQLAsync(t);

                                            openforupdate = true;
                                        }
                                        catch (Exception e)
                                        {
                                                throw new Exception(e.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    string qt = "";
                                    if (_common._dbType == "ms sql")
                                    {
                                        qt = sql;

                                    }
                                    else
                                        qt = sql + _common._dbVars["forupdate"];

                                    var query = await _common._dbHelper?.ExecuteSQLAsync(qt);
                                }
                            }
                        }

                        if (XmlDoc.data.Rows.Count > 0)
                        {
                            DataRow row = XmlDoc.data.Rows[0];
                            lastno = GetRowValue(row, "LASTNO");
                            string digtVal = GetRowValue(row, "NOOFDIGITS");
                            if(!string.IsNullOrEmpty(digtVal))
                            {
                                digits = Convert.ToInt32(digtVal);
                            }

                            pval = GetRowValue(row, "PREFIX")?.Trim();
                        }
                        else
                        {
                            pval = string.Empty;
                        }
                    }

                    if (!string.IsNullOrEmpty(pval))
                    {
                        dstr = string.Empty;

                        for (j = 0; j < digits; j++)
                            dstr += "0";

                        int paddingLength = digits - lastno.Length;

                        if (paddingLength < 0)
                            paddingLength = 0;

                        if (paddingLength > dstr.Length)
                            paddingLength = dstr.Length;

                        result = pval +
                                 dstr.Substring(0, paddingLength) +
                                 lastno;

                        if (!OnlyGet)
                        {
                            if (GetVal)
                            {
                                if (!postAutoGen)
                                {
                                    sql = "update " + CompanyName + seqtbl +
                                          " set lastno = " +
                                          (Convert.ToInt32(lastno) + 1).ToString() +
                                          " where " + wstr;

                                    var query = await _common._dbHelper?.ExecuteSQLAsync(sql);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (openforupdate)
                    {
                        await _common._dbHelper?.ExecuteSQLAsync(sql);
                    }

                    throw;
                }
            }
            catch (Exception e)
            {

                throw;
            }

            return result;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <param name="rowNo">Row number to set record ID for</param>
        /// <param name="recordId">Record ID value to set</param>
        /// <returns>True if successful, false if invalid DC number</returns>
        /// <example>SubmitRecordId(1, 5, 1000023209304) sets record ID for DC1 row5</example>
        public bool SubmitRecordId(int dcNo, int rowNo, Int64 recordId)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                TFrame frame = Frames[dcNo - 1];

                // Add or update the key-value pair
                if (frame.RecordIdDict.ContainsKey(rowNo))
                {
                    frame.RecordIdDict[rowNo] = recordId;
                }
                else
                {
                    frame.RecordIdDict.Add(rowNo, recordId);
                }

                return true;
            }
            else
                return false;
        }

        /// <param name="frame">TFrame object to get record ID from</param>
        /// <param name="rowNo">Row number to retrieve record ID for</param>
        /// <returns>Record ID as Int64, 0 if not found</returns>
        /// <example>GetRecordId(customerFrame, 5) returns 1000023209304</example>
        public Int64 GetRecordId(TFrame frame, int rowNo)
        {
            if (frame?.RecordIdDict == null)
                return 0;

            if (frame.RecordIdDict.TryGetValue(rowNo, out Int64 recordId))
            {
                return recordId;
            }
            return 0;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <param name="rowNo">Row number to retrieve record ID for</param>
        /// <returns>Record ID as Int64, 0 if invalid DC or row not found</returns>
        /// <example>GetRecordId(1, 5) returns 1000023209304</example>
        public Int64 GetRecordId(int dcNo, int rowNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return 0;

            return GetRecordId(Frames[dcNo - 1], rowNo);
        }

        /// <param name="fieldName">Field name to set value for during loading</param>
        /// <param name="rowNo">Row number to set value for</param>
        /// <param name="value">Value to set for the field</param>
        /// <returns>True if successful, false if field or frame not found</returns>
        /// <example>SubmitValueOnLoad("CustomerName", 1, "John Doe") loads initial value</example>
        public bool SubmitValueOnLoad(string fieldName, int rowNo, string value, Int64 idValue = 0, bool convertClientDateToDbFormat = false) //, string encryptValue = "")
        {
            TField fld = GetField(fieldName);

            if (fld == null)
                return false;

            TFrame frame = Frames[fld.FrameNo - 1];
            if (frame == null)
                return false;

            // Get recordId for the row using dictionary
            Int64? recordId = GetRecordId(frame, rowNo);

            Int64 fldIdValue = idValue;
            Int64 fldOldIdValue = idValue;

            if (fld.EncryptValue)
            {
                value = AXPEncryptData.Decrypt(value);
            }

            try
            {
                _onLoad = true;
                //if (recordId.HasValue)
                {
                    SetFieldValue(
                        FieldName: fieldName,
                        DataType: fld.DataType,
                        CustomDataType: fld.CustType,
                        TableName: fld.Tablename,
                        Value: value, // This need to be handled for Normalized Field
                        OldValue: value,
                        RowNo: rowNo,
                        IdValue: fldIdValue, // Value should be assigned (Normalized Field)
                        OldIdValue: fldOldIdValue, // Value should be assigned (Normalized Field)
                        FrameNo: fld.FrameNo,
                        RecordId: recordId.Value,
                        SourceKey: fld.SourceKey,
                        convertClientDateToDbFormat
                    //encrypt_value: encryptValue
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error in SetFieldValue: {ex.Message}");
                throw new Exception("Error in SetFieldValue: " + ex.Message);
            }
            finally
            {
                _onLoad = false;
            }

            //return false;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <returns>Comma-separated list of field names in the DC</returns>
        /// <example>GetDCFields(1) returns "CustomerCode,CustomerName,Address"</example>
        public string GetDCFields(int dcNo)
        {
            return string.Join(",", GetDCFieldsAsList(dcNo));
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <returns>list of field names in the DC</returns>
        /// <example>GetDCFields(1) returns "CustomerCode,CustomerName,Address"</example>
        public List<string> GetDCFieldsAsList(int dcNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return [];

            TFrame frame = Frames[dcNo - 1];
            List<string> fieldNames = new List<string>();

            int startIndex = frame.StartIndex;
            int fieldCount = frame.FieldCount;

            for (int i = startIndex; i < startIndex + fieldCount; i++) // 1. startIndex ? 2. Flds[ i - 1 ] needed?
            {
                if (Flds[i] != null)
                {
                    fieldNames.Add(Flds[i].FieldName);
                }
            }

            return fieldNames;
        }

        public List<TField> GetDCFieldObjsAsList(int dcNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return [];

            TFrame frame = Frames[dcNo - 1];
            List<TField> fields = new List<TField>();

            int startIndex = frame.StartIndex;
            int fieldCount = frame.FieldCount;

            for (int i = startIndex; i < startIndex + fieldCount; i++) // 1. startIndex ? 2. Flds[ i - 1 ] needed?
            {
                if (Flds[i] != null)
                {
                    fields.Add(Flds[i]);
                }
            }

            return fields;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <returns>Table name for the specified DC, empty string if invalid</returns>
        /// <example>GetDCTable(1) returns "Customers"</example>
        public string GetDCTable(int dcNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return string.Empty;
            else
                return Frames[dcNo - 1].TableName ?? string.Empty;
        }

        /// <returns>Total number of data containers in the structure</returns>
        /// <example>GetDCCount() returns 5</example>
        public int GetDCCount()
        {
            return Frames?.Count ?? 0;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <returns>Number of rows in the DC, 0 if invalid DC</returns>
        /// <example>GetRowCount(1) returns 25</example>
        public int GetRowCount(int dcNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return 0;
            else
                return Frames[dcNo - 1].RowCount >= 0 ? Frames[dcNo - 1].RowCount : 0;
        }

        public int GetAllDcRowCount()
        {
            int totalRowCount = 0;
            HashSet<string> processedTables = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var frm in Frames)
            {
                if (!processedTables.Add(frm.TableName))
                    continue;

                totalRowCount += GetRowCount(frm.FrameNo);
            }

            return totalRowCount;
        }


        /// <returns>Comma-separated list of grid DC numbers</returns>
        /// <example>GetGridDcs() returns "1,3,5"</example>
        public string GetGridDcs()
        {
            return string.Join(",", GetGridDcsAsList());
        }

        /// <returns>list of grid DC numbers</returns>
        /// <example>GetGridDcs() returns [1,3,5]</example>
        public List<int> GetGridDcsAsList()
        {
            var GridDcs = Frames
                .Where(frame => frame.AsGrid)
                .Select(frame => frame.FrameNo)
                .ToList();

            return GridDcs;
        }

        /// <returns>Comma-separated list of non-grid table names</returns>
        /// <example>GetNGTableNames() returns "Customers,Products"</example>
        public string GetNGTableNames()
        {
            return string.Join(",", GetNGTableNamesAsList());
        }

        /// <returns>list of non-grid table names</returns>
        /// <example>GetNGTableNames() returns ["Customers","Products",..]</example>
        public List<string> GetNGTableNamesAsList()
        {
            var NonGridDcs = Frames
                .Where(frame => !frame.AsGrid)
                .Select(frame => frame.TableName)
                .ToList();

            return NonGridDcs;
        }

        /// <param name="FieldName">Field name to check for normalization</param>
        /// <returns>True if field is normalized/source key, false otherwise</returns>
        /// <example>IsFieldNormalized("CustomerCode") returns true</example>
        public bool IsFieldNormalized(string FieldName)
        {
            var Field = GetField(FieldName);

            return Field == null ? false : Field.SourceKey;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <returns>Dictionary mapping row numbers to record IDs</returns>
        /// <example>GetRecordIdsForDC(1) returns {{1,1001}, {2,1002}}</example>
        public Dictionary<int, Int64> GetRecordIdsForDC(int dcNo)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                return new Dictionary<int, Int64>(Frames[dcNo - 1].RecordIdDict);
            }
            return new Dictionary<int, Int64>();
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <param name="rowNo">Row number to check</param>
        /// <returns>True if record ID exists for the row, false otherwise</returns>
        /// <example>HasRecordId(1, 5) returns true</example>
        public bool HasRecordId(int dcNo, int rowNo)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                return Frames[dcNo - 1].RecordIdDict.ContainsKey(rowNo);
            }
            return false;
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <param name="rowNo">Row number to remove record ID for</param>
        /// <example>RemoveRecordId(1, 5) removes record ID for DC1 row5</example>
        public void RemoveRecordId(int dcNo, int rowNo)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                Frames[dcNo - 1].RecordIdDict.Remove(rowNo);
            }
        }

        /// <param name="dcNo">Data container number (1-based index)</param>
        /// <example>ClearRecordIds(1) clears all record IDs for DC1</example>
        public void ClearRecordIds(int dcNo)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                Frames[dcNo - 1].RecordIdDict.Clear();
            }
        }

        public bool SetNonGridRowAction(string tableName, string rowAction)
        {
            // Each frame will have its own RowActionDict.For non-grid cases,
            // we are handling it with both the table name and the row number for easier usage
            TFrame frame = GetFrame(tableName);

            if (frame == null)
                return false;

            if (frame.RowActionDict.ContainsKey(tableName))
            {
                frame.RowActionDict[tableName] = rowAction;
                frame.RowActionDict["1"] = rowAction;
            }
            else
            {
                frame.RowActionDict.Add(tableName, rowAction);
                frame.RowActionDict.Add("1", rowAction);
            }

            return true;
        }

        public bool SetGridRowAction(int dcNo, int rowNo, string rowAction)
        {
            if (dcNo >= 1 && dcNo <= Frames.Count && Frames[dcNo - 1] != null)
            {
                TFrame frame = Frames[dcNo - 1];
                string RowNo = rowNo.ToString();

                if (frame.RowActionDict.ContainsKey(RowNo))
                {
                    frame.RowActionDict[RowNo] = rowAction;
                }
                else
                {
                    frame.RowActionDict.Add(RowNo, rowAction);
                }

                return true;
            }
            else
                return false;
        }

        // public string GetRowAction(int dcNo, int rowNo)
        // {
        //     if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
        //         return string.Empty;

        //     TFrame frame = Frames[dcNo - 1];

        //     return frame.RowActionDict[rowNo.ToString()] ?? string.Empty;
        // }

        public string GetRowAction(int dcNo, int rowNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return string.Empty;

            TFrame frame = Frames[dcNo - 1];
            if (frame.RowActionDict.Count > 0 && frame.RowActionDict.ContainsKey(rowNo.ToString()))
                return frame.RowActionDict[rowNo.ToString()];
            else
                return string.Empty;
        }

        //public string GetGridRowChanges(int dcNo, int rowNo)
        //{
        //    if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
        //        return string.Empty;

        //    TFrame frame = Frames[dcNo - 1];

        //    if(frame.RowActionDict[rowNo.ToString()] == null) return string.Empty;

        //    int startIndex = frame.StartIndex;
        //    int fieldCount = frame.FieldCount;
        //    List<string> fieldChanges = new List<string>();

        //    for (int i = startIndex; i < startIndex + fieldCount; i++)
        //    {
        //        //if (Flds[i] != null)
        //        {
        //            //if(Flds[i].FrameNo == dcNo)
        //            {
        //                var fldValue = GetFieldValue(Flds[i], rowNo);
        //                var fldOldValue = GetOldFieldValue(Flds[i], rowNo);

        //                if (fldValue != fldOldValue)
        //                {
        //                    fieldChanges.Add(Flds[i].FieldName + "=" + fldValue);
        //                }
        //            }
        //        }
        //    }
        //    return string.Join(",", fieldChanges);
        //}

        public string GetGridRowChanges(int dcNo, int rowNo)
        {
            return string.Join(",", GetGridRowChangesAsList(dcNo, rowNo));
        }

        public List<string> GetGridRowChangesAsList(int dcNo, int rowNo)
        {
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return [];

            TFrame frame = Frames[dcNo - 1];

            if (frame.RowActionDict[rowNo.ToString()] == null)
                return [];

            int startIndex = frame.StartIndex;
            int fieldCount = frame.FieldCount;
            List<string> fieldChanges = new List<string>();

            for (int i = startIndex; i < startIndex + fieldCount; i++)
            {
                //if (Flds[i] != null)
                {
                    //if (Flds[i].FrameNo == dcNo)
                    {
                        var fldValue = string.Empty;
                        var fldOldValue = string.Empty;
                        if (Flds[i].SourceKey)
                        {
                            fldValue = GetIDValue(Flds[i], rowNo).ToString();
                            fldOldValue = GetOldIDValue(Flds[i], rowNo).ToString();
                        }
                        else
                        {
                            fldValue = GetFieldValue(Flds[i], rowNo);
                            fldOldValue = GetOldFieldValue(Flds[i], rowNo);
                        }

                        if (fldValue != fldOldValue)
                        {
                            fieldChanges.Add(Flds[i].FieldName + "=" + fldValue);
                        }
                    }
                }
            }
            return fieldChanges;
        }

        public List<TField> GetGridRowChangedFieldsAsList(int dcNo, int rowNo)
        {
            List<TField> changedFields = new List<TField>();
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return changedFields;

            TFrame frame = Frames[dcNo - 1];

            if (frame.RowActionDict[rowNo.ToString()] == null)
                return changedFields;

            int startIndex = frame.StartIndex;
            int fieldCount = frame.FieldCount;

            for (int i = startIndex; i < startIndex + fieldCount; i++)
            {
                var fldValue = string.Empty;
                var fldOldValue = string.Empty;
                if (Flds[i].SourceKey)
                {
                    fldValue = GetIDValue(Flds[i], rowNo).ToString();
                    fldOldValue = GetOldIDValue(Flds[i], rowNo).ToString();
                }
                else
                {
                    fldValue = GetFieldValue(Flds[i], rowNo);
                    fldOldValue = GetOldFieldValue(Flds[i], rowNo);
                }

                if (fldValue != fldOldValue)
                {
                    changedFields.Add(Flds[i]);
                }
            }
            return changedFields;
        }
        
        public string GetNGChanges(string tableName)
        {
            return string.Join(",", GetNGChangesAsList(tableName));
        }

        //public string GetNGChanges(string tableName)
        //{
        //    TFrame frame = GetFrame(tableName);

        //    if (frame == null) return string.Empty;

        //    if (frame.RowActionDict[tableName] == null) return string.Empty;

        //    int startIndex = frame.StartIndex;
        //    int fieldCount = frame.FieldCount;
        //    List<string> fieldChanges = new List<string>();

        //    for (int i = startIndex; i < startIndex + fieldCount; i++)
        //    {
        //        if (Flds[i] != null)
        //        {
        //            if (Flds[i].Tablename == tableName)
        //            {
        //                var fldValue = GetFieldValue(Flds[i], 1);
        //                var fldOldValue = GetOldFieldValue(Flds[i], 1);

        //                if (fldValue != fldOldValue)
        //                {
        //                    fieldChanges.Add(Flds[i].FieldName + "=" + fldValue);
        //                }
        //            }
        //        }
        //    }
        //    return string.Join(",", fieldChanges);
        //}

        public List<string> GetNGChangesAsList(string tableName)
        {
            int startIndex = 0;
            int fieldCount = Flds.Count;
            List<string> fieldChanges = new List<string>();

            for (int i = startIndex; i < fieldCount; i++)
            {
                if (!Flds[i].AsGrid)
                {
                    if (Flds[i].Tablename == tableName)
                    {
                        var fldValue = string.Empty;
                        var fldOldValue = string.Empty;
                        if (Flds[i].SourceKey)
                        {
                            fldValue = GetIDValue(Flds[i], 1).ToString();
                            fldOldValue = GetOldIDValue(Flds[i], 1).ToString();
                        }
                        else
                        {
                            fldValue = GetFieldValue(Flds[i], 1);
                            fldOldValue = GetOldFieldValue(Flds[i], 1);
                        }

                        if (fldValue != fldOldValue)
                        {
                            fieldChanges.Add(Flds[i].FieldName + "=" + fldValue);
                        }
                    }
                }
            }
            return fieldChanges;
        }

        public List<TField> GetNGChangedFieldsAsList(string tableName, string mode = "")
        {
            int startIndex = 0;
            int fieldCount = Flds.Count;
            List<TField> changedFields = new List<TField>();

            for (int i = startIndex; i < fieldCount; i++)
            {
                if (!Flds[i].AsGrid)
                {
                    if (Flds[i].Tablename == tableName)
                    {
                        var fldValue = string.Empty;
                        var fldOldValue = string.Empty;
                        if (Flds[i].SourceKey)
                        {
                            fldValue = GetIDValue(Flds[i], 1).ToString();
                            fldOldValue = GetOldIDValue(Flds[i], 1).ToString();

                            if (mode?.ToLower() == "create")
                                fldOldValue = "";
                        }
                        else
                        {
                            fldValue = GetFieldValue(Flds[i], 1);
                            fldOldValue = GetOldFieldValue(Flds[i], 1);
                        }

                        if (fldValue != fldOldValue)
                        {
                            changedFields.Add(Flds[i]);
                        }
                    }
                }
            }
            return changedFields;
        }

        public List<TField> GetRowFieldsAsList(int dcNo, int rowNo)
        {
            List<TField> fields = new List<TField>();
            if (dcNo < 1 || dcNo > Frames.Count || Frames[dcNo - 1] == null)
                return fields;

            TFrame frame = Frames[dcNo - 1];

            int startIndex = frame.StartIndex;
            int fieldCount = frame.FieldCount;

            for (int i = startIndex; i < startIndex + fieldCount; i++)
            {
                var fldValue = string.Empty;
                var fldOldValue = string.Empty;
                if (Flds[i].SourceKey)
                {
                    fldValue = GetIDValue(Flds[i], rowNo).ToString();
                    fldOldValue = GetOldIDValue(Flds[i], rowNo).ToString();
                }
                else
                {
                    fldValue = GetFieldValue(Flds[i], rowNo);
                    fldOldValue = GetOldFieldValue(Flds[i], rowNo);
                }

                fields.Add(Flds[i]);
            }
            return fields;
        }
        
        public string GetChangedFields(string tableName)
        {
            return string.Join(",", GetChangedFieldsAsList(tableName));
        }

        public List<string> GetChangedFieldsAsList(string tableName)
        {
            int startIndex = 0;
            int fieldCount = Flds.Count;
            List<string> fieldChanges = new List<string>();

            for (int i = startIndex; i < fieldCount; i++)
            {
                if (!Flds[i].AsGrid)
                {
                    if (Flds[i].Tablename == tableName)
                    {
                        var fldValue = GetFieldValue(Flds[i], 1);
                        var fldOldValue = GetOldFieldValue(Flds[i], 1);

                        if (fldValue != fldOldValue)
                        {
                            fieldChanges.Add(Flds[i].FieldName);
                        }
                    }
                }
            }
            return fieldChanges;
        }

        //public void SetCurrencyDec(string s, int n)
        //{
        //    // Implementation needed
        //}


        //public string NextTrans()
        //{

        //    // If List is empty, nothing to return 
        //    if (structDefList == null || structDefList.Count == 0)
        //        return string.Empty;

        //    // If first call and index = -1, move to index 0
        //    if (currentMapIndex == -1)
        //        currentMapIndex = 0;
        //    else
        //        currentMapIndex++;

        //    // If exceeded range, return 0
        //    if (currentMapIndex >= structDefList.Count)
        //        return string.Empty;

        //    // Fetch the transid and return
        //    return structDefList[currentMapIndex].TargetTransId;
        //}

        /// <summary>
        /// If the relationship is:
        ///      Parent → Child1 → Child2 → Child3
        /// 
        /// Calling GetParenStructdef(Child3) will:
        /// Check immediate parent

        ///   If not found in list, recursively move up the chain

        ///  Return the first valid parent
        /// </summary>
        /// <param name="childTransId"> the childTransid got from NextTrans Method </param>
        //public TStructDefRecord? GetParentStructDef(string childTransId)
        //{
        //    // Find the record where this childTransId is the target (the child record)
        //    var childStruct = structDefList
        //        .FirstOrDefault(sdef => sdef.TargetTransId == childTransId);

        //    if (childStruct == null)
        //        return null; // No matching child → no parent

        //    string parentTransId = childStruct.SourceTransId;

        //    // Find the parent where its TargetTransId matches the parent's ID
        //    var parentStruct = structDefList
        //        .FirstOrDefault(sdef => sdef.TargetTransId == parentTransId);

        //    if (parentStruct != null)
        //        return parentStruct;

        //    // Recursively move upwards using parentTransId until found
        //    return GetParentStructDef(parentTransId);
        //}

        //public TStructDefRecord? GetStructDef(string transId)
        //{
        //    return structDefList.FirstOrDefault(s => s.TargetTransId == transId); 
        //}

        /// <summary>
        /// GetParentStructDef method using loop
        /// </summary>
        //public TStructDefRecord? GetParentStructDef(string childTransId)
        //{
        //    // Loop structList 
        //    foreach (var sdef in structDefList)
        //    {
        //        string sourceTransId = sdef.SourceTransId; 

        //        if (ParentStructDef != null && ParentStructDef.TargetTransId == sourceTransId)
        //        {
        //            return ParentStructDef;

        //        }

        //        return GetStructDef(sourceTransId); 
        //    }

        //    return null; 
        //}


        //public TStructDefRecord? GetParentStructDef(string childTransId)
        //{
        //    // Loop structDefList
        //    foreach (var sdef in structDefList)
        //    {
        //        // Match childTransId with TargetTransId
        //        if (sdef.TargetTransId == childTransId)
        //        {
        //            string sourceTransId = sdef.SourceTransId;

        //            // If SourceTransId matches ParentStructDef.TransId
        //            if (ParentStructDef != null && ParentStructDef.TransId == sourceTransId)
        //            {
        //                return ParentStructDef;   // return ParentStructDef
        //            }

        //            // Else return GetStructDef(sourceTransId)
        //            return GetStructDef(sourceTransId);
        //        }
        //    }

        // If no matching TargetTransId found
        //    return null;
        //}

        public void GetParentValue(
        int frmno,
        int rowno,
        string pFields,
        List<KeyValuePair<string, string>> sList)
        {
            sList.Clear();

            int i = 1;

            // pField := Trim(GetNthString(pFields, i))
            string pField = GetNthString(pFields, i)?.Trim() ?? "";

            while (!string.IsNullOrEmpty(pField))
            {
                string fname = "sub" + frmno.ToString() + "_" + pField;

                // tfld := sd.structdef.GetField(pField)
                var tfld = GetField(pField);

                if (tfld != null)
                    pField = tfld.FieldName; // actual field name from structdef

                string val = GetFieldValue(fname, rowno);

                // sList.Add(pField + '=' + val)
                sList.Add(new KeyValuePair<string, string>(pField, val));

                i++;
                pField = GetNthString(pFields, i)?.Trim() ?? "";
            }
        }

        public int GetParentActiveRow(int rowNo, int popIndex)
        {
            int result = -1;

            // popgrid := pPopgrid(sd.structdef.popgrids[popindex]);
            var popgrid = PopGrids[popIndex];

            string s = popgrid.Parent;

            // pfrmno := StrToInt(copy(s,3,3));
            int pfrmno = int.Parse(s.Substring(2, 3));

            // ParentList := TStringList.create;
            var parentList = new List<KeyValuePair<string, string>>();

            // GetParentValue(SD,popgrid.FrameNo,RowNo,popgrid.ParentField,ParentList);
            GetParentValue(popgrid.FrameNo, rowNo, popgrid.ParentField, parentList);

            int rc = GetRowCount(pfrmno);

            for (int i = 1; i <= rc; i++)
            {
                bool validRow = false;

                for (int j = 0; j < parentList.Count; j++)
                {
                    string fieldName = parentList[j].Key;
                    string parentValue = parentList[j].Value;

                    var tfld = GetField(fieldName);

                    string currentValue = GetFieldValue(fieldName, i);

                    if (tfld.DataType == "n")    // numeric comparison
                    {
                        if (string.IsNullOrEmpty(currentValue) ||
                            double.Parse(currentValue) != double.Parse(parentValue))
                        {
                            validRow = false;
                            break;
                        }
                        else
                        {
                            validRow = true;
                        }
                    }
                    else                        // string comparison
                    {
                        if (currentValue != parentValue)
                        {
                            validRow = false;
                            break;
                        }
                        else
                        {
                            validRow = true;
                        }
                    }
                }

                if (validRow)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        //public string GetFieldValueFromParent(string fieldName, int rowNo)
        //{
        //    if (ParentStructDef == null) return ""; 
        //    return ParentStructDef.GetFieldValue(fieldName, rowNo); 
        //}


        //public string GetFieldValueFromParent(string fieldName, int rowNo)
        //{
        //string result = "";

        //idValue = 0;
        //oldFieldValue = "";
        //oldIdValue = 0;
        //parentTransid = "";
        //parentFldName = "";

        //// If working on query → direct DB access
        //if (workOnQuery)
        //{
        //    return providerQuery.FieldByName(fieldName).AsString;
        //}

        //// Choose correct StoreData
        //TStoreData sd;

        //if (structDefRec.SourceTransId == Transid)
        //{
        //    sd = ParentStoreData;
        //}
        //else
        //{
        //    int k = structDefRec.SourceIndex;
        //    if (k == -1)
        //        return result;

        //    sd = ((StoreDataRecWrapper)StoreDataList[k]).StoreData;
        //}

        //// Lookup field definition
        //var tfld = sd.StructDef.GetField(fieldName);
        //int kf;

        //// ------------------------------------------------------------------
        //// Case 1: Field exists in StructDef
        //// ------------------------------------------------------------------
        //if (tfld != null)
        //{
        //    int rno;

        //    if (tfld.AsGrid)
        //    {
        //        rno = rowNo;

        //        // Handle popup parent-row switching
        //        if (structDefRec.SourcePopIndex > -1 &&
        //            tfld.FrameNo == structDefRec.PopParentFrameNo)
        //        {
        //            rno = GetParentActiveRow(sd, rowNo, structDefRec.SourcePopIndex);
        //            if (rno == -1)
        //                rno = rowNo;
        //        }

        //        kf = sd.GetFieldIndex(fieldName, rno);
        //    }
        //    else
        //    {
        //        kf = sd.GetFieldIndex(fieldName, 1);
        //    }
        //}
        //// ------------------------------------------------------------------
        //// Case 2: Field not found — check for implicit parent id fields
        //// ------------------------------------------------------------------
        //else
        //{
        //    kf = -1;

        //    for (int j = 0; j < sd.StructDef.Frames.Count; j++)
        //    {
        //        var frame = sd.StructDef.Frames[j];

        //        // Check match like "<TableName>ID"
        //        if (fieldName.Trim().ToLower() ==
        //            (frame.TableName.Trim() + "id").ToLower())
        //        {
        //            int rno;

        //            if (frame.AsGrid)
        //            {
        //                rno = rowNo;

        //                if (structDefRec.SourcePopIndex > -1 &&
        //                    frame.FrameNo == structDefRec.PopParentFrameNo)
        //                {
        //                    rno = GetParentActiveRow(sd, rowNo, structDefRec.SourcePopIndex);
        //                    if (rno == -1)
        //                        rno = rowNo;
        //                }
        //            }
        //            else
        //            {
        //                rno = 1;
        //            }

        //            // Return ParentDocId value
        //            result = sd.GetParentDocId(frame.FrameNo, rno).ToString();
        //            break;
        //        }
        //    }
        //}

        //// ------------------------------------------------------------------
        //// Field exists in the actual data
        //// ------------------------------------------------------------------
        //if (kf >= 0)
        //{
        //    var prec = (FieldRec)sd.FieldList[kf];

        //    result = prec.Value;
        //   idValue = prec.IdValue;
        //    _oldFieldValue = prec.OldValue;
        //    oldIdValue = prec.OldIdValue;

        //    tfld = sd.StructDef.GetField(fieldName);

        //    // Handle PostAutoGen rule
        //    if (sd.StructDef.AxProvider.dbm.gf.PostAutoGen
        //        && tfld != null
        //        && tfld.ModeOfEntry == "autogenerate")
        //    {
        //        parentTransid = sd._transType;
        //        parentFldName = fieldName;
        //    }
        //}

        //return result;
        //}

        public bool IsFieldExists(string fieldName, int rowNo)
        {
            // Default result is false
            bool result = false;

            // If this flag is set, Delphi exits immediately
            //if (workOnQuery)
            //    return result;

            //StructDef sdef = _parentStructDef;

            // Choose correct StoreData based on SourceTransId
            //if (structDefRec.SourceTransId == Transid)
            //{
            //    if (ParentStructDef != null)
            //           sd = ParentStructDef;
            //}
            //else
            //{
            //    //int k = structDefRec.SourceIndex;
            //    int k = structDefRec.SourceIndex;
            //    if (k == -1)
            //        return result;

            //    sd = StoreDataList[k]).StoreData;
            //}

            int kf = GetFieldIndex(fieldName, rowNo);

            return (kf >= 0);
        }

        public bool ValidRow(int rowNo, GenMapField genMapField)
        {
            bool result = true;

            //string rowControlField = map?.Attributes?["control"]?.Value ?? "";
            string rowControlField = genMapField.Control;
            if (string.IsNullOrEmpty(rowControlField))
                return result;

            Int64 fid = _genIdValue;

            if (!IsFieldExists(rowControlField, rowNo))
                rowNo = 1;

            string fv = GetFieldValueFromParent(rowControlField, rowNo)?.ToLower() ?? "";

            result = (fv == "t");

            //idValue = fid;

            _rowExists = (_oldFieldValue?.ToLowerInvariant() == "t") && (fv != "t");

            return result;
        }

        public void FillSpecificRow(GenMapField genMapField, int sourceRowNo)
        {
            //StructDef sdef = _parentStructDef;
            //TField fld = _parentStructDef.GetField(genMapField.TargetField);
            TField fld = GetField(genMapField.TargetField);

            if (fld == null) return;
            string fieldName = fld.FieldName;
            string dataType = fld.DataType;
            string saveTableName = fld.Tablename;
            int frameNo = fld.FrameNo;
            bool sourceKey = fld.SourceKey;
            var customDataType = fld.CustType;


            bool validity;
            string s, fv;
            int i;
            int x = 0;

                _parentRowNo = sourceRowNo;
            //if (map.Attributes["type" == "field" && Convert.ToInt32(map.Attributes["srow"]) == 0)
                if (genMapField.Type == "field" && Convert.ToInt32(genMapField.SourceRow) == 0)
                {

                    GetGenFieldValue(fieldName, _parentRowNo, genMapField);
                    validity = ValidRow(_parentRowNo, genMapField);
                }
                else
                {
                    //int srow = Convert.ToInt32(map.Attributes["srow"]);
                    int srow = genMapField.SourceRow;
                    GetGenFieldValue(fieldName, srow, genMapField);

                    if (srow != 0)
                        validity = ValidRow(srow, genMapField);
                    else
                        validity = ValidRow(_parentRowNo, genMapField);
                }


                // --- Numeric auto ID handling ---
                if (dataType == "n" && _genIdValue > 0)
                {
                    _genFieldValue = _genIdValue.ToString();
                    _genIdValue = 0;
                }
                //else if(dataType == "d")
                //{
                //    _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, clientDateFormat);
                //}

                if (!fld.SourceKey)
                {
                    _genIdValue = 0;
                }
                else if (genMapField.IsID)
                {
                    _genIdValue = Int64.Parse(_genFieldValue);

                    _genFieldValue = string.Empty;
                }

                // --- Compute TRow (x) while skipping invalid rows ---
                //if (map != null || map.Attributes != null)
                //x = Convert.ToInt32(map?.Attributes?["trow"]?.Value);
                x = Convert.ToInt32(genMapField.TargetRow);
                //for (i = 1; i <= Convert.ToInt32(map?.Attributes?["trow"]?.Value) - 1; i++)
                for (int j = 1; j <= x - 1; j++)
                {
                    //s = structObj.axprovider.dbm.gf.Pad(frameNo.ToString(), 2, ' ')
                    //    + structObj.axprovider.dbm.gf.Pad(i.ToString(), 2, ' ');
                    s = _utils.Pad(frameNo.ToString(), 2, ' ')
                        + _utils.Pad(j.ToString(), 2, ' ');



                    if (_invalidRows.Contains(s))
                        x--;
                }

            // --- Find or add field to FieldsAdded ---
            int fpos = _fieldsAdded.IndexOf(fieldName);
            if (fpos == -1)
            {
                _fieldsAdded.Add(fieldName);
                _rowsAdded.Add("0");
                fpos = _fieldsAdded.Count - 1;
            }

            // --- Exit if invalid row ---
            if (!validity)
                {
                    //s = structObj.axprovider.dbm.gf.Pad(frameNo.ToString(), 2, ' ')
                    //    + structObj.axprovider.dbm.gf.Pad(map.Attributes["trow"], 2, ' ');
                    //s = _utils.Pad(frameNo.ToString(), 2, ' ')
                    //   + _utils.Pad(map?.Attributes?["trow"]?.Value, 2, ' ');
                    s = _utils.Pad(frameNo.ToString(), 2, ' ')
                       + _utils.Pad(genMapField.TargetRow.ToString(), 2, ' ');

                    if (!_invalidRows.Contains(s))
                    {
                        _invalidRows.Add(s);

                        if (_rowExists)
                            DeleteRow(frameNo, x);
                    }
                    return;
                }

                // --- Update max row list ---
                //if (int.Parse(structDefRec.MaxRowList[frameNo - 1]) < x)
                //    structDefRec.MaxRowList[frameNo - 1] = x.ToString();

                _rowsAdded[fpos] = x.ToString();

                // --- Auto-generate mode field value ---
                if (fld.ModeofEntry == "autogenerate")
                {
                    fv = GetFieldValue(fieldName, 1);
                    if (!string.IsNullOrEmpty(fv))
                        _genFieldValue = fv;
                }

                // --- Save field ---
                //SetFieldValue(
                //     fieldName,
                //     dataType,
                //     saveTableName,
                //     _genFieldValue,
                //     "",
                //     x,
                //     _genIdValue,
                //     0,
                //     frameNo,
                //     0,
                //     sourceKey
                // );

                SubmitValue(
                        fieldName,
                        x,
                        _genFieldValue,
                        "",
                        _genIdValue,
                        0,
                        0
                    );

                // NOTE: Handle Parser here
                //structDefRec.Validate.Parser.RegisterVar(fieldName, dataType[0], fieldValue);
                AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValue);

                //structObj.axprovider.dbm.gf.DoDebug.Msg(
                //    $"Mapped {fieldName} Row = {x} value = {FieldValue} idvalue {IdValue}"
                //);
            //}
        }

        public int GetSourceFrameNo(TField field)
        {
            int result = 0;
            int fno = field.FrameNo;
            TField fpb = null;

            // Process every GenMap field, as the Delphi code processes
            // every child node.
            foreach (GenMapField genMapField in _currentGenMappings)
            {
                if (genMapField == null)
                    continue;

                string typeAttr = genMapField.Type;
                int trow = genMapField.TargetRow;

                if (typeAttr == "field" && trow == 0)
                {
                    string tfname = (genMapField.TargetField ?? "").ToLowerInvariant();
                    string sfname = (genMapField.SourceField ?? "").ToLowerInvariant();

                    if (string.IsNullOrEmpty(tfname) || string.IsNullOrEmpty(sfname))
                        continue;

                    // Equivalent to:
                    // StoreDataRec.StoreData.structdef.GetField(tfname)
                    TField fpa = GetField(tfname);

                    if (fpa == null || fpa.FrameNo != fno)
                        continue;

                    // Equivalent to:
                    // sd.structdef.GetField(sfname)
                    fpb = _parentStructDef.GetField(sfname);

                    if (fpb != null && fpb.AsGrid)
                    {
                        result = fpb.FrameNo;
                        break;
                    }
                    else
                    {
                        // Check whether the source field matches a frame's TableName + "id".
                        for (int j = 0; j < _parentStructDef.Frames.Count; j++)
                        {
                            var frame = _parentStructDef.Frames[j];

                            if (string.Equals(
                                sfname,
                                frame.TableName.Trim() + "id",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                result = frame.FrameNo;
                                break;
                            }
                        }
                    }

                }


            }

            // Preserve the final Delphi behavior.
            if (fpb != null && result == 0)
                result = fpb.FrameNo;

            return result;
        }
        public void FillGroupedGridToGrid(GenMapField genMapField)
        {
            //StructDef? sdef = _parentStructDef;

            //if (_parentStructDef == null) return;
            var fld = GetField(genMapField.TargetField);

            if (fld == null) return;
            string fieldName = fld.FieldName;
            string dataType = fld.DataType;
            string saveTableName = fld.Tablename;
            int frameNo = fld.FrameNo;
            bool sourceKey = fld.SourceKey;
            string customDataType = fld.CustType;

            int i = 0, k = 0, fpos = 0, fno = 0;
            int r = 0;
            string groupFieldValue;



            //if (StoreDataRec.SourceTransId.ToLower() ==Transid.ToLower())
            //if (ParentStructDef?.Transid.ToLower() ==Transid.ToLower())
            //{
            //    SD = ParentStructDef;
            //}
            //else
            //{
            //    k = StoreData.SourceIndex;
            //    //SD = ((StoreDataRecWrapper)StoreDataList[k]).StoreData;
            //    SD = _structDefList[k];
            //}

            fno = GetSourceFrameNo(fld);
            int maxRow = _parentStructDef.GetRowCount(fno);

            _groupFieldValues.Clear();
            _genFieldValueList.Clear();
            _genIdValueList.Clear();

            // --------- Collect grouped values ----------
            for (i = 1; i <= maxRow; i++)
            {
                if (!ValidRow(i, genMapField))
                    continue;

                GetGenFieldValue(fieldName, i, genMapField);

                groupFieldValue = GetFieldValueFromParent(genMapField.Group, i);
                k = _groupFieldValues.IndexOf(groupFieldValue);

                if (k == -1)
                {
                    _groupFieldValues.Add(groupFieldValue);

                    if (dataType == "n")
                        _genFieldValueList.Add("0");
                    else
                        _genFieldValueList.Add("");

                    _genIdValueList.Add(0);
                    k = _groupFieldValues.Count - 1;
                }

                //GetFieldValue(map.Name, i);
                GetGenFieldValue(genMapField.TargetField, i, genMapField);

                // ID logic
                if (!fld.SourceKey)
                {
                    _genIdValue = 0;
                }
                //else if (map.Attributes["id"].Value == "True")
                else if (genMapField.IsID)
                {
                    _genIdValue = Int64.Parse(_genFieldValue);
                    _genFieldValue = "";
                }

                // Grouped number aggregation or normal value
                if (dataType == "n" && _genFieldValue != "")
                {
                    double sum = double.Parse(_genFieldValueList[k]) + double.Parse(_genFieldValue);
                    _genFieldValueList[k] = sum.ToString();
                }
                //else if (dataType == "d")
                //{
                //    _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, clientDateFormat);
                //}
                else if (_genFieldValueList[k] == "")
                {
                    _genFieldValueList[k] = _genFieldValue;
                }

                _genIdValueList[k] = _genIdValue;
            }

            // ---- Write aggregated rows ----
            fpos = _fieldsAdded.IndexOf(fieldName);
            if (fpos == -1)
            {
                _fieldsAdded.Add(fieldName);
                _rowsAdded.Add("0");
                fpos = _fieldsAdded.Count - 1;
            }

            r = int.Parse(_rowsAdded[fpos]);

            for (i = 0; i < _genFieldValueList.Count; i++)
            {
                r++;

                //SetFieldValue(
                //    fieldName,
                //    dataType,
                //    saveTableName,
                //    _genFieldValueList[i],
                //    "",
                //    r,
                //    _genIdValueList[i],
                //    0,
                //    frameNo,
                //    0,
                //    sourceKey
                //);

                SubmitValue(
                   fieldName,
                   r,
                  _genFieldValueList[i],
                   "",
                   _genIdValueList[i],
                   0,
                   0
               );

                //    SetFieldValue(
                //    fieldName,
                //    dataType,
                //    saveTableName,
                //    _genFieldValueList[i],
                //    "",
                //    r,
                //    _genIdValueList[i],
                //    0,
                //    frameNo,
                //    0,
                //    sourceKey
                //);

                //structDefRec.Validate.Parser.RegisterVar(
                //    fieldName,
                //    dataType[0],
                //    fieldValueList[i]
                //);

                AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValueList[i]);

                //SourceId = _parentStructDef.GetRecordId(fld.FrameNo, i);

                //structObj.axprovider.dbm.gf.DoDebug.Msg(
                //    $"Mapped {fieldName} Row = {r} value = {FieldValueList[i]} idvalue {IdValueList[i]}"
                //);
            }

            //RowsAdded[fpos] = r.ToString();

            // Update MaxRowList
            // Fix: This code should be fixed 
            // ----------Start-------------
            //if (int.Parse(structDefRec.MaxRowList[frameNo - 1]) < r)
            //{
            //    structDefRec.MaxRowList[frameNo - 1] = r.ToString();
            //}
            // --------------End------------
        }

        public void FillGridToGrid(GenMapField genMapField)
        {
            //StructDef? sdef = _parentStructDef;
            TField? fld = GetField(genMapField.TargetField!);
            if (fld == null) return;
            string? fieldName = fld?.FieldName;
            string? dataType = fld?.DataType;
            string? saveTableName = fld?.Tablename;
            int? frameNo = fld?.FrameNo;
            bool? sourceKey = fld?.SourceKey;
            string? customDataType = fld?.CustType;

            int k, i, r = 0;

            // Select correct StoreData
            //if (StoreDataRec.SourceTransId.ToLower() == fTransid.ToLower())
            //{
            //    SD = ParentStoreData;
            //}
            //else
            //{
            //    k = StoreDataRec.SourceIndex;
            //    SD = ((StoreDataRecWrapper)StoreDataList[k]).StoreData;
            //}

            int fno = GetSourceFrameNo(fld!);
            int? maxRow = _parentStructDef.GetRowCount(fno);

            // Find or add field
            int fpos = _fieldsAdded.IndexOf(fieldName);
            if (fpos == -1)
            {
                _fieldsAdded.Add(fieldName);
                _rowsAdded.Add("0");
                fpos = _fieldsAdded.Count - 1;
            }

            r = int.Parse(_rowsAdded[fpos]);

            // ------------------ MAIN LOOP ------------------
            for (i = 1; i <= maxRow; i++)
            {
                if (!ValidRow(i, genMapField))
                    continue;

                GetGenFieldValue(fieldName, i, genMapField);

                // If numeric auto-id value
                if (dataType == "n" && _genIdValue > 0)
                {
                    _genFieldValue = _genIdValue.ToString();
                    _genIdValue = 0;
                }
                //else if (dataType == "d")
                //{
                //    _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, clientDateFormat);
                //}                

                // ID logic
                if (!fld.SourceKey)
                {
                    _genIdValue = 0;
                }
                //else if (map.Attributes["id"] == "True")
                else if (genMapField.IsID)
                {
                    _genIdValue = Int64.Parse(_genFieldValue);
                    _genFieldValue = "";
                }

                r++;

                //SetFieldValue(
                //     fieldName,
                //     dataType,
                //     saveTableName,
                //     _genFieldValue,
                //     "",
                //     r,
                //     _genIdValue,
                //     0,
                //     frameNo,
                //     0,
                //     sourceKey
                // );

                SubmitValue(
                   fieldName,
                   r,
                   _genFieldValue,
                   "",
                   _genIdValue,
                   0,
                   0
               );

                //StoreDataRec.Validate.Parser.RegisterVar(
                //    fieldName,
                //    dataType[0],
                //    FieldValue
                //);

                AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValue);


                //structObj.axprovider.dbm.gf.DoDebug.Msg(
                //    $"Mapped {fieldName} Row = {r} value = {FieldValue} idvalue {IdValue}"
                //);

                // update max row count
                //if (int.Parse(StoreDataRec.MaxRowList[frameNo - 1]) < r)
                //{
                //    StoreDataRec.MaxRowList[frameNo - 1] = r.ToString();
                //}
            }

            // Save latest row index
            //RowsAdded[fpos] = r.ToString();
        }

        //public void DeleteRow(int frameNo, int row)
        //{
        //    int i, k, p, parentFrameNo, subrow;
        //    string delrows, subdc, subdrows;

        //    if (MDMap != null)
        //    {
        //        // MDMap.CompanyName = CompanyName;
        //        MDMap.GetFieldData = GetFieldData;
        //        MDMap.GetRowCount = GetRowCount;
        //        MDMap.NewTrans = NewTrans;

        //        if (GetRecordId(frameNo, row) > 0)
        //            MDMap.DeleteRow(ftranstype, frameNo, row);
        //    }

        //    FieldList.Sort(SortFieldListLoad);
        //    AfterSort();

        //    k = 0;
        //    subrow = -1;
        //    delrows = "";
        //    subdc = "";
        //    subdrows = "";

        //    for (i = 0; i < FieldList.Count; i++)
        //    {
        //        var fld = (FieldRec)FieldList[i];

        //        if (fld.RowNo == row && fld.FrameNo == frameNo)
        //        {
        //            fld.OldRow = fld.RowNo;
        //            fld.RowNo = -1;
        //            DeletedSubgridRedids(fld.RecordId.ToString(), fld.FrameNo.ToString());
        //        }

        //        if (fld.RowNo > row && fld.FrameNo == frameNo)
        //            fld.RowNo -= 1;

        //        p = SubFrames.IndexOf(fld.FrameNo.ToString());
        //        parentFrameNo = (p != -1) ? int.Parse(SubFramesParent[p]) : 0;

        //        if (parentFrameNo > 0 && parentFrameNo == frameNo)
        //        {
        //            if (fld.ParentRowNo == row)
        //            {
        //                if (k == 0)
        //                    k = GetSubFormRowCount(fld.FrameNo, fld.ParentRowNo);

        //                subrow = fld.RowNo;

        //                if (MDMap != null &&
        //                    !delrows.Contains(subrow + ",") &&
        //                    fld.RecordId > 0)
        //                {
        //                    MDMap.DeleteRow(ftranstype, fld.FrameNo, row);
        //                    delrows += subrow + ",";
        //                    subdrows += "d" + subrow + ",";
        //                    subdc = fld.FrameNo.ToString();
        //                }

        //                fld.OldRow = fld.RowNo;
        //                fld.RowNo = -1;
        //                fld.OldParentRow = fld.ParentRowNo;
        //                fld.ParentRowNo = -1;
        //            }
        //            else if (fld.ParentRowNo > row)
        //            {
        //                fld.ParentRowNo -= 1;
        //            }

        //            if (fld.RowNo > subrow && subrow != -1)
        //                fld.RowNo -= k;
        //        }
        //    }

        //    if (structdef.axprovider.dbm.gf.isservice)
        //    {
        //        if (ins_del_rows.Count > 0)
        //        {
        //            for (i = 0; i < ins_del_rows.Count; i++)
        //            {
        //                if (ins_del_rows.Names[i] == "DC" + frameNo)
        //                    ins_del_rows[i] = ins_del_rows[i] + "d" + row + ",";
        //                else
        //                    ins_del_rows.Add("DC" + frameNo + "=d" + row + ",");

        //                if (subdc != "")
        //                {
        //                    // delete(subdrows, length(subdrows), 1);
        //                    if (ins_del_rows.Names[i] == "DC" + subdc)
        //                        ins_del_rows[i] = ins_del_rows[i] + subdrows;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ins_del_rows.Add("DC" + frameNo + "=d" + row + ",");

        //            if (subdc != "")
        //            {
        //                // delete(subdrows, length(subdrows), 1);
        //                ins_del_rows.Add("DC" + subdc + "=" + subdrows + ",");
        //            }
        //        }
        //    }
        //}


        //public void DeleteRow(int frameNo, int row, bool init)
        //{
        //    int i, k, p, parentFrameNo, subrow;
        //    string delrows, subdc;

        //    if (MDMap != null)
        //    {
        //        MDMap.GetFieldData = GetFieldData;
        //        MDMap.GetRowCount = GetRowCount;
        //        MDMap.NewTrans = NewTrans;

        //        if (GetRecordId(frameNo, row) > 0)
        //            MDMap.DeleteRow(ftranstype, frameNo, row);
        //    }

        //    FieldList.Sort(SortFieldListLoad);
        //    AfterSort();

        //    k = 0;
        //    subrow = -1;
        //    delrows = "";
        //    subdc = "";

        //    for (i = 0; i < FieldList.Count; i++)
        //    {
        //        var fld = (FieldRec)FieldList[i];

        //        if (fld.RowNo == row && fld.FrameNo == frameNo)
        //        {
        //            fld.OldRow = fld.RowNo;
        //            fld.RowNo = -1;
        //            DeletedSubgridRedids(fld.RecordId.ToString(), fld.FrameNo.ToString());
        //        }

        //        if (fld.RowNo > row && fld.FrameNo == frameNo)
        //            fld.RowNo -= 1;

        //        p = SubFrames.IndexOf(fld.FrameNo.ToString());
        //        parentFrameNo = (p != -1) ? int.Parse(SubFramesParent[p]) : 0;

        //        if (parentFrameNo > 0 && parentFrameNo == frameNo)
        //        {
        //            if (fld.ParentRowNo == row)
        //            {
        //                if (k == 0)
        //                    k = GetSubFormRowCount(fld.FrameNo, fld.ParentRowNo);

        //                subrow = fld.RowNo;

        //                if (MDMap != null &&
        //                    !delrows.Contains(subrow + ",") &&
        //                    fld.RecordId > 0)
        //                {
        //                    MDMap.DeleteRow(ftranstype, fld.FrameNo, row);
        //                    delrows += subrow + ",";
        //                    subdc = fld.FrameNo.ToString();
        //                }

        //                fld.OldRow = fld.RowNo;
        //                fld.RowNo = -1;
        //                fld.OldParentRow = fld.ParentRowNo;
        //                fld.ParentRowNo = -1;
        //            }
        //            else if (fld.ParentRowNo > row)
        //            {
        //                fld.ParentRowNo -= 1;
        //            }

        //            if (fld.RowNo > subrow && subrow != -1)
        //                fld.RowNo -= k;
        //        }
        //    }

        //    if (!init)
        //        return;

        //    if (structdef.axprovider.dbm.gf.isservice)
        //    {
        //        if (ins_del_rows.IndexOf("DC" + frameNo + "=d*,") < 0)
        //            ins_del_rows.Add("DC" + frameNo + "=d*,");

        //        if (subdc != "")
        //            if (ins_del_rows.IndexOf("DC" + subdc + "=d*,") < 0)
        //                ins_del_rows.Add("DC" + subdc + "=d*,");
        //    }

        //    /*
        //    if ins_del_rows.Count > 0 then
        //    begin
        //      for i := 0 to ins_del_rows.Count - 1 do
        //      begin
        //         if ins_del_rows.Names[i] = 'DC'+inttostr(frameno) then
        //            ins_del_rows.Values[ins_del_rows.Names[i]] := 'd*,';
        //         if subdc <> '' then
        //         begin
        //            if ins_del_rows.Names[i] = 'DC'+subdc then
        //              ins_del_rows.Values[ins_del_rows.Names[i]] := 'd*,';
        //         end;
        //      end;
        //    end else begin
        //      ins_del_rows.Add('DC'+inttostr(frameno)+'=d*,');
        //      if subdc <> '' then ins_del_rows.Add('DC'+subdc+'=d*,');
        //    end;
        //    */
        //}


        //public TGenMap GetGenMap(string genMapName)
        //{
        //    return GenMaps?.FirstOrDefault(gm => gm.Name.Equals(genMapName, StringComparison.OrdinalIgnoreCase));
        //}

        //public List<TGenMap> GetGenMapsByTarget(string targetTransId)
        //{
        //    return GenMaps?.Where(gm => gm.TargetTransId.Equals(targetTransId, StringComparison.OrdinalIgnoreCase)).ToList()
        //           ?? new List<TGenMap>();
        //}

        public List<string> GetChildTransAsList()
    => GenMaps?.Select(g => g.TargetTransId).ToList() ?? new List<string>();

        public List<TGenMap> GetChildGenMaps()
            => GenMaps?.ToList() ?? new List<TGenMap>();

        public string GetGenMapName() => GenMaps?
            .FirstOrDefault(gm => gm.TargetTransId.Equals(Struct.TransId, StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;

        public string GetGenMapName(StructDef parentStructDef) => parentStructDef.GenMaps?
            .FirstOrDefault(gm => gm.TargetTransId.Equals(Struct.TransId, StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;

        public void DeleteRow(int dcNo, int RowNo)
        {
            for (int i = 0; i < FieldList.Count; i++)
            {
                if (FieldList[i].FrameNo == dcNo && FieldList[i].RowNo == RowNo)
                {
                    FieldList[i].OldRow = FieldList[i].RowNo;
                    FieldList[i].RowNo = -1;
                    SubmitValue(FieldList[i].FieldName, -1, "", FieldList[i].OldValue, 0, FieldList[i].OldIdValue, 0);
                }

                if (FieldList[i].FrameNo == dcNo && FieldList[i].RowNo > RowNo)
                {
                    FieldList[i].RowNo = FieldList[i].RowNo - 1;
                }
            }
        }

        public void SaveGridGenMap(StructDef parentStructDef, TGenMap currentGenMap = null)
        {
            if (parentStructDef == null) // || parentStructDef._xml == null)
                throw new ArgumentNullException(nameof(parentStructDef), "Parent structdef is not assigned.");

            _parentStructDef = parentStructDef;
            currentGenMap ??= _parentStructDef.GenMaps?.FirstOrDefault(gm => gm.TargetTransId.Equals(Struct.TransId, StringComparison.OrdinalIgnoreCase));
            _currentGenMappings = currentGenMap.FieldMappings;

            if (currentGenMap == null)
                throw new InvalidOperationException($"No genmap found for target transid {Struct.TransId}.");

            MapName = currentGenMap.Name;
            //SourceId = _parentStructDef.LastSavedRecordid;

            if (AxParser == null) 
                throw new ArgumentNullException(nameof(AxParser), "AxParser not assigned");

            foreach (var genFld in currentGenMap.FieldMappings)
            {

                if (ShouldProcessAsSpecificRow(genFld))
                {
                    //FillSpecificRow(genFld);
                    continue;
                }
                else if (ShouldProcessAsGroupedGrid(genFld))
                {
                    FillGroupedGridToGrid(genFld);
                }
                else
                {
                    FillGridToGrid(genFld);
                }
            }

            // [feat]: Adds rowAction "create" to each new row in genMap edit if not added
            foreach (var frm in Frames)
            {
                AddRowAction(frm);
            }

        }

        public void AddRowAction(TFrame frame)
        {
            //var frame = Frames[frameNo];
            var frameNo = frame.FrameNo; ;

            for (int i = 1; i <= frame.RowCount; i++)
            {
                var curRowAction = GetRowAction(frameNo, i); // i = row number

                if (string.IsNullOrEmpty(curRowAction))
                {
                    var rowAction = "create";

                    if (frame.AsGrid)
                    {
                        SetGridRowAction(frameNo, i, rowAction);
                    }
                    else
                    {
                        SetNonGridRowAction(frame.TableName, rowAction);
                    }
                }
            }
        }

        public float StrToFloatZ(string s)
        {
            float result;
            if (string.IsNullOrEmpty(s))
            {
                result = 0;
            }
            else
            {
                result = float.Parse(s);
            }

            return result;
        }

        public Int64 StrToInt64(string s)
        {
            Int64 result;
            if (string.IsNullOrEmpty(s))
            {
                result = 0;
            }
            else
            {
                result = Int64.Parse(s);
            }

            return result;
        }

        public int StrToIntZ(string s)
        {
            int result;
            if (string.IsNullOrEmpty(s))
            {
                result = 0;
            }
            else
            {
                result = int.Parse(s);
            }

            return result;
        }

        public string GetNthString(string SrcString, int StrPos)
        {
            string result = "";
            int i = 0;
            int k = 1;

            while (k <= StrPos && i < SrcString.Length)
            {
                if (SrcString[i] == ',')
                {
                    k++;
                }
                else if (k == StrPos)
                {
                    result += SrcString[i];
                }
                i++;
            }

            return result;
        }

        public string GetNthString(string SrcString, int StrPos, char Separator)
        {
            string result = "";
            int i = 0;
            int k = 1;

            while (k <= StrPos && i < SrcString.Length)
            {
                if (SrcString[i] == Separator)
                {
                    k++;
                }
                else if (k == StrPos)
                {
                    result += SrcString[i];
                }
                i++;
            }

            return result;
        }

        public string GetNthString(string SrcString, int StrPos, string Separator)
        {
            if (Separator.Length == 1)
            {
                return GetNthString(SrcString, StrPos, Separator[0]);
            }
            else
            {
                string result = "";
                int i = 0;
                int k = 1;
                int searchLength = Separator.Length;

                while (k <= StrPos && i < SrcString.Length)
                {
                    if (
                        i + searchLength <= SrcString.Length
                        && SrcString.Substring(i, searchLength) == Separator
                    )
                    {
                        k++;
                        i += searchLength;
                    }
                    else if (k == StrPos)
                    {
                        result += SrcString[i];
                        i++;
                    }
                    else
                    {
                        i++;
                    }
                }

                return result;
            }
        }

        public bool IsKeyword(string fname)
        {
            var _keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string keywordList =
                "ADD,ALTER,AND,ANY,AS,ASC,AUDIT,BETWEEN,BY,CHAR,CHECK,CLUSTER,COLUMN,COMMENT"
                + ",COMPRESS,CONNECT,CREATE,CURRENT,DATE,DECIMAL,DEFAULT,DELETUE"
                + ",DESC,DISTINCT,DROP,ELSE,EXCLUSIVE,EXISTS,FILE,FLOAT,FOR,FROM"
                + ",GRANT,GROUP,HAVING,IDENTIFIED,IMMEDIATE,IN,INCREMENT,INDEX,INITIAL"
                + ",INSERT,INTEGER,INTERSECT,INTO,IS,LEVEL,LIKE,LOCK,LONG,MAXEXTENTS,MINUS"
                + ",MODE,MODIFY,NOAUDIT,NOCOMPRESS,NOT,NOWAIT,NULL,NUMBER,OF,OFFLINE,OPTION"
                + ",OR,ORDER,PCTFREE,PRIOR,PRIVILEGES,PUBLIC,RAW,RENAME,RESOURCE,REVOKE"
                + ",ROW,ROWID,ROWLABEL,ROWNUM,ROWS,SELECT,SESSION,SET,SHARE,SIZE,SMALLINT"
                + ",START,SUCCESSFUL,SYNONYM,SYSDATE,TABLE,THEN,TO,TRIGGER,UID,UNION"
                + ",UNIQUE,USER,VALIDATE,VALUES,VARCHAR,VARCHAR2,VIEW,WHENEVER,WHERE,WITH"
                + ",SIZE,SELECT,DROP,AS,IN,ADD,UPDATE,ALTER,FIELD,COLUMN,ORDER,VIEW"
                + ",TABLE,TAB,FROM,DATABASE,VALUES,ORDER BY,DEFAULT,VALUE,JOINT,PASSWORD"
                + ",GROUP,NO,NOT,TO,FieldChanged,GetRCell,Upper,Lower,DTOC"
                + ",CTOD,RND,ROUND,STUFF,IIF,AmtWord,Val,STR,SUBSTR,Convert,RevConvert"
                + ",Encode,Decode,HashChar,MandY,DaysElapsed,TimeElapsed,AddToTime"
                + ",AddToDate,AddToMonth,IsEmptyValue,LastDayOfMonth,ValidEncodeDate"
                + ",Eval,IsEmpty,Abs,MakeDate,Date,FormatDateTime,Time,DayofDate"
                + ",MonthofDate,YearofDate,Gen_id,Total,Sum,GetMax,GetValue,GetId"
                + ",GetRow,GetRowCount,GetCostRate,GetAdjustedAmount,SumTill"
                + ",GetCell,GETINTEGER,Power,CreatedBy,CreatedOn"
                + ",AxpCeil,AxpFloor,Days360,data";

            foreach (var keyword in keywordList.Split(','))
            {
                _keywords.Add(keyword.Trim().ToUpper());
            }

            return _keywords.Contains(fname?.ToUpper() ?? string.Empty);
        }

        /// <summary>
        /// Generates SQL & prepares parameter for MDMap
        /// </summary>
        public async Task<List<SQLCommandBuilder>> DoMDMapAsync()
        {
            foreach (var mdMap in MdMaps)
            {
                if (mdMap.FieldMappings.Count == 0)
                    continue;

                // Check control field
                if (!string.IsNullOrEmpty(mdMap.ControlField))
                {
                    TField controlField = GetField(mdMap.ControlField);
                    if (controlField != null)
                    {
                        string cfValue = GetFieldValue(controlField, mdMap.SourceFrame);
                        if (cfValue != "T") continue;
                    }
                }

                var command = await BuildMdMapOperationAsync(mdMap);
                if (command != null && (!string.IsNullOrEmpty(command.Query)))
                {
                    PreparedSqlOperations.Add(command);
                }
            }

            return PreparedSqlOperations;
        }

        /// <summary>
        /// Returns DbType for equelent .Net type
        /// </summary>
        public static DbType GetFldDbType(Type type)
        {
            // Handle nullable types by getting the underlying type
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

            if (_typeMap.TryGetValue(effectiveType, out DbType value))
            {
                return value;
            }
            else
            {
                throw new ArgumentException($"Type {effectiveType.Name} is not mapped to a DbType.");
            }
        }

        /// <summary>
        /// Gets companyname for the transId
        /// </summary>
        public async Task<string> GetCompanyNameAsync(string transId)
        {
            if (string.IsNullOrEmpty(transId)) return string.Empty;

            string sql = $"SELECT DISTINCT(schemaname) FROM axschema WHERE transid = @transId";

            try
            {
                var result = await _common._dbHelper?.ExecuteSQLAsync(sql,
                    new[] { "@transId" },
                    new[] { DbType.String },
                    new[] { $"'{transId.ToLower()}'" });

                if (result != null && result.data.Rows.Count > 0)
                {
                    DataRow row = result.data.Rows[0];
                    return GetRowValue(row, "schemaname");
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error getting company name: {ex.Message}");
                return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks the DbType is numeric or not
        /// </summary>
        public static bool IsNumericType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.Byte:
                case DbType.SByte:
                case DbType.Int16:
                case DbType.UInt16:
                case DbType.Int32:
                case DbType.UInt32:
                case DbType.Int64:
                case DbType.UInt64:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.Single:
                case DbType.VarNumeric:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Private Methods

        private async Task SetAutoGenList()
        {
            if (SdAutogenFields.Count > 0)
                return;
            if (!HasAutoGenFields)
                return;

            try
            {
                string seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();
                if (string.IsNullOrEmpty(seqtbl))
                    seqtbl = "Sequence";
                string sqlLower = GetSqlLower();

                string sql =
                    $"SELECT * FROM {CompanyName.ToUpper()}{seqtbl} "
                    + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(_transType.ToLower())} "
                    + "ORDER BY activesequence DESC";

                // Execute SQL using your DBHelper
                var result = await _common._dbHelper?.ExecuteSQLAsync(sql);

                if (result.data.Rows.Count > 0)
                {
                    foreach (DataRow row in result.data.Rows)
                    {
                        string prefix = GetRowValue(row, "prefix")?.ToLower() ?? "";
                        string prefixField = GetRowValue(row, "prefixfield")?.ToLower() ?? "";
                        string fieldName = GetRowValue(row, "fieldname")?.ToLower() ?? "";

                        _prefixes.Add(prefix);
                        _prefixFields.Add(prefixField);
                        SdAutogenFields.Add(fieldName);
                        _autogenTransid.Add(_transType.ToLower());
                        _autoGenVals.Add("");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error - log or throw
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error in SetAutoGenList: {ex.Message}");
            }
        }

        /// <param name="value">String value to quote</param>
        /// <returns>SQL-quoted string with escaped single quotes</returns>
        /// <example>QuotedStr("O'Reilly") returns "'O''Reilly'"</example>
        private string QuotedStr(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }

        /// <param name="row">DataRow to get value from</param>
        /// <param name="columnName">Column name to retrieve value from</param>
        /// <returns>Column value as string, empty if column doesn't exist</returns>
        /// <example>GetRowValue(dataRow, "CustomerName") returns "John Doe"</example>
        private string GetRowValue(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? row[columnName]?.ToString() ?? "" : "";
        }

        /// <param name="transid">Transaction ID for prefix lookup</param>
        /// <param name="fieldname">Field name to get prefix for</param>
        /// <returns>User-specific active prefix for the field</returns>
        /// <example>GetUserActivePrefix("SALES", "InvoiceNo") returns "INV-"</example>
        private async Task<string> GetUserActivePrefix(string transid, string fieldname)
        {
            try
            {
                string sqlLower = GetSqlLower();
                string username = UserName?.ToLower() ?? "";

                string wstr =
                    $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldname.ToLower())} "
                    + $"AND {sqlLower}(uname) = {QuotedStr(username)}";

                string sql = $"SELECT prefix FROM {CompanyName.ToUpper()}USERSEQUENCE WHERE {wstr}";

                // Execute the query
                var result = await _common._dbHelper?.ExecuteSQLAsync(sql);

                if (result.data.Rows.Count == 0)
                    return "";

                return GetRowValue(result.data.Rows[0], "prefix") ?? "";
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error in GetUserActivePrefix: {ex.Message}");
                return "";
            }
        }

        /// <param name="transid">Transaction ID for prefix lookup</param>
        /// <param name="fieldname">Field name to get prefix for</param>
        /// <returns>Active prefix for the field</returns>
        /// <example>GetActivePrefix("SALES", "InvoiceNo") returns "INV-"</example>
        private async Task<string> GetActivePrefix(string transid, string fieldname)
        {
            try
            {
                string sqlLower = GetSqlLower();
                string username = UserName?.ToLower();

                // First query: Check user sequence
                string wstr =
                    $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldname.ToLower())} "
                    + $"AND {sqlLower}(uname) = {QuotedStr(username)}";

                string sql = $"SELECT prefix FROM {CompanyName.ToUpper()}USERSEQUENCE WHERE {wstr}";
                var userSeqResult = await _common._dbHelper?.ExecuteSQLAsync(sql);

                string seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();
                if (string.IsNullOrEmpty(seqtbl))
                    seqtbl = "Sequence";

                // Second query: Get prefix field from sequence table
                sql =
                    $"SELECT prefixfield FROM {CompanyName.ToUpper()}{seqtbl} "
                    + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldname.ToLower())}";

                wstr =
                    $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldname.ToLower())}";

                if (userSeqResult.data.Rows.Count == 0)
                {
                    sql += " AND ACTIVESEQUENCE = 'T'";
                    wstr += " AND ACTIVESEQUENCE = 'T'";
                }
                else
                {
                    string userPrefix = GetRowValue(userSeqResult.data.Rows[0], "prefix") ?? "";
                    sql += $" AND prefix = {QuotedStr(userPrefix)}";
                    wstr += $" AND prefix = {QuotedStr(userPrefix)}";
                }

                // Execute the second query
                var seqResult = await _common._dbHelper?.ExecuteSQLAsync(sql);

                if (seqResult.data.Rows.Count == 0)
                    return "";

                return GetRowValue(seqResult.data.Rows[0], "prefixfield")?.Trim() ?? "";
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error in GetActivePrefix: {ex.Message}");
                return "";
            }
        }

        /// <param name="list">TStringList to search in</param>
        /// <param name="key">Key to find in the list</param>
        /// <returns>Index of key in list, -1 if not found</returns>
        /// <example>FindIndexInList(fieldValList, "5") returns 4</example>
        //private int FindIndexInList(Dictionary<string,string> list, string key)
        //{
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        if (list[i].StartsWith(key + "="))
        //            return i;
        //    }
        //    return -1;
        //}

        private int FindIndexInList(Dictionary<string, string> dict, string key)
        {
            int i = 0;
            foreach (var kvp in dict)
            {
                if (kvp.Key == key)
                    return i;
                i++;
            }
            return -1; // key not found
        }

        /// <param name="list">TStringList to get value from</param>
        /// <param name="key">Key to find value for</param>
        /// <returns>Value associated with key, null if not found</returns>
        /// <example>GetValueFromList(fieldValList, "5") returns "John Doe"</example>
        private string GetValueFromList(List<string> list, string key)
        {
            foreach (string item in list)
            {
                if (item.StartsWith(key + "="))
                    return item.Substring(key.Length + 1);
            }
            return "";
        }

        private string GetValueFromList(Dictionary<string, string> list, string key)
        {
            if (list.TryGetValue(key, out string value))
                return value;

            return "";
        }

        /// <param name="parent">Parent XML node to search under</param>
        /// <param name="nodeName">Name of child node to find</param>
        /// <returns>Inner text of the node, empty string if not found</returns>
        /// <example>GetNodeValue(xmlNode, "Caption") returns "Customer Details"</example>
        private string GetNodeValue(XmlNode parent, string nodeName)
        {
            XmlNode node = parent?.SelectSingleNode(nodeName);
            return node?.InnerText ?? "";
        }

        /// <param name="node">XML node to get attribute from</param>
        /// <param name="attrName">Attribute name to retrieve</param>
        /// <returns>Attribute value, empty string if attribute doesn't exist</returns>
        /// <example>GetAttributeValue(xmlNode, "cat") returns "dc"</example>
        private string GetAttributeValue(XmlNode node, string attrName)
        {
            return node?.Attributes[attrName]?.Value ?? "";
        }

        /// <param name="fld">TField reference to convert mode of entry for</param>
        /// <example>ConvertMOE(ref field) converts "to be entered" to "accept"</example>
        private void ConvertMOE(ref TField fld)
        {
            if (fld.ModeofEntry == "to be entered")
                fld.ModeofEntry = "accept";
            else if (fld.ModeofEntry == "to be calculated")
                fld.ModeofEntry = "calculate";
            else if (fld.ModeofEntry == "from list" || fld.ModeofEntry == "from api")
                fld.ModeofEntry = "select";
            else if (fld.SourceKey)
                fld.ModeofEntry = "select";
            else if (fld.ModeofEntry?.StartsWith("from ") == true && !fld.SourceKey)
                fld.ModeofEntry = "fill";
            //else throw new NotSupportedException("Invalid mode of entry");
        }

        private async Task<XmlDocument> GetStructure(string sname, string name)
        {
            XmlDocument result = new XmlDocument();
            result.LoadXml("<root></root>");

            string whereClause;
            string paramValue;
            string paramType = "c"; // character type

            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            // Build WHERE clause and parameter value
            //if (string.IsNullOrEmpty(name))
            //{
            //    whereClause = "caption = :name";
            //    paramValue = caption;
            //}
            //else
            {
                //var dbVars = DBHelper.GetDBVars(_dbType);
                string sqlLower = _common._dbVars.ContainsKey("sqllower") ? _common._dbVars["sqllower"] : "LOWER";
                whereClause = $"{sqlLower}(name) = @name";
                paramValue = name.ToLower();
            }

            // Read the BLOB from database
            using (var memoryStream = new MemoryStream())
            {
                await ReadMemo("props", sname, whereClause, paramValue, paramType, memoryStream);

                if (memoryStream.Length == 0)
                {
                    result.LoadXml("<root/>");
                    return result;
                }

                memoryStream.Position = 0;

                if (sname == "tstructs")
                {
                    // Handle compressed data for tstructs
                    using (var decompressedStream = await DecompressStream(memoryStream))
                    {
                        decompressedStream.Position = 0;
                        using (var reader = new StreamReader(decompressedStream, Encoding.UTF8))
                        {
                            string xmlContent = (await reader.ReadToEndAsync()).Trim();
                            result.LoadXml(xmlContent);
                        }
                    }

                    // Apply product checking if needed
                    //result = CheckProduct(name, result);
                }
                else
                {
                    // For other structures, read directly
                    using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        string xmlContent = (await reader.ReadToEndAsync()).Trim();
                        result.LoadXml(xmlContent);
                    }
                }
            }

            return result;
        }

        private async Task ReadMemo(
            string fieldName,
            string table,
            string whereClause,
            string paramValue,
            string paramType,
            Stream outputStream
        )
        {
            try
            {
                // Determine field name based on database type
                string dbFieldName = fieldName;
                //var dbVars = DBHelper.GetDBVars(_dbType);

                // For PostgreSQL, we need to handle bytea data differently
                //if (_dbType.ToLower() == "postgre" || _dbType.ToLower() == "postgres")
                //{
                //    dbFieldName = $"encode({fieldName}, 'escape') as {fieldName}";
                //}
                //else if (table != "tstructs" && table != "axpertreports")
                //{
                //    if (dbVars.ContainsKey("MsDBverno") && dbVars["MsDBverno"] == "Above 2012")
                //    {
                //        dbFieldName = $"cast({fieldName} as text) as {fieldName}";
                //    }
                //}

                //dbFieldName = fieldName;

                string sql =
                    $"SELECT {dbFieldName} FROM {table} WHERE {whereClause} ORDER BY blobno";

                var sqlResult = await _common._dbHelper.ExecuteSQLAsync(
                    sql,
                    new[] { "@name" },
                    new[] { DbType.String },
                    new[] { (object)paramValue }
                );

                // Check for errors first
                if (!string.IsNullOrEmpty(sqlResult.error))
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogError($"SQL error: {sqlResult.error}");
                    return;
                }

                if (sqlResult.data == null)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage("DataTable is null in SQL result");
                    return;
                }

                if (sqlResult.data.Rows.Count == 0)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"No rows found for name: {paramValue}");
                    return;
                }

                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"Found {sqlResult.data.Rows.Count} rows for name: {paramValue}");

                // Process each row
                foreach (DataRow row in sqlResult.data.Rows)
                {
                    // Check if the field exists in the row
                    if (!sqlResult.data.Columns.Contains(fieldName))
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Field '{fieldName}' does not exist in the result");
                        continue;
                    }

                    // Check if the field value is null or DBNull
                    if (row[fieldName] == null || row[fieldName] == DBNull.Value)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Field '{fieldName}' is null or DBNull in row");
                        continue;
                    }

                    // Handle the data based on its actual type
                    object fieldValue = row[fieldName];

                    if (fieldValue is byte[] blobData)
                    {
                        // This is the most common case - byte array from database
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Found byte array of length: {blobData.Length}");

                        if (blobData.Length > 0)
                        {
                            await outputStream.WriteAsync(blobData, 0, blobData.Length);
                            if (_traceService.IsTracingEnabled())
                                _traceService.LogMessage(
                                    $"Successfully wrote {blobData.Length} bytes to stream"
                                );
                        }
                    }
                    else if (fieldValue is string stringData)
                    {
                        // Handle string data (might be encoded bytea from PostgreSQL)
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage(
                                $"Found string data: {stringData.Substring(0, Math.Min(50, stringData.Length))}..."
                            );

                        if (_common._dbType.ToLower() == "postgre" || _common._dbType.ToLower() == "postgres")
                        {
                            // PostgreSQL returns bytea as hex string or escaped text
                            byte[] blobDataFromString = ParsePostgreSQLBytea(stringData);
                            if (blobDataFromString != null && blobDataFromString.Length > 0)
                            {
                                await outputStream.WriteAsync(
                                    blobDataFromString,
                                    0,
                                    blobDataFromString.Length
                                );
                                if (_traceService.IsTracingEnabled())
                                    _traceService.LogMessage(
                                        $"Successfully wrote {blobDataFromString.Length} bytes from string"
                                    );
                            }
                        }
                        else
                        {
                            // For other databases, treat as UTF-8 text
                            byte[] stringBytes = Encoding.UTF8.GetBytes(stringData);
                            await outputStream.WriteAsync(stringBytes, 0, stringBytes.Length);
                        }
                    }
                    else
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogError($"Unexpected field type: {fieldValue.GetType().Name}");
                    }
                }
            }
            // Note: This May Need to be removed
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error reading memo: {ex.Message}");
                throw;
            }
        }

        // Helper method to parse PostgreSQL bytea data
        private byte[] ParsePostgreSQLBytea(string byteaData)
        {
            try
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage(
                        $"Parsing bytea data: {byteaData?.Substring(0, Math.Min(50, byteaData?.Length ?? 0))}..."
                    );

                if (string.IsNullOrEmpty(byteaData))
                    return null;

                // PostgreSQL bytea can be in hex format (starts with \x) or escape format
                if (byteaData.StartsWith("\\x"))
                {
                    // Hex format: \x0123456789ABCDEF
                    string hexData = byteaData.Substring(2);
                    return HexStringToByteArray(hexData);
                }
                else
                {
                    // Escape format - convert to bytes using UTF8 encoding
                    return Encoding.UTF8.GetBytes(byteaData);
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage("Error parsing PostgreSQL bytea");
                return null;
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            try
            {
                int length = hex.Length;
                byte[] bytes = new byte[length / 2];
                for (int i = 0; i < length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError("Error converting hex string to byte array");
                return null;
            }
        }

        private async Task<MemoryStream> DecompressStream(Stream compressedStream)
        {
            try
            {
                compressedStream.Position = 0;

                // Read the size of uncompressed data (first 4 bytes)
                byte[] sizeBytes = new byte[4];
                int bytesRead = await compressedStream.ReadAsync(sizeBytes, 0, 4);

                if (bytesRead != 4)
                {
                    throw new InvalidDataException(
                        "Invalid compressed stream format: missing size header"
                    );
                }

                // Convert bytes to size (little-endian)
                uint uncompressedSize = BitConverter.ToUInt32(sizeBytes, 0);

                // Use System.IO.Compression for ZLib decompression
                using (
                    var decompressionStream = new System.IO.Compression.ZLibStream(
                        compressedStream,
                        System.IO.Compression.CompressionMode.Decompress,
                        true
                    )
                )
                {
                    var decompressedStream = new MemoryStream();
                    await decompressionStream.CopyToAsync(decompressedStream);
                    decompressedStream.Position = 0;

                    // Verify we got the expected amount of data
                    if (decompressedStream.Length != uncompressedSize)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage(
                                $"Warning: Expected {uncompressedSize} bytes, got {decompressedStream.Length} bytes"
                            );
                    }

                    return decompressedStream;
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Decompression error: {ex.Message}");

                // Fallback: try GZip compression (some older formats might use this)
                try
                {
                    compressedStream.Position = 0;
                    using (
                        var gzipStream = new GZipStream(
                            compressedStream,
                            CompressionMode.Decompress,
                            true
                        )
                    )
                    {
                        var fallbackStream = new MemoryStream();
                        await gzipStream.CopyToAsync(fallbackStream);
                        fallbackStream.Position = 0;
                        return fallbackStream;
                    }
                }
                catch
                {
                    // Final fallback: return original data as-is
                    compressedStream.Position = 0;
                    var originalStream = new MemoryStream();
                    await compressedStream.CopyToAsync(originalStream);
                    originalStream.Position = 0;
                    return originalStream;
                }
            }
        }

        private bool IsGZipCompressed(Stream stream)
        {
            try
            {
                // Check for GZip magic number: 0x1F 0x8B
                byte[] header = new byte[2];
                long originalPosition = stream.Position;

                stream.Read(header, 0, 2);
                stream.Position = originalPosition;

                return header[0] == 0x1F && header[1] == 0x8B;
            }
            catch
            {
                return false;
            }
        }

        private string GetSqlLower()
        {
            //if (_common._dbHelper == null)
            //    await _common.OpenDBConnectionAsync();

            //var dbVars = DBHelper.GetDBVars(_common._dbHelper.GetDBType());

            return _common._dbVars.ContainsKey("sqllower") ? _common._dbVars["sqllower"] : "LOWER";
        }

        //private async Task<SQLResult> ExecuteSQLAsync(
        //    string sql,
        //    string[] paramNames = null,
        //    DbType[] paramTypes = null,
        //    object[] paramValues = null
        //)
        //{
        //    if (_common._dbHelper == null)
        //        await _common.OpenDBConnectionAsync();

        //    var sqlResult = await _common._dbHelper?.ExecuteSQLAsync(
        //        sql,
        //        paramNames,
        //        paramTypes,
        //        paramValues
        //    );

        //    return sqlResult;
        //}

        private async Task<NonQueryResult> ExecuteNonSQLAsync(
            string sql,
            string[] paramNames = null,
            DbType[] paramTypes = null,
            object[] paramValues = null
        )
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            NonQueryResult nonQueryResult = await _common._dbHelper.ExecuteNonQueryAsync(
                sql,
                paramNames,
                paramTypes,
                paramValues
            );

            return nonQueryResult;
        }

        private void GetPickLists(ref TField fd)
        {
            string sqlText = fd.SQLText;
            int i = sqlText.ToLower().IndexOf("dynamicfilter");
            fd.TxtSelection = i > 0;

            if (!fd.TxtSelection)
                return;

            if (sqlText.ToLower().IndexOf("dynamicfilterword") > 0)
                fd.WordSearch = true;

            //fd.Pickfields = new List<string>();
            //fd.Pickcaptions = new List<string>();

            bool firstTime = true;
            string fn = "";
            string captions = "";
            string caps = "";

            while (i > 0)
            {
                captions = sqlText.Substring(i);
                int j = captions.IndexOf("}");
                if (j > 0)
                {
                    captions = sqlText.Substring(i - 1, j + 1).Trim();

                    if (captions.ToLower().IndexOf("dynamicfilterword") > 0)
                        captions = captions.Substring(18);
                    else
                        captions = captions.Substring(14);

                    int tpos = captions.IndexOf("~");
                    if (tpos > 0)
                    {
                        string fnames = captions.Substring(0, tpos).Trim();
                        fd.Pickfields.Add(fnames);

                        if (firstTime)
                        {
                            fn = fnames;
                            caps = captions.Substring(tpos + 1).Trim();
                            firstTime = false;
                        }
                    }
                }

                sqlText = sqlText.Substring(i + j);
                i = sqlText.ToLower().IndexOf("dynamicfilter");
            }

            if (!string.IsNullOrEmpty(fn))
            {
                int index = 1;
                while (true)
                {
                    string f = GetNthString(fn, index);
                    if (string.IsNullOrEmpty(f))
                        break;

                    string f1 = GetNthString(caps, index);
                    if (string.IsNullOrEmpty(f1))
                        f1 = f;

                    fd.Pickcaptions.Add(f1);
                    index++;
                }
            }
        }

        private void GetDropDownPickLists(ref TField fd)
        {
            string sqlText = fd.SQLText;
            int i = sqlText.ToLower().IndexOf("dynamicfilter");

            // If dynamicfilter is found, exit
            if (i > 0)
                return;

            //fd.Pickfields = new List<string>();
            //fd.Pickcaptions = new List<string>();

            string s = sqlText.Trim().ToLower();
            s = s.Replace("  ", " ");

            string s1 = s.Substring(7); // Skip "SELECT "
            int n = 1;

            // Get the first word after SELECT
            string firstPart = GetNthString(s1, n).Trim() + " ";
            string s2 = GetNthString(firstPart, 1, " ");

            if (IsKeyword(s2))
            {
                s = GetNthString(firstPart, 2, " ").Trim();
            }
            else
            {
                s = s2.Trim();
            }

            int cpos = s.IndexOf(" as ");
            if (cpos < 0)
                cpos = s.IndexOf(" ");

            if (cpos > 0)
                s = s.Substring(0, cpos);

            int index = 1;
            while (true)
            {
                string f = GetNthString(s, index);
                if (string.IsNullOrEmpty(f))
                    break;

                fd.Pickfields.Add(f);
                index++;
            }
        }

        public bool ShouldProcessAsSpecificRow(GenMapField genFld)
        {
            return genFld.TargetRow > 0;
        }

        public bool ShouldProcessAsGroupedGrid(GenMapField genFld)
        {
            return !string.IsNullOrEmpty(genFld?.Group);
        }

        private bool ShouldProcessAsGridToGrid(GenMapField genFld)
        {
            return genFld.TargetRow == 0 && string.IsNullOrEmpty(genFld?.Group);
        }

        private void GetGenFieldValue(string fieldName, int rowNo, GenMapField genMapField)
        {
            //string nodeValue = _currentMap.InnerText.ToLower();
            //string fieldType = _currentMap.Attributes?["type"]?.Value ?? "";
            //string fieldType = _currentMap.Attributes?["type"]?.Value ?? "";

            //if (fieldName == "primaryrecordid")
            if (genMapField.SourceField == "primaryrecordid")
            {
                //_fieldValue = _parentPrimaryId.ToString();
                _genFieldValue = "primaryrecordid";
                _genIdValue = 0;
            }
            else if (genMapField.SourceField == "sourcerecordid")
            {
                _genFieldValue = "sourcerecordid";
                int fieldIndex = GetFieldIndex(fieldName, rowNo);

                if (fieldIndex != -1)
                {
                    //var fieldRec = FieldList[fieldIndex];
                    //if (!string.IsNullOrEmpty(fieldRec.OldValue))
                    //    _genFieldValue = fieldRec.OldValue;
                    _genFieldValue = GetOldFieldValue(fieldName, rowNo);
                }
                _genIdValue = 0;
            }
            else if (genMapField.SourceField == "parentrecordid")
            {
                _genFieldValue = "parentrecordid";
                _genIdValue = 0;
            }
            else if (genMapField.Type == "field")
            {
                _genFieldValue = GetFieldValueFromParent(genMapField.SourceField, rowNo);
                if (_parentStructDef.GetField(genMapField.SourceField).SourceKey)
                {
                    _genIdValue = _parentStructDef.GetIDValue(genMapField.SourceField, rowNo);
                }
                else
                {
                    _genIdValue = 0;
                }

                // Handle auto generation records
                //if (PostAutoGen && !string.IsNullOrEmpty(_parentTransId))
                //{
                //    if (IsParentAutoRecFound(_parentTransId, _parentFldName))
                //    {
                //        var autoRec = new AutoGenRec
                //        {
                //            TransId = _storeDataRec.StoreData._transType,
                //            FieldName = fieldName,
                //            RType = "dep",
                //            ParentTransId = _parentTransId,
                //            ParentFldName = _parentFldName,
                //            RowNo = 1,
                //            Schema = CompanyName,
                //            TableName = _currentField.Tablename,
                //            ParentList = null,
                //            RecordIdUpdated = false
                //        };

                //        _struct.AxProvider.Dbm.Gf.AutoGenData.Add(autoRec);
                //        AddDepsToAutoGenList(fieldName);
                //    }
                //}
            }
            else
            {
                //_genFieldValue = genMapField.TargetField;
                _genFieldValue = genMapField.SourceField;
                //string idValueStr = _currentMap.Attributes?["idvalue"]?.Value;
                //string idValueStr = _genIdValue.ToString();
                string idValueStr = genMapField.IdValue;
                _genIdValue = StrToInt64(idValueStr);
            }
        }

        private string GetFieldValueFromParent(string fieldName, int rowNo)
        {
            // This is Handle in SaveGenMap
            //if (_parentStructDef == null || _parentStructDef?._xml == null)
            //    return string.Empty;
            //throw new ArgumentNullException(nameof(_parentStructDef), "Parent structdef is not assigned.");

            return _parentStructDef?.GetFieldValue(fieldName, rowNo);
        }

        /// <summary>
        /// Gets the source frame number from MDMap list
        /// </summary>
        private int GetSourceFrameNo(XmlNode mapNode)
        {
            int result = -1;
            string operationType = GetNodeValue(mapNode, Consts.xml_Type);

            // For non-increment/decrement operations
            if (operationType != "Inc" && operationType != "Dec")
            {
                string detailField = GetNodeValue(mapNode, Consts.xml_DField);
                TField field = GetField(detailField);

                if (field == null)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"DetailField of MdMap does not exist: ({field})");
                    throw new Exception("DetailField of MdMap does not exist");
                }

                result = field.FrameNo;
                bool isGridField = field.AsGrid;

                // Check detail search field
                string detailSearchField = GetNodeValue(mapNode, Consts.xml_DSField);
                field = GetField(detailSearchField);

                if (field == null)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"DetailSearchField of MdMap does not exist: ({field})");
                    throw new Exception("DetailSearchField of MdMap does not exist");
                }

                if (field.AsGrid && !isGridField)
                {
                    result = field.FrameNo;
                    isGridField = true;
                }

                // Check control field
                string controlField = GetNodeValue(mapNode, Consts.xml_MDControl);
                if (!string.IsNullOrEmpty(controlField))
                {
                    field = GetField(controlField);

                    if (field == null)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogError($"ControlField of MdMap does not exist: ({field})");
                        throw new Exception("ControlField of MdMap does not exist");
                    }

                    if (field.AsGrid && !isGridField)
                    {
                        result = field.FrameNo;
                        isGridField = true;
                    }
                }
            }
            else
            {
                // For increment/decrement operations
                result = 1;
            }

            return result;
        }

        /// <summary>
        /// Parses operation type from string
        /// </summary>
        private OperationType ParseOperationType(string opType)
        {
            return opType.ToLower() switch
            {
                "add" => OperationType.Add,
                "less" => OperationType.Less,
                "highest" => OperationType.Highest,
                "lowest" => OperationType.Lowest,
                "inc" => OperationType.Inc,
                "dec" => OperationType.Dec,
                "replace" => OperationType.Replace,
                _ => OperationType.Normal
            };
        }

        /// <summary>
        /// Finds mdMap index based on criteria
        /// </summary>
        private int GetMdMapIndex(
            string transId,
            string tableName,
            string masterSearchField,
            string detailSearchField,
            string controlField
        )
        {
            for (int i = 0; i < MdMaps.Count; i++)
            {
                var mdMap = MdMaps[i];

                if (mdMap.TransId == transId &&
                    mdMap.Table == tableName &&
                    mdMap.ControlField == controlField &&
                    mdMap.DetailSearchField == detailSearchField &&
                    mdMap.MasterSearchField == masterSearchField)
                {
                        return i;                 
                }
            }

            return -1;
        }

        /// <summary>
        /// Parses and adds MDMap from AMDMap text format
        /// </summary>
        /// <param name="mapLine">Map line in format "key=value"</param>
        /// <param name="currentMdMap">Current MDMap being configured</param>
        private void ParseMdMap(string mapLine, TMdMap currentMdMap)
        {
            if (string.IsNullOrWhiteSpace(mapLine) || currentMdMap == null)
                return;

            var parts = mapLine.Split('=');
            if (parts.Length != 2)
                return;

            string key = parts[0].Trim().ToLower();
            string value = parts[1].Trim();

            switch (key)
            {
                case "transid":
                    currentMdMap.TransId = value.ToLower();
                    break;

                case "sourceframe":
                    if (int.TryParse(value, out int frameNo))
                        currentMdMap.SourceFrame = frameNo;
                    break;

                case "tablename":
                    currentMdMap.Table = value;
                    break;

                case "mastertransid":
                    currentMdMap.MasterTransId = value;
                    break;

                case "controlfield":
                    currentMdMap.ControlField = value;
                    break;

                case "append":
                    currentMdMap.Append = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                //case "treeupdate":
                //    currentMdMap.Tree = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                //    break;

                //case "treetable":
                //    currentMdMap.TreeTable = value;
                //    break;

                //case "treelink":
                //    currentMdMap.TreeLink = value;
                //    break;

                case "initondel":
                    currentMdMap.InitOnDel = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                default:
                    // Assume it's a field mapping: targetField=sourceField:decimalPlaces
                    ParseFieldMapping(key, value, currentMdMap);
                    break;
            }
        }

        /// <summary>
        /// Gets full table name with company/schema prefix
        /// </summary>
        private string GetFullTableName(TMdMap mdMap)
        {
            if (!string.IsNullOrEmpty(mdMap.CompanyName))
                return $"{mdMap.CompanyName}.{mdMap.Table}";
            return mdMap.Table;
        }

        /// <summary>
        /// Parses field mapping in format "targetField=sourceField:decimalPlaces"
        /// </summary>
        /// Need to update & verify
        private void ParseFieldMapping(string targetField, string mappingValue, TMdMap mdMap)
        {
            // Parse target field and operation
            char firstChar = targetField[0];
            OperationType operation = OperationType.Normal;
            string cleanTargetField = targetField;

            switch (firstChar)
            {
                case '@': // Master search field
                    mdMap.MasterSearchField = targetField.Substring(1);
                    mdMap.DetailSearchField = mappingValue;
                    return;

                case '+':
                    operation = OperationType.Add;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '-':
                    operation = OperationType.Less;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '>':
                    operation = OperationType.Highest;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '<':
                    operation = OperationType.Lowest;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '^':
                    operation = OperationType.Inc;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '!':
                    operation = OperationType.Dec;
                    cleanTargetField = targetField.Substring(1);
                    break;

                case '*':
                    operation = OperationType.Replace;
                    cleanTargetField = targetField.Substring(1);
                    break;

                default:
                    operation = OperationType.Normal;
                    break;
            }

            // Parse source field and decimal places
            var mappingParts = mappingValue.Split(':');
            string sourceField = mappingParts[0].Trim();
            int decimalPlaces = 0;

            if (mappingParts.Length > 1)
            {
                string decimalPart = mappingParts[1].Trim();
                if (decimalPart.Equals("_currdecimal", StringComparison.OrdinalIgnoreCase))
                {
                    decimalPlaces = GetCurrentDecimalPrecision();
                }
                else if (int.TryParse(decimalPart, out int parsedDecimal))
                {
                    decimalPlaces = parsedDecimal;
                }
            }
            //else if (!string.IsNullOrEmpty(_masterTransId))
            //{
            //    // Get decimal places from master transaction if specified
            //    decimalPlaces = GetFieldDecimalPrecision(_masterTransId, targetField);
            //}

            // Add field mapping
            if (!mdMap.FieldMappings.Any(f =>
                    //f.SourceField == sourceField &&
                    f.TargetField == cleanTargetField
            ))
            {
                mdMap.FieldMappings.Add(new MdMapField
                {
                    SourceField = sourceField,
                    TargetField = cleanTargetField,
                    Operation = operation,
                    DecimalPlaces = decimalPlaces
                });
            }

        }

        /// <summary>
        /// Gets current decimal precision from application settings
        /// </summary>
        /// Need to update & verify
        private int GetCurrentDecimalPrecision()
        {
            // This should come from the application configuration
            // For now, return a default value
            return 2;
        }

        /// <summary>
        /// Builds a complete MDMap operation with proper SQL and parameters
        /// </summary>
        private async Task<SQLCommandBuilder> BuildMdMapOperationAsync(TMdMap mdMap)
        {
            var columnNames = new List<string>();
            var values = new List<object>();
            var parameters = new List<string>();
            var fldDbTypes = new List<DbType>();

            SetSourceValues(mdMap);

            string targetTable = GetFullTableName(mdMap);
            string whereClause = $"WHERE {mdMap.MasterSearchField} = @DetailSearchValue";

            //columnNames.Add(mdMap.MasterSearchField);
            parameters.Add("@DetailSearchValue");

            var dsField = GetField(mdMap.DetailSearchField);
            var dbType = GetFieldDBType(dsField);

            object value;
            if (dsField.SourceKey)
            {
                value = GetIDValue(dsField, 1);
                dbType = DbType.Int64;
            }
            else
            {
                value = GetFieldValue(dsField, 1);
                value = ConvertValueByDbType(value, dbType);
            }

            values.Add(value);
            fldDbTypes.Add(dbType);

            //if (dsField.SourceKey)
            //    values.Add(GetIDValue(dsField, 1));
            //else
            //    values.Add(GetFieldValue(dsField, 1));

            //fldDbTypes.Add(GetFieldDBType(dsField));

            // Check if record exists
            string sql = $"SELECT * FROM {targetTable} {whereClause}";
            bool isRecordExist = false;
            SQLResult result = null;

            try
            {
                result = await _common._dbHelper?.ExecuteSQLAsync(sql, parameters.ToArray(), fldDbTypes.ToArray(), values.ToArray());

                if (result != null && result.data.Rows.Count > 0)
                {
                    isRecordExist = Convert.ToInt64(result.data.Rows[0][0]) > 0;
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error checking record existence: {ex.Message}");
            }

            if (!isRecordExist && !mdMap.Append)
                return null;

            string query = "";

            // Build SQL and parameters
            if (isRecordExist)
            {
                query = BuildUpdateSql(mdMap, targetTable, whereClause, columnNames, parameters, values, fldDbTypes, result.data);
            }
            else
            {
                query = BuildInsertSql(mdMap, targetTable, columnNames, parameters, values, fldDbTypes, result.data);
            }

            var sqlCommand =
                  new SQLCommandBuilder(
                    query,
                    parameters.ToArray(),
                    fldDbTypes.ToArray(),
                    values.ToArray(),
                    isRecordExist ? Constants.DATA_ACTION.UPDATE : Constants.DATA_ACTION.INSERT
                  );


            return sqlCommand;
        }

        /// <summary>
        /// Builds INSERT SQL with numeric column logic
        /// </summary>
        private string BuildInsertSql(
            TMdMap mdMap,
            string tableName,
            List<string> columnNames,
            List<string> parameters,
            List<object> values,
            List<DbType> fldDbTypes,
            DataTable data )
        {
            // Always include master search field
            //if (!string.IsNullOrEmpty(mdMap.MasterSearchField))
            //{
            //    columnNames.Add(mdMap.MasterSearchField);
            //    //parameters.Add("@detailSearchValue");
            //    //values.Add(value);
            //    //dbTypes.Add(dbType);
            //}

            bool masterFieldIsMapped = !string.IsNullOrEmpty(mdMap.MasterSearchField) &&
                                            mdMap.FieldMappings.Any(fm => string.Equals(
                                                fm.TargetField, mdMap.MasterSearchField, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(mdMap.MasterSearchField))
            {
                if (!masterFieldIsMapped)
                {
                    columnNames.Add(mdMap.MasterSearchField);
                }
                else
                {
                    // A FieldMapping already targets this column, so drop the
                    // earlier @DetailSearchValue placeholder to keep columnNames/
                    // parameters/values counts aligned (the mapping's own value wins).
                    int idx = parameters.IndexOf("@DetailSearchValue");
                    if (idx >= 0)
                    {
                        parameters.RemoveAt(idx);
                        values.RemoveAt(idx);
                        fldDbTypes.RemoveAt(idx);
                    }
                }
            }

            // Process each field mapping
            for (int i = 0; i < mdMap.FieldMappings.Count; i++)
            {
                var (columnClause, parameterClause, value, fldDbType) = FormatParams(mdMap.FieldMappings[i]);

                columnNames.Add(mdMap.FieldMappings[i].TargetField);
                parameters.Add(parameterClause);
                values.Add(value);
                fldDbTypes.Add(fldDbType);
            }

            foreach(DataColumn clm in data.Columns)
            {
                DbType fldDbType = GetFldDbType(clm.DataType);

                if (IsNumericType(fldDbType))
                {
                    if (!columnNames.Contains(clm.ToString()))
                    {
                        columnNames.Add(clm.ToString());
                        parameters.Add($"@{clm.ToString()}");

                        if (clm.ColumnName.ToLower() == tableName + "id")
                        {
                            var dt = GetRecordIdsFromDB(1).GetAwaiter().GetResult();
                            var recId = Convert.ToInt64(dt.data.Rows[0][0]);
                            values.Add(recId);
                            fldDbTypes.Add(DbType.Int64);
                        }
                        else
                        {
                            values.Add(0);
                            fldDbTypes.Add(fldDbType);
                        }
                    }
                }
            }

            if (columnNames.Count == 0)
            {
                return string.Empty;
            }

            return $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) " +
                   $"VALUES ({string.Join(", ", parameters)})";
        }

        /// <summary>
        /// Builds UPDATE SQL with operation-specific logic
        /// </summary>
        private string BuildUpdateSql(
        TMdMap mdMap,
            string tableName,
            string whereClause,
            List<string> columnNames,
            List<string> parameters,
            List<object> values,
            List<DbType> fldDbTypes, 
            DataTable mappedData)
        {
            var setClauses = new List<string>();

            for (int i = 0; i < mdMap.FieldMappings.Count; i++)
            {
                var setClause = BuildSetClause(mdMap.FieldMappings[i], mappedData);

                if (mdMap.FieldMappings[i].Operation != OperationType.Inc &&
                    mdMap.FieldMappings[i].Operation != OperationType.Dec)
                {
                    var (columnClause, parameterClause, value, fldDbType) = FormatParams(mdMap.FieldMappings[i]);
                    columnNames.Add(columnClause);
                    parameters.Add(parameterClause);
                    values.Add(value);
                    fldDbTypes.Add(fldDbType);
                }

                if (!string.IsNullOrEmpty(setClause))
                {
                    setClauses.Add(setClause);

                }
            }

            if (setClauses.Count == 0)
            {
                return string.Empty;
            }

            return $"UPDATE {tableName} SET {string.Join(", ", setClauses)} {whereClause}";
        }

        /// <summary>
        /// Sets the source field value in each FieldMapping
        /// </summary>
        private void SetSourceValues(TMdMap mdMap)
        {
            foreach (var field in mdMap.FieldMappings)
            {
                field.SourceValue = GetFieldValue(field.SourceField, 1);
            }
        }

        /// <summary>
        /// Builds operation-specific SET clause
        /// </summary>
        private string BuildSetClause(MdMapField field, DataTable mappedData)
        {
            string paramName = $"@{field.SourceField}";

            return field.Operation switch
            {
                OperationType.Normal or OperationType.Replace =>
                    $"{field.TargetField} = {paramName}",

                OperationType.Add =>
                    $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} + {paramName}",

                OperationType.Less =>
                    $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} - {paramName}",

                OperationType.Highest =>
                    BuildHighestClause(field.TargetField, paramName),

                OperationType.Lowest =>
                    BuildLowestClause(field.TargetField, paramName),

                OperationType.Inc =>
                    $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} + 1",

                OperationType.Dec =>
                    $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} - 1",

                _ => $"{field.TargetField} = {paramName}"
            };
        }

        /// <summary>
        /// Builds highest value clause
        /// </summary>
        private string BuildHighestClause(string field, string paramName)
        {
            switch (_common._dbType)
            {
                case "sqlserver":
                    return $"{field} = CASE WHEN {paramName} > {field} THEN {paramName} ELSE {field} END";

                case "oracle":
                    // Oracle has GREATEST function but handles NULLs differently
                    return $"{field} = COALESCE(GREATEST({field}, {paramName}), {field}, {paramName})";

                default:
                    // MySQL, PostgreSQL, SQLite, etc.
                    return $"{field} = GREATEST({field}, {paramName})";
            }
        }

        /// <summary>
        /// Builds lowest value clause
        /// </summary>
        private string BuildLowestClause(string field, string paramName)
        {
            switch (_common._dbType)
            {
                case "sqlserver":
                    // Handle empty strings and NULLs properly
                    return $"{field} = CASE WHEN {field} IS NULL THEN {paramName} " +
                           $"WHEN {field} = '' THEN CASE WHEN {paramName} IS NULL THEN '' ELSE {paramName} END " +
                           $"WHEN {paramName} < {field} THEN {paramName} ELSE {field} END";

                case "oracle":
                    // Oracle's LEAST function returns NULL if any argument is NULL
                    return $"{field} = COALESCE(LEAST({field}, {paramName}), {field}, {paramName})";

                default:
                    // MySQL, PostgreSQL, SQLite, etc.
                    return $"{field} = LEAST({field}, {paramName})";
            }
        }

        /// <summary>
        /// Creates parameter for a field
        /// </summary>
        private (string columnName, string paramValue, object value, DbType paramDbType) 
            FormatParams(MdMapField mdField)
        {
            if (string.IsNullOrEmpty(mdField.SourceField))
                return ("", "", null, DbType.String);

            DbType fldDbType = GetFieldDBType(mdField.SourceField);
            string dataType = GetFieldDataType(mdField.SourceField);
            object formattedValue = FormatValueForSql(
                mdField.SourceValue,
                dataType,
                mdField.DecimalPlaces
            );

            return (mdField.SourceField, $"@{mdField.SourceField}", formattedValue ?? DBNull.Value, fldDbType);
        }

        /// <summary>
        /// Formats value for parameter using field type
        /// </summary>
        private object FormatValueForSql(string value, string dataType, int decimalPlaces) //, bool isInsert)
        {
            if (string.IsNullOrEmpty(value))
                return DBNull.Value;
            //return isInsert && dataType?.ToUpper() == "N" ? 0 : DBNull.Value;

            try
            {
                switch (dataType?.ToUpper())
                {
                    case "D": // Date
                        string inputValue = value?.Trim();

                        string[] dateTimeFormats =
                        {
                            $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
                            _common.ApiDateFormat
                        };

                        if (DateTime.TryParseExact(
                                inputValue,
                                dateTimeFormats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out DateTime dateResult))
                        {
                            return dateResult;
                        }
                        //if (DateTime.TryParse(value, out DateTime dateValue))
                        //    return dateValue;
                        return DBNull.Value;
                    //return isInsert ? DateTime.Now : DBNull.Value;

                    case "N": // Numeric
                        if (decimalPlaces > 0)
                        {
                            if (decimal.TryParse(value, out decimal decValue))
                                return Math.Round(decValue, decimalPlaces);
                        }
                        else
                        {
                            if (long.TryParse(value, out long longValue))
                                return longValue;
                        }
                        return 0;

                    default: // String and other types
                        return value;
                }
            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogError($"Error formatting value: {ex.Message}");
                return DBNull.Value;
                //return isInsert && dataType?.ToUpper() == "N" ? 0 : DBNull.Value;
            }
        }

        /// <summary>
        /// Adds default columns for INSERT
        /// </summary>
        private void AddDefaultInsertColumns(ref List<string> columns, ref List<string> parameters)
        {
            // Similar to your pattern of adding createdby, createdon, etc.
            columns.AddRange(new[] { "createdby", "createdon", "username", "app_desc", "app_level" });
            parameters.AddRange(new[] { "@createdby", "@createdon", "@username", "@app_desc", "@app_level" });
        }

        private void InitializeFldLookup()
        {
            if (_fieldLookup == null || !_lookupInitialized)
            {
                _fieldLookup = Flds?.ToDictionary(
                    f => f.FieldName,
                    StringComparer.OrdinalIgnoreCase
                ) ?? new Dictionary<string, TField>();
                _lookupInitialized = true;
            }
        }

        public static string GetFieldValueFromDT(DataTable table, string columnName)
        {
            if (table == null || table.Rows.Count == 0)
                return null;

            // Find column with case-insensitive match
            var column = table.Columns.Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (column == null)
                return null;

            return table.Rows[0][column.ColumnName]?.ToString() ?? "";
        }

        public async Task<SQLResult> GetRecordIdsFromDB(Int32 rowsCount)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            int siteNo = 0;
            try
            {
                siteNo = Convert.ToInt32(_common._sessionVars[ARMCommon.Helpers.Constants.SESSION_DATA.SITENO.ToString()].ToString());
                if (siteNo < 0)
                    siteNo = 1;
            }
            catch
            {
            }

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            string sql = string.Empty;

            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.AXPUT_GETRECORDIDS_MSSQL;
                string[] paramName = { "@psiteno", "@pnoofrows"};
                DbType[] paramType = { DbType.Int32, DbType.Int32 };
                object[] paramValue = { siteNo, rowsCount };

                var dsResult = await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.AXPUT_GETRECORDIDS.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.AXPUT_GETRECORDIDS_ORACLE.ToString();

                string[] paramName = { "@siteno", "@rowscount" };
                DbType[] paramType = { DbType.Int32, DbType.Int32 };
                object[] paramValue = { siteNo, rowsCount };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }



        }

        public async Task GetTstructSpecificDevOptions(string transId)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXGET_TSTRUCTDEVOPTIONS.ToString();
            string[] paramName = { "@transid"};
            DbType[] paramType = { DbType.String};
            object[] paramValue = { transId };

            var dbResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            if (dbResult != null && dbResult.data != null && dbResult.data.Rows.Count > 0)
            {
                var transIdSpecificConfig = dbResult.data.AsEnumerable().Where(x => x.Field<string>("structname").ToLower() == transId).Select(x => x).ToList();
                if(transIdSpecificConfig.Count > 0)
                {
                    var saveImageInDBflag = transIdSpecificConfig.AsEnumerable().Where(x => x.Field<string>("props").ToLower() == "saveimage").Select(r => r.Field<string>("propsval")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(saveImageInDBflag))
                        SaveImageInDB = Convert.ToBoolean(saveImageInDBflag);
                    else
                        SaveImageInDB = false;

                    var fileUploadLimit = transIdSpecificConfig.AsEnumerable().Where(x => x.Field<string>("props").ToLower() == "file upload limit").Select(r => r.Field<string>("propsval")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(fileUploadLimit))
                        FileUploadLimit = fileUploadLimit.ToString();
                    
                }

                var allFormsConfig = dbResult.data.AsEnumerable().Where(x => x.Field<string>("structname").ToLower() == "ALL Forms").Select(x => x).ToList();
                if (allFormsConfig.Count > 0)
                {
                    var saveImageInDBflag = allFormsConfig.AsEnumerable().Where(x => x.Field<string>("props").ToLower() == "saveimage").Select(r => r.Field<string>("propsval")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(saveImageInDBflag))
                        SaveImageInDB = Convert.ToBoolean(saveImageInDBflag);
                    else
                        SaveImageInDB = false;

                    var fileUploadLimit = allFormsConfig.AsEnumerable().Where(x => x.Field<string>("props").ToLower() == "file upload limit").Select(r => r.Field<string>("propsval")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(fileUploadLimit))
                        FileUploadLimit = fileUploadLimit.ToString();
                }
            }
        }

        private object ConvertValueByDbType(object value, DbType dbType)
        {
            if (value == null || value == DBNull.Value)
                return value;

            switch (dbType)
            {
                case DbType.Int16:
                    return Convert.ToInt16(value);

                case DbType.Int32:
                    return Convert.ToInt32(value);

                case DbType.Int64:
                    return Convert.ToInt64(value);

                case DbType.Single: //Handle: 7 digits
                    return Convert.ToSingle(value, CultureInfo.InvariantCulture);

                case DbType.Double: //Handle: 15-16 digits
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);

                case DbType.Decimal: //Handle: 28-29 digits
                    return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

                case DbType.Date:
                    //return DateOnly.ParseExact(value.ToString(),_common.ApiDateFormat,CultureInfo.InvariantCulture);
                    //return DateTime.ParseExact(value.ToString(), new[] { "dd-MM-yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
                    DateTime dateValue;

                    if (value is DateTime dt)
                    {
                        dateValue = dt;
                    }
                    else
                    {
                        dateValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    }

                    return dateValue.ToString(_common.ApiDateFormat, CultureInfo.InvariantCulture).Split(" ")[0];

                case DbType.DateTime:
                    //return DateTime.Parse(value.ToString());
                    //return DateTime.ParseExact(value.ToString(), $"{_common.ApiDateFormat} {_common.ApiTimeFormat}", CultureInfo.InvariantCulture);
                    //return DateTime.ParseExact(value.ToString(), new[] { $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
                    //    $"dd-MM-yyyy {_common.ApiTimeFormat}", $"dd/MM/yyyy {_common.ApiTimeFormat}" },
                    //    CultureInfo.InvariantCulture, DateTimeStyles.None);
                    DateTime dateTimeValue;

                    if (value is DateTime dtt)
                    {
                        dateTimeValue = dtt;
                    }
                    else
                    {
                        dateTimeValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    }

                    return dateTimeValue.ToString(
                        $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
                        CultureInfo.InvariantCulture);
                default:
                    return value;
            }
        }

        //private object ConvertValueByDbType(object value, DbType dbType)
        //{
        //    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        //    Directory.CreateDirectory(_exeDir);
        //    string _logFileName = "StructDef-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        //    _exeDir = Path.Combine(_exeDir, _logFileName);

        //    File.AppendAllText(_exeDir, $"MD:Value: {value}, MD:DbType: {dbType}\n\n");

        //    if (value == null || value == DBNull.Value)
        //        return value;

        //    switch (dbType)
        //    {
        //        case DbType.Int16:
        //            return Convert.ToInt16(value);

        //        case DbType.Int32:
        //            return Convert.ToInt32(value);

        //        case DbType.Int64:
        //            return Convert.ToInt64(value);

        //        case DbType.Single: //Handle: 7 digits
        //            return Convert.ToSingle(value, CultureInfo.InvariantCulture);

        //        case DbType.Double: //Handle: 15-16 digits
        //            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        //        case DbType.Decimal: //Handle: 28-29 digits
        //            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        //        case DbType.Date:
        //            //return DateOnly.ParseExact(value.ToString(),_common.ApiDateFormat,CultureInfo.InvariantCulture);
        //            //return DateTime.ParseExact(value.ToString(), new[] { "dd-MM-yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
        //            var orgDateValue = value;
        //            try
        //            {
        //                DateTime dateValue;

        //                if (value is DateTime dt)
        //                {
        //                    dateValue = dt;
        //                }
        //                else
        //                {
        //                    dateValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        //                }

        //                File.AppendAllText(_exeDir, $"DateValue: {dateValue}, Value: {value}\n\n");

        //                var finalValue = dateValue.ToString(_common.ApiDateFormat, CultureInfo.InvariantCulture).Split(" ")[0];

        //                File.AppendAllText(_exeDir, $"FinalDateValue: {finalValue}\n\n");

        //                return finalValue;

        //            }
        //            catch (Exception e)
        //            {

        //                File.AppendAllText(_exeDir, $"{e}\n\n" + $"value: {value}, orgDateValue: {orgDateValue}\n\n");

        //            }
        //            return value;


        //        case DbType.DateTime:
        //            //return DateTime.Parse(value.ToString());
        //            //return DateTime.ParseExact(value.ToString(), $"{_common.ApiDateFormat} {_common.ApiTimeFormat}", CultureInfo.InvariantCulture);
        //            //return DateTime.ParseExact(value.ToString(), new[] { $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
        //            //    $"dd-MM-yyyy {_common.ApiTimeFormat}", $"dd/MM/yyyy {_common.ApiTimeFormat}" },
        //            //    CultureInfo.InvariantCulture, DateTimeStyles.None);

        //            var orgDateTimeValue = value;
        //            try
        //            {
        //                DateTime dateTimeValue;

        //                if (value is DateTime dtt)
        //                {
        //                    dateTimeValue = dtt;
        //                }
        //                else
        //                {
        //                    dateTimeValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        //                }

        //                File.AppendAllText(_exeDir, $"DateTimeValue: {dateTimeValue}, Value: {value}\n\n");


        //                var finalDateTimeVlaue = dateTimeValue.ToString(
        //                    $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
        //                    CultureInfo.InvariantCulture);

        //                File.AppendAllText(_exeDir, $"FinalDateTimeValue: {finalDateTimeVlaue}\n\n");

        //                return finalDateTimeVlaue;
        //            }
        //            catch (Exception e)
        //            {

        //                File.AppendAllText(_exeDir, $"{e}\n\n" + $"value: {value}, orgDateTimeValue: {orgDateTimeValue}\n\n");

        //            }
        //            return value;
        //        default:
        //            return value;
        //    }
        //}


        #endregion

    }
}

