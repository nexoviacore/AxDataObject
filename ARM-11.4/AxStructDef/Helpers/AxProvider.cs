using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Compression;
using System.Text;
using System.Xml;
//using AxStructDef.Interface;
//using AxStructDef.Helpers; 
//using AxStructDef.Stubs;
using Npgsql;
//using AxStructDef.Model;
using ARMCommon.Interface;
using ARMCommon.Helpers;
using ARMCommon.Model;



namespace AxStructDef.Helpers;

public class AxProvider
{
    // ----------------------- Private fields -----------------------
    //private Xds x, q, forcds, xTransCheck;

    private readonly string _connectionString;
    private readonly string _dbType;
    private readonly IDbHelper _dbHelper;
    private XmlNode xnode;
    private string CreatedOn, CreatedBy, ImportedOn, ImportedBy, prev_pwds;

    // ----------------------- Public fields ------------------------
    // public DBManager dbm;
    // public string  GetStructCaption;
    public string GetPageName, GetPageCaption, GetPageImg, GetPageVisible, savedimages;
    public int GetPageOrdNo;
    public bool bImportPrintDoc;
    public string ErrorStr;
    public LicMgr lm;
    public bool ForAxScript, bForRapidPost;
    public GeneralFunctions gf;
    public DBHelper dbm { get; set; }

    // ------------------- Constructor / Destructor -----------------
    public AxProvider(IDbHelper dbHelper, string connectionString, string dbType)
    {
        _dbHelper = dbHelper;
        _connectionString = connectionString;
        _dbType = dbType;
        //_logger = logger;

        dbm = new DBHelper();

        // Initialize gf (GlobalFunctions) as well
        gf = new GeneralFunctions
        {
            IsService = false,
            IsRapidPost = false,
            username = "username",
            usergroup = "usergroup",
            startpath = "",
            axdecimal = -1,
            AxpLocalDB = false
        };
    }

    ~AxProvider()
    {
        // Cleanup if needed
    }

           public string GetSqlLower()
        {
            var dbVars = DBHelper.GetDBVars(_dbType);
            return dbVars.ContainsKey("sqllower") ? dbVars["sqllower"] : "LOWER";
        }

           public async Task<SQLResult> ExecuteSQLAsync(string sql, string[] paramNames = null, DbType[] paramTypes = null, object[] paramValues = null)
        {
            var sqlResult = await _dbHelper.ExecuteSQLAsync(sql, _connectionString,
                    paramNames, paramTypes, paramValues);

            return sqlResult;
        }

    public async Task<NonQueryResult> ExecuteNonSQLAsync(string sql, string[] paramNames = null, DbType[] paramTypes = null, object[] paramValues = null)
    {
        NonQueryResult nonQueryResult = await _dbHelper.ExecuteNonQueryAsync(sql, _connectionString, paramNames, paramTypes, paramValues);

        return nonQueryResult;
    }

   

