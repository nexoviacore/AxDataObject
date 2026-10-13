using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Extensions.Logging; 
using AxStructDef.Helpers;
using AxStructDef.Models;


namespace AxStructDef.Stubs;

public class Xds : System.ComponentModel.Component
{
    private readonly ILogger<Xds> _logger;
    private bool _fcds;
    private dynamic _xfield;
    private dynamic _xparam;
    private dynamic _xfieldByName;
    private dynamic _xparamByName;
    private dynamic _xsetsql;
    private dynamic _xgetsql;
    private dynamic _xsetsqltext;
    private dynamic _xgetsqltext;
    private dynamic _xopen;
    private dynamic _xclose;
    private dynamic _xexecsql;
    private dynamic _xeof;
    private dynamic _xisempty;
    private dynamic _xfirst;
    private dynamic _xnext;
    private dynamic _xfindfield;
    private dynamic _xinsert;
    private dynamic _xupdate;
    private dynamic _xfindrecord;
    private dynamic _xdelete;
    private dynamic _xgetactive;
    private dynamic _xsetactive;
    private dynamic _xassignparam;
    private dynamic _xstoredprocexec;
    private dynamic _xquote;
    private dynamic _xreadmemo;
    private dynamic _xwritememo;
    private dynamic _xreadblob;
    private dynamic _xwriteblob;
    private dynamic _xreadstream;
    private dynamic _xwritestream;

    private string _insFields;
    private string _fTypes;
    private string _insValues;
    private string _updFields;
    private string _updateTable;
    private string _prmFields;
    private string _prmValues;
    private string _pTypes;
    private string _updateCondition;
    private string _commands;
    private List<string> _flds;
    private List<string> _vals;
    private dynamic _pdoc;
    private int _prmNo;
    private GeneralFunctions _gf;
    private string _axpDateParams;
    private string _ldsRequestXML;
    private bool _bIsLOBField;
    private int _getDataRetries;
    private dynamic _transaction;

    public dynamic Connection { get; set; }
    public string name { get; set; }
    public dynamic Provider { get; set; }
    public dynamic Dbx { get; set; }
    public dynamic SpDbx { get; set; }
    public dynamic Ado { get; set; }
    public dynamic SpAdo { get; set; }
    public dynamic CDS { get; set; }
    public int Tag { get; set; }
    public bool Editing { get; set; }
    public bool Inserting { get; set; }
    public event EventHandler BeforePost;
    public List<string> ParamNames { get; set; }
    public dynamic PNode { get; set; }
    public dynamic UpdateChanges { get; set; }
    public string CDSName { get; set; }

    public Xds(string sName, System.ComponentModel.IComponent owner, dynamic connection, GeneralFunctions gfun, ILogger<Xds> logger)
    {
        CDSName = sName;
        Connection = connection;
        _gf = gfun;
        _flds = new List<string>();
        _vals = new List<string>();
        ParamNames = new List<string>();
        _logger = logger;
        // CreateComponents();
        
    }

      private void FreeAndNull<T>(ref T obj) where T : class
    {
        if (obj != null)
        {
            // Assuming objects implement IDisposable or have a similar cleanup mechanism
            if (obj is IDisposable disposable)
                disposable.Dispose();
            obj = null;
        }
    }

     private void XClose()
    {
        // Implement or replace with actual logic to close resources
    }


    public new void  Dispose()
    {
        try
        {
            if (!(_gf?.RemoteLogin ?? false))
            {
                XClose();
            }
            FreeAndNull(ref _xfield);
            FreeAndNull(ref _xparam);
            FreeAndNull(ref _flds);
            FreeAndNull(ref _vals);
            ParamNames = null; 
        
        }
        catch (Exception e)
        {
            //($"fields,params etc free error : {e.Message}");
            Provider = null;
        }

        try
        {
            if (Provider != null)
            {
                Provider.DataSet = null;
                Provider = null; 
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"provider free error : {e.Message}");
            Provider = null;
        }

        try
        {
            if (CDS != null)
            {
                if (CDS.Active)
                    CDS.Active = false;
                CDS.ProviderName = string.Empty;
                CDS.Destroy();
                CDS = null;
            }
        }
        catch (Exception e)
        {
            //_gf?.DoDebug?.Msg($"cds free error : {e.Message}");
            CDS = null;
        }

        if (Connection?.Driver == "dbx")
        {
            try
            {
                if (Dbx != null)
                {
                    if (Dbx.Active)
                        Dbx.Active = false;
                    Dbx.SQL.Clear();
                    Dbx = null; 
                }
                if (SpDbx != null)
                    SpDbx = null;
            }
            catch (Exception e)
            {
                _logger.LogError($"dbx free error : {e.Message}");
                Dbx = null;
            }
        }
        else if (Connection?.Driver == "ado")
        {
            if (Ado != null)
            {
                _logger.LogInformation("Adoquery disconnect...");
                Ado.Active = false;
                Ado.Close();
                _logger.LogInformation("Adoquery sqls clearing...");
                Ado.SQL.Clear();
                _logger.LogInformation("Adoquery destroying...");
                
                                _logger.LogInformation("adoquery destroyed...");
            }
            if (SpAdo != null)
                SpAdo = null; 
        }
    }


