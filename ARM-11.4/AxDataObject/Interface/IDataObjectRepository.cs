using ARMCommon.Model;
using AxDataObject.Models.Dto;
using AxDataObject.Models;
using AxStructDef;

namespace AxDataObject.Interface;

public interface IDataObjectRepository
{
    // DataObject Redis ops
    Task<DataObject> GetDataObjectAsync(string schemaName, string transId, string userName, string recordId);
    Task<bool> SaveDataObjectAsync(DataObject dataObject, string schemaName, string transId, string userName, string recordId);
    Task<bool> DeleteDataObjectAsync(string schemaName, string transId, string userName, string recordId);

    // StructDef (read-only, shared cache)
    Task<StructDef> GetStructDefAsync(string transId, bool refreshCache = false);

    // DB: permission checks
    Task<SQLResult> GetViewPermissionAsync(GetDataRequest req);
    Task<SQLResult> GetCreatePermissionAsync(SetDataRequest req);
    Task<SQLResult> GetEditPermissionAsync(SetDataRequest req);

    // DB: DC-wise SQL execution (AxGet)
    Task<SQLResult> GetDCWiseSQLsAsync(string transId, List<string> roles);
    Task<SQLResult> ExecuteDCWiseSQLAsync(long recordId, string sql);

    // DB: save (AxPut)
    Task<TransactionResult> ExecuteSaveCommandsAsync(List<SQLCommandBuilder> commands);

    // DB: child record lookup (GenMap)
    Task<SQLResult> GetChildTransRecordIdAsync(string parentRecId, string table, string mapName);
}