    #region Important functions 
    public async Task<XmlDocument> GetStructure(string sname, string name, string caption, string purpose)
    {
        XmlDocument result = new XmlDocument();
        result.LoadXml("<root></root>");

        string whereClause;
        string paramValue;
        string paramType = "c"; // character type

        // Build WHERE clause and parameter value
        if (string.IsNullOrEmpty(name))
        {
            whereClause = "caption = :name";
            paramValue = caption;
        }
        else
        {
            var dbVars = DBHelper.GetDBVars(_dbType);
            string sqlLower = dbVars.ContainsKey("sqllower") ? dbVars["sqllower"] : "LOWER";
            whereClause = $"{sqlLower}(name) = :name";
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

    private async Task ReadMemo(string fieldName, string table, string whereClause,
                      string paramValue, string paramType, Stream outputStream)
    {
        try
        {
            // Determine field name based on database type
            string dbFieldName = fieldName;
            var dbVars = DBHelper.GetDBVars(_dbType);

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

            dbFieldName = fieldName;

            //string sql = "SELECT * FROM tms.tstructs WHERE name = 'empyf'";
            string sql = $"SELECT {dbFieldName} FROM {table} WHERE {whereClause} ORDER BY blobno";

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string is not initialized");
            }

            // Execute query with parameter
            var sqlResult = await _dbHelper.ExecuteSQLAsync(sql, _connectionString,
                new[] { "name" },
                new[] { DbType.String },
                new[] { (object)paramValue });

            // Check for errors first
            if (!string.IsNullOrEmpty(sqlResult.error))
            {
                Console.WriteLine($"SQL error: {sqlResult.error}");
                return;
            }

            if (sqlResult.data == null)
            {
                Console.WriteLine("DataTable is null in SQL result");
                return;
            }

            if (sqlResult.data.Rows.Count == 0)
            {
                Console.WriteLine($"No rows found for name: {paramValue}");
                return;
            }

            Console.WriteLine($"Found {sqlResult.data.Rows.Count} rows for name: {paramValue}");

            // Process each row
            foreach (DataRow row in sqlResult.data.Rows)
            {
                // Check if the field exists in the row
                if (!sqlResult.data.Columns.Contains(fieldName))
                {
                    Console.WriteLine($"Field '{fieldName}' does not exist in the result");
                    continue;
                }

                // Check if the field value is null or DBNull
                if (row[fieldName] == null || row[fieldName] == DBNull.Value)
                {
                    Console.WriteLine($"Field '{fieldName}' is null or DBNull in row");
                    continue;
                }

                // Handle the data based on its actual type
                object fieldValue = row[fieldName];

                if (fieldValue is byte[] blobData)
                {
                    // This is the most common case - byte array from database
                    Console.WriteLine($"Found byte array of length: {blobData.Length}");
                    if (blobData.Length > 0)
                    {
                        await outputStream.WriteAsync(blobData, 0, blobData.Length);
                        Console.WriteLine($"Successfully wrote {blobData.Length} bytes to stream");
                    }
                }
                else if (fieldValue is string stringData)
                {
                    // Handle string data (might be encoded bytea from PostgreSQL)
                    Console.WriteLine($"Found string data: {stringData.Substring(0, Math.Min(50, stringData.Length))}...");

                    if (_dbType.ToLower() == "postgre" || _dbType.ToLower() == "postgres")
                    {
                        // PostgreSQL returns bytea as hex string or escaped text
                        byte[] blobDataFromString = ParsePostgreSQLBytea(stringData);
                        if (blobDataFromString != null && blobDataFromString.Length > 0)
                        {
                            await outputStream.WriteAsync(blobDataFromString, 0, blobDataFromString.Length);
                            Console.WriteLine($"Successfully wrote {blobDataFromString.Length} bytes from string");
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
                    Console.WriteLine($"Unexpected field type: {fieldValue.GetType().Name}");
                }
            }
        }
        catch (NpgsqlException npgEx)
        {
            Console.WriteLine($"Postgres Exception: {npgEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading memo: {ex.Message}");
            throw;
        }
    }

    // Helper method to parse PostgreSQL bytea data
    private byte[] ParsePostgreSQLBytea(string byteaData)
    {
        try
        {
            Console.WriteLine($"Parsing bytea data: {byteaData?.Substring(0, Math.Min(50, byteaData?.Length ?? 0))}...");

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
            Console.WriteLine("Error parsing PostgreSQL bytea");
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
            Console.WriteLine("Error converting hex string to byte array");
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
                throw new InvalidDataException("Invalid compressed stream format: missing size header");
            }

            // Convert bytes to size (little-endian)
            uint uncompressedSize = BitConverter.ToUInt32(sizeBytes, 0);

            // Use System.IO.Compression for ZLib decompression
            using (var decompressionStream = new System.IO.Compression.ZLibStream(compressedStream, System.IO.Compression.CompressionMode.Decompress, true))
            {
                var decompressedStream = new MemoryStream();
                await decompressionStream.CopyToAsync(decompressedStream);
                decompressedStream.Position = 0;

                // Verify we got the expected amount of data
                if (decompressedStream.Length != uncompressedSize)
                {
                    Console.WriteLine($"Warning: Expected {uncompressedSize} bytes, got {decompressedStream.Length} bytes");
                }

                return decompressedStream;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decompression error: {ex.Message}");

            // Fallback: try GZip compression (some older formats might use this)
            try
            {
                compressedStream.Position = 0;
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, true))
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

    public string GetDBType()
    {
        return _dbType ?? string.Empty;
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

    //private XmlDocument CheckProduct(string name, XmlDocument xmlDoc)
    //{
    //    // Placeholder for product checking logic
    //    // This would typically validate licenses or apply product-specific transformations
    //    return xmlDoc;
    //}

    public string GetStructName()
    {
        // Placeholder - implement based on your application logic
        return string.Empty;
    }

    public string GetStructCaption()
    {
        // Placeholder - implement based on your application logic
        return string.Empty;
    }

    //private async Task GetPrintTemplate(string sname, string name, string filename)
    //{
    //    // Placeholder for print template retrieval logic
    //    await Task.CompletedTask;
    //}

    // Helper method to load XML from string (equivalent to LoadXMLDataFromWS)

    private XmlDocument LoadXMLDataFromWS(string xmlContent)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlContent);
        return doc;
    }
        
        #endregion


}
