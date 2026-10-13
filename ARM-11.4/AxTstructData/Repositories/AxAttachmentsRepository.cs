using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace AxTstructData
{
    public class AxAttachmentsRepository : IAxAttachmentsRepository
    {
        private readonly Utils _common;

        public AxAttachmentsRepository(Utils common)
        {
            _common = common;
        }

        public async Task<SQLResult> GetImageFromDB(AxAttachments attachment) 
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_GETIMAGE.ToString();
            sql = sql.Replace("$IMAGETABLENAME$", $"{ attachment.TransId + attachment.FieldName}");
            string[] paramName = { "@recordid" };
            DbType[] paramType = { DbType.Int64 };
            object[] paramValue = { attachment.RecordId };
            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }

        public async Task<SQLResult> DeleteImagesFromDB(AxAttachments attachment)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_DELETEIMAGE.ToString();
            sql = sql.Replace("$IMAGETABLENAME$", $"{attachment.TransId + attachment.FieldName}");

            string[] paramName = { "@recordid" };
            DbType[] paramType = { DbType.Int64 };
            object[] paramValue = { attachment.RecordId };

            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }

        public async Task<SQLResult> WriteImagesToDB(AxAttachments attachment, byte[] chunk, int blobNo, string fileType)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_INSERTIMAGE.ToString();
            sql = sql.Replace("$IMAGETABLENAME$", $"{attachment.TransId + attachment.FieldName}");

            string[] paramName =
            {
                "@recordid",
                "@blobno",
                "@img",
                "@ftype"
            };

            DbType[] paramType =
            {
                DbType.Int64,
                DbType.Int32,
                DbType.Binary,
                DbType.String
            };

            object[] paramValue =
            {
                attachment.RecordId,
                blobNo,
                chunk,
                fileType
            };

            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }

        public async Task<SQLResult> GetHeaderAttachmentsFromDB(AxAttachments attachment)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_GETHEADERATTACHMENT.ToString();
            sql = sql.Replace("$ATTACHTABLENAME$", $"{attachment.TransId + "attach"}");
            string[] paramName = { "@recordid" };
            DbType[] paramType = { DbType.Int64 };
            object[] paramValue = { attachment.RecordId };
            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }

        public async Task<SQLResult> DeleteHeaderAttachmentsFromDB(AxAttachments attachment)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_DELETEHEADERATTACHMENT.ToString();
            sql = sql.Replace("$ATTACHTABLENAME$", $"{attachment.TransId + "attach"}");

            string[] paramName = { "@recid" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { attachment.RecordId + "$" + attachment.FileName };

            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }

        public async Task<SQLResult> InsertHeaderAttachmentsToDB(AxAttachments attachment, byte[] chunk, int blobNo, string fileType)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.AXATTACH_INSERTHEADERATTACHMENT.ToString();
            sql = sql.Replace("$ATTACHTABLENAME$", $"{attachment.TransId + "attach"}");

            string[] paramName =
            {
                "@recordid",
                "@recid",
                "@filename",
                "@blobno",
                "@template",
            };

            DbType[] paramType =
            {
                DbType.Int64,
                DbType.String,
                DbType.String,
                DbType.Int32,
                DbType.Binary
            };

            object[] paramValue =
            {
                attachment.RecordId,
                attachment.RecordId + "$" + attachment.FileName,
                attachment.FileName,
                blobNo,
                chunk
            };

            var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }
    }
}