    private void CreateComponents()
    {
        throw new NotImplementedException();
    }

    private void SetCDS(bool flag)
    {
        _fcds = flag;
    }

    private List<string> GetSQL()
    {
        throw new NotImplementedException();
    }

    private string GetSQLText()
    {
        throw new NotImplementedException();
    }

    private void SetSQL(List<string> s)
    {
        throw new NotImplementedException();
    }

    private void SetSQLText(string s)
    {
        throw new NotImplementedException();
    }

    private dynamic GetFieldList()
    {
        throw new NotImplementedException();
    }

    private void ASetValue(string name, string value) => throw new NotImplementedException();
    private void DSetValue(string name, string value) => throw new NotImplementedException();
    private void APSetValue(string name, string value) => throw new NotImplementedException();
    private void DPSetValue(string name, string value) => throw new NotImplementedException();
    private void ASetSql(List<string> s) => throw new NotImplementedException();
    private void DSetSql(List<string> s) => throw new NotImplementedException();
    private void DSetSqlText(string s) => throw new NotImplementedException();
    private List<string> AGetSql() => throw new NotImplementedException();
    private List<string> DGetSql() => throw new NotImplementedException();
    private string DGetSqlText() => throw new NotImplementedException();
    private dynamic AFieldByName(string s) => throw new NotImplementedException();
    private dynamic DFieldByName(string s) => throw new NotImplementedException();
    private void AOpen() => throw new NotImplementedException();
    private void DOpen() => throw new NotImplementedException();
    private void ROpen() => throw new NotImplementedException();
    private void COpen() => throw new NotImplementedException();
    private void AClose() => throw new NotImplementedException();
    private void DClose() => throw new NotImplementedException();
    private void CClose() => throw new NotImplementedException();
    private void RClose() => throw new NotImplementedException();
    private void AExecSql() => throw new NotImplementedException();
    private void DExecSql() => throw new NotImplementedException();
    private void CExecSql() => throw new NotImplementedException();
    private bool AEof() => throw new NotImplementedException();
    private bool DEof() => throw new NotImplementedException();
    private bool AIsEmpty() => throw new NotImplementedException();
    private bool DIsEmpty() => throw new NotImplementedException();
    private void AFirst() => throw new NotImplementedException();
    private void DFirst() => throw new NotImplementedException();
    private void ANext() => throw new NotImplementedException();
    private void DNext() => throw new NotImplementedException();
    private dynamic AParamByName(string s) => throw new NotImplementedException();
    private dynamic DParamByName(string s) => throw new NotImplementedException();
    private dynamic AFindField(string s) => throw new NotImplementedException();
    private dynamic DFindField(string s) => throw new NotImplementedException();
    private void AInsert(string table) => throw new NotImplementedException();
    private void DInsert(string table) => throw new NotImplementedException();
    private void AUpdate(string table, string where) => throw new NotImplementedException();
    private void DUpdate(string table, string where) => throw new NotImplementedException();
    private bool AFindRecord(string table, string where) => throw new NotImplementedException();
    private bool DFindRecord(string table, string where) => throw new NotImplementedException();
    private void ADelete(string table, string where) => throw new NotImplementedException();
    private void DDelete(string table, string where) => throw new NotImplementedException();
    private void RDelete(string table, string where) => throw new NotImplementedException();
    private void MakeStr() => throw new NotImplementedException();
    private void ASetActive(bool f) => throw new NotImplementedException();
    private void DSetActive(bool f) => throw new NotImplementedException();
    private bool AGetActive() => throw new NotImplementedException();
    private bool DGetActive() => throw new NotImplementedException();
    private bool GetActive() => throw new NotImplementedException();
    private void SetActive(bool f) => throw new NotImplementedException();
    private void AAssignParam(int paramNo, string paramValue, string paramType) => throw new NotImplementedException();
    private void DAssignParam(int paramNo, string paramValue, string paramType) => throw new NotImplementedException();
    private void RAssignParam(int paramNo, string paramValue, string paramType) => throw new NotImplementedException();
    private void CAssignParam(int paramNo, string paramValue, string paramType) => throw new NotImplementedException();
    private string DStoredProcExec(string spName, string inVars, string outVars) => throw new NotImplementedException();
    private string AStoredProcExec(string spName, string inVars, string outVars) => throw new NotImplementedException();
    private string AQuote(string tableName) => throw new NotImplementedException();
    private string OQuote(string tableName) => throw new NotImplementedException();
    private void AReadMemo(string fname, string fileName) => throw new NotImplementedException();
    private void AReadStream(string fname, string sname, StringReader stm) => throw new NotImplementedException();
    private void DReadMemo(string fname, string fileName) => throw new NotImplementedException();
    private void DReadStream(string fname, string sname, StringReader stm) => throw new NotImplementedException();
    private void DReadBlob(string fname, string fileName) => throw new NotImplementedException();
    private void AWriteMemo(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private void DWriteMemo(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private void DWriteBlob(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private string DownLoad(string fileName, string tempFile, string extn) => throw new NotImplementedException();
    private void ByteArrayToFile(byte[] byteArray, string fileName) => throw new NotImplementedException();
    private string ByteArrayToStream(byte[] byteArray) => throw new NotImplementedException();
    private void ASetSqlText(string s) => throw new NotImplementedException();
    private string AGetSqlText() => throw new NotImplementedException();
    private void AWriteBlob(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private void AReadBlob(string fname, string fileName) => throw new NotImplementedException();
    private void AWriteStream(string fname, string table, string where, StringReader stm, bool isBlob = false) => throw new NotImplementedException();
    private void BWriteStream(string fname, string table, string where, StringReader stm, bool isBlob = false) => throw new NotImplementedException();
    private void DWriteStream(string fname, string table, string where, StringReader stm, bool isBlob = false) => throw new NotImplementedException();
    private void CUpdate(string table, string where) => throw new NotImplementedException();
    private void CInsert(string table) => throw new NotImplementedException();
    private void RemoteMakeStr() => throw new NotImplementedException();
    private void MakeStrWithValue() => throw new NotImplementedException();
    private void Free() => throw new NotImplementedException();
    private void AssignCdsParamsToAdoParams() => throw new NotImplementedException();
    private void AdoCDSOpen() => throw new NotImplementedException();
    private void AssignCdsParamsToDbxParams() => throw new NotImplementedException();
    private void DbxCDSOpen() => throw new NotImplementedException();
    private string ChangeSqlForAssingParams(string sqlText, string p) => throw new NotImplementedException();
    private string ChangeSqlForAssingParams8909mode(string sqlText, string p) => throw new NotImplementedException();
    private string ChangeSqlForAssingParams8907mode(string sqlText, string p) => throw new NotImplementedException();
    private void CrossDateChecking() => throw new NotImplementedException();
    private void SetCommandTimeout() => throw new NotImplementedException();
    private byte[] StreamToByteArray(string fileName) => throw new NotImplementedException();
    private void PUpdate(string table, string where) => throw new NotImplementedException();
    private void PInsert(string table) => throw new NotImplementedException();
    private void KillInactiveSession() => throw new NotImplementedException();

    // CDS Methods
    private void RSetSqlText(string s) => throw new NotImplementedException();
    private string RGetSqlText() => throw new NotImplementedException();
    private dynamic RParamByName(string s) => throw new NotImplementedException();
    private void RPSetValue(string name, string value) => throw new NotImplementedException();
    private bool RIsEmpty() => throw new NotImplementedException();
    private dynamic RFieldByName(string s) => throw new NotImplementedException();
    private dynamic RFindField(string s) => throw new NotImplementedException();
    private bool REof() => throw new NotImplementedException();
    private void RFirst() => throw new NotImplementedException();
    private void RNext() => throw new NotImplementedException();
    private void RExec() => throw new NotImplementedException();
    private void RUpdate(string table, string where) => throw new NotImplementedException();
    private void RInsert(string table) => throw new NotImplementedException();
    private void RReadMemo(string fname, string fileName) => throw new NotImplementedException();
    private void RReadStream(string fname, string sname, StringReader stm) => throw new NotImplementedException();
    private void RWriteMemo(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private void RWriteStream(string fname, string table, string where, StringReader stm, bool isBlob = false) => throw new NotImplementedException();
    private void RReadBlob(string fname, string fileName) => throw new NotImplementedException();
    private void RWriteBlob(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    private bool RFindRecord(string table, string where) => throw new NotImplementedException();
    private bool RGetActive() => throw new NotImplementedException();
    private void RSetActive(bool f) => throw new NotImplementedException();
    private void GetDataFromServer() => throw new NotImplementedException();
    private void SaveQrytoPatch(string qryStr) => throw new NotImplementedException();
    private void AssignCdsParamsToParamsNode() => throw new NotImplementedException();
    private void AddAppNode(dynamic deNode) => throw new NotImplementedException();
    private void AddAxpropsNode(dynamic deNode) => throw new NotImplementedException();
    private StringReader ByteArrayToMemStream(byte[] byteArray) => throw new NotImplementedException();
    private byte[] CallGetCDS(dynamic asbService) => throw new NotImplementedException();
    private byte[] GetData(dynamic asbService, string requestStr) => throw new NotImplementedException();
    private void SetCDSOptionalParams(dynamic cds) => throw new NotImplementedException();
    private bool RetryGetDataFromServer(string eMsg) => throw new NotImplementedException();
    private bool CompareLDSRequest(string ldsFname) => throw new NotImplementedException();
    private void StoreCDSRequest(string fileName, string cdsRequest) => throw new NotImplementedException();
    private List<string> RGetSql() => throw new NotImplementedException();
    private void RSetSql(List<string> s) => throw new NotImplementedException();
    private string GetInsFields() => _insFields;
    private string GetInsValues() => _insValues;

    public dynamic FieldByName(string s) => throw new NotImplementedException();
    public dynamic ParamByName(string s) => throw new NotImplementedException();
    public void Open() => throw new NotImplementedException();
    public void Close() => throw new NotImplementedException();
    public void ExecSql() => throw new NotImplementedException();
    public bool Eof() => throw new NotImplementedException();
    public bool IsEmpty() => throw new NotImplementedException();
    public void First() => throw new NotImplementedException();
    public void Next() => throw new NotImplementedException();
    public dynamic FindField(string s) => throw new NotImplementedException();
    public void Insert(string table) => throw new NotImplementedException();
    public void Update(string table, string where) => throw new NotImplementedException();
    public void Submit(string fieldName, string fieldValue, string dataType) => throw new NotImplementedException();
    public void AddOrEdit(string table, string where) => throw new NotImplementedException();
    public bool FindRecord(string table, string where) => throw new NotImplementedException();
    public void ClearEdit() => throw new NotImplementedException();
    public void Append(string table) => throw new NotImplementedException();
    public void Edit(string table, string where) => throw new NotImplementedException();
    public void Post() => throw new NotImplementedException();
    public void DeleteRecord(string table, string where) => throw new NotImplementedException();
    public List<string> GetParamNames() => throw new NotImplementedException();
    public void AssignParam(int paramNo, string paramValue, string paramType) => throw new NotImplementedException();
    public string StoredProcExec(string spName, string inVars, string outVars) => throw new NotImplementedException();
    public string Quote(string tableName) => throw new NotImplementedException();
    public void ReadMemo(string fname, string fileName) => throw new NotImplementedException();
    public void ReadStream(string fname, string sname, StringReader stm) => throw new NotImplementedException();
    public void WriteMemo(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    public void ReadBlob(string fname, string fileName) => throw new NotImplementedException();
    public void WriteBlob(string fname, string table, string where, string fileName) => throw new NotImplementedException();
    public void WriteStream(string fname, string table, string where, StringReader stm, bool isBlob = false) => throw new NotImplementedException();
    public string GetFieldType(string fname) => throw new NotImplementedException();
    public void ClearParams() => throw new NotImplementedException();
    public string GetTable() => throw new NotImplementedException();
    public string GenerateInsertQuery(string tableName) => throw new NotImplementedException();
    public string GenerateInsertQueryCsv(string tableName) => throw new NotImplementedException();

    public void SetCDSName(string name)
    {
        CDSName = name;
    }

    public bool Buffered
    {
        get => _fcds;
        set => SetCDS(value);
    }

    public List<string> SQL
    {
        get => GetSQL();
        set => SetSQL(value);
    }

    public dynamic Fields => GetFieldList();

    public bool Active
    {
        get => GetActive();
        set => SetActive(value);
    }

    public string SqlText
    {
        get => GetSQLText();
        set => SetSQLText(value);
    }

    public string InsertedFields => GetInsFields();
    public string InsertedValues => GetInsValues();
}