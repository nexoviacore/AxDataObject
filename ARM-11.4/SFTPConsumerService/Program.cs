using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using Renci.SshNet;
using System.Data;
using System.Text;

class SFTPConsumerService
{
    static void Main(string[] args)
    {
        var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
        var json = System.IO.File.ReadAllText(appSettingsPath);
        dynamic config = JsonConvert.DeserializeObject(json);
        string queueName = config.AppConfig["QueueName"];
        var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
        serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
        var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();

        async Task<string> OnConsuming(string message)
        {
            try
            {
                JObject messageData = JObject.Parse(message);
                string queueData = (messageData["queuedata"]?.ToString() != "" ? messageData["queuedata"]?.ToString() : messageData["queuejson"]?.ToString());
                JObject receivedSFTPDetails = JsonConvert.DeserializeObject<JObject>(queueData);
                string updateVersion = receivedSFTPDetails["updateversion"]?.ToString() ?? "";
                string version = receivedSFTPDetails["version"]?.ToString() ?? "";
                string appName = receivedSFTPDetails["appname"].ToString() ?? "";
                var axpertSFTPVersionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "sftpversion.json");
                if (!File.Exists(axpertSFTPVersionPath))
                {
                    File.Create(axpertSFTPVersionPath).Close();
                }
                string jsonContent = await CustomFileReadAsync(axpertSFTPVersionPath);

                JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();
                if (!jobsversion.ContainsKey(appName))
                {
                    jobsversion.Add(appName, version);
                    await CustomFileWriteAsync(axpertSFTPVersionPath, jobsversion.ToString());
                    Console.WriteLine("entry to sftpversion.json is added");
                }
                else
                {
                    string jsversion = jobsversion[appName].ToString();
                    if (Int64.Parse(version) < Int64.Parse(jsversion))
                    {
                        jobsversion[appName] = version;
                        await CustomFileWriteAsync(axpertSFTPVersionPath, jobsversion.ToString());
                        Console.WriteLine("entry to sftpversion.json is updated");
                        Console.WriteLine($"Skippng this record as incoming record version is {jsversion} and latest running version is {version} ");
                        return "";
                    }
                }

                if (updateVersion == "true")
                {
                    return "";
                }

                string sourcetype = receivedSFTPDetails["sftp_applicable"].ToString() ?? "";
                long id = (long)receivedSFTPDetails["id"];
                string host = receivedSFTPDetails["hostname"].ToString() ?? "";
                int portNo = (int)receivedSFTPDetails["port_number"];
                string username = receivedSFTPDetails["user_name"]?.ToString() ?? "";
                string password = receivedSFTPDetails["password"].ToString();
                string localDirectory = receivedSFTPDetails["localfolder"]?.ToString() ?? "";
                string sourceDirectory = receivedSFTPDetails["sourcefolder"]?.ToString() ?? "";
                string fileType = receivedSFTPDetails["file_type"]?.ToString() ?? "";
                int backDatedFileDays = (string.IsNullOrEmpty(receivedSFTPDetails["backdated_filedays"]?.ToString()) ? (int)receivedSFTPDetails["backdated_filedays"] : 0);
                string fileSeperator = receivedSFTPDetails["file_separator"]?.ToString() ?? ",";
                string client_email_id = receivedSFTPDetails["client_email_id"]?.ToString() ?? "";
                string mail_content = receivedSFTPDetails["mail_content"]?.ToString() ?? ",";
                string trans_id = receivedSFTPDetails["trans_id"]?.ToString() ?? "axusr";
                string mail_subject = receivedSFTPDetails["mail_subject"]?.ToString() ?? "";
                string mailfrom = receivedSFTPDetails["mailfrom"]?.ToString() ?? "admin";
                string cc = receivedSFTPDetails["cc"]?.ToString() ?? "";
                string bcc = receivedSFTPDetails["bcc"]?.ToString() ?? "";
                localDirectory = Path.Combine(localDirectory + sourceDirectory);
                if (!Directory.Exists(localDirectory))
                    Directory.CreateDirectory(localDirectory);

                if (sourcetype.Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    using (var sftpClient = new SftpClient(host, portNo, username, password))
                    {
                        try
                        {
                            sftpClient.Connect();
                            if (sftpClient.IsConnected)
                            {
                                string sftpDirectory = sftpClient.WorkingDirectory + sourceDirectory;
                                var files = sftpClient.ListDirectory(sftpDirectory);

                                foreach (var file in files)
                                {
                                    string fileExtension = Path.GetExtension(file.Name).ToUpper();
                                    if (file.IsDirectory || file.Name.StartsWith(".") || file.Name == ".." || file.Name == ". " || file.Name.ToUpper().IndexOf($"_FAILURE{fileExtension}") > -1 || file.Name.ToUpper().IndexOf($"SUCCESS{fileExtension}") > -1 || file.Name.ToUpper().IndexOf($"FAILURE{fileExtension}") > -1)
                                    {
                                        continue;
                                    }

                                    if (fileExtension != "." + fileType.ToUpper())
                                        continue;

                                    int fileLastModifiedDays = (int)(DateTime.Now - file.LastWriteTime).TotalDays;
                                    if (backDatedFileDays != 0 && fileLastModifiedDays > backDatedFileDays)
                                        continue;

                                    string fileName = file.Name;
                                    string sourceFilePath = Path.Combine(sftpDirectory, fileName);
                                    string localFilePath = Path.Combine(localDirectory, fileName);
                                    string csvFileName = Path.GetFileNameWithoutExtension(fileName) + ".csv";

                                    using (var fileStream = File.Create(localFilePath))
                                    {
                                        sftpClient.DownloadFile(sourceFilePath, fileStream);
                                    }

                                    Console.WriteLine($"Downloaded: {fileName}");

                                    await ProcessFile(appName, id, fileName, localDirectory, sftpDirectory, sftpClient, fileSeperator, username, mail_content, client_email_id, trans_id, mail_subject, mailfrom, cc, bcc);
                                }

                                Console.WriteLine("All files downloaded successfully.");
                            }
                            else
                            {
                                Console.WriteLine("Failed to connect to the SFTP server.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        finally
                        {
                            sftpClient.Disconnect();
                        }
                    }

                }
                else
                {
                    // Get a list of all files in the source folder
                    string[] files = Directory.GetFiles(sourceDirectory);

                    // Copy each file to the destination folder
                    foreach (string file in files)
                    {
                        string fileExtension = Path.GetExtension(file).ToUpper();
                        if (fileExtension != "." + fileType.ToUpper() || file.ToUpper().IndexOf($"_FAILURE{fileExtension}") > -1)
                            continue;

                        int fileLastModifiedDays = (int)(DateTime.Now - File.GetLastWriteTime(file)).TotalDays;
                        if (backDatedFileDays != 0 && fileLastModifiedDays > backDatedFileDays)
                            continue;

                        string fileName = Path.GetFileName(file);
                        string localFilePath = Path.Combine(localDirectory, fileName);

                        File.Copy(file, localFilePath, true);

                        Console.WriteLine($"Downloaded: {fileName}");
                        await ProcessFile(appName, id, fileName, localDirectory, sourceDirectory, null, fileSeperator, username, mail_content, client_email_id, trans_id, mail_subject, mailfrom, cc, bcc);

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return "Error";
            }
            return "Success";
        }

        async Task<bool> ProcessFile(string appName, long id, string fileName, string localDirectory, string sourceDirectory, SftpClient sftpClient, string fileSeperator, string username, string mailcontent, string emailto, string trans_id, string mail_subject, string mailfrom, string cc, string bcc)
        {
            string localFilePath = Path.Combine(localDirectory, fileName.Replace("\\/", "\\").Replace("/", "\\"));
            string sourceFilePath = Path.Combine(sourceDirectory, fileName);
            string csvFileName = Path.GetFileNameWithoutExtension(fileName) + ".csv";
            string extension = Path.GetExtension(fileName).ToLower();
            bool conversionStatus = await ConvertDownloadedFiles(localDirectory, localFilePath, extension, fileSeperator);
            if (conversionStatus)
            {
                SQLResult dtImportResult = await ImportData(appName, id, localDirectory, fileName, csvFileName);
                string resultJson = JsonConvert.SerializeObject(dtImportResult.data);
                JArray jsonArray = JArray.Parse(resultJson);
                string resultValue = (string)jsonArray[0]["result"];
                JArray nestedArray = JArray.Parse(resultValue);
                string statusValue = (string)nestedArray[0]["status"];
                string failureFile = (string)nestedArray[0]["failure_filename"]?.ToString() ?? "";
                string failureMessage = (string)nestedArray[0]["failure_message"]?.ToString() ?? "";
                Console.WriteLine($"Conversion result: {resultJson}");
                if (statusValue == "failure")
                {
                    await ConvertFailureFiles(localDirectory, sourceDirectory, failureFile, extension, sftpClient, fileSeperator, appName, username, mailcontent, emailto, trans_id, mail_subject, mailfrom, cc, bcc);

                    if (sftpClient != null)
                    {
                        string convertedFilePath = Path.Combine(sourceDirectory, "Failure", fileName);
                        string targetFolder = Path.Combine(sourceDirectory, "Failure");
                        if (!sftpClient.Exists(targetFolder))
                        {
                            sftpClient.CreateDirectory(targetFolder);
                        }

                        sftpClient.RenameFile(sourceFilePath, Path.Combine(targetFolder, fileName));
                        Console.WriteLine($"Moved: {sourceFilePath} to {Path.Combine(targetFolder, fileName)}");
                    }

                }
                else
                {
                    if (sftpClient != null)
                    {
                        string targetFolder = Path.Combine(sourceDirectory, "Success");
                        if (!sftpClient.Exists(targetFolder))
                        {
                            sftpClient.CreateDirectory(targetFolder);
                        }

                        sftpClient.RenameFile(sourceFilePath, Path.Combine(targetFolder, fileName));
                        Console.WriteLine($"Moved: {sourceFilePath} to {Path.Combine(targetFolder, fileName)}");
                    }
                }
            }
            else
            {
                if (sftpClient != null)
                {
                    string convertedFilePath = Path.Combine(sourceDirectory, "Failure", fileName);
                    string targetFolder = Path.Combine(sourceDirectory, "Failure");
                    if (!sftpClient.Exists(targetFolder))
                    {
                        sftpClient.CreateDirectory(targetFolder);
                    }

                    sftpClient.RenameFile(sourceFilePath, Path.Combine(targetFolder, fileName));
                    Console.WriteLine($"Moved: {sourceFilePath} to {Path.Combine(targetFolder, fileName)}");

                    await SendMail(appName, username, mailcontent, emailto, trans_id, mail_subject, mailfrom, cc, bcc, localFilePath);
                }
            }

            return true;
        }

        async Task<bool> ConvertDownloadedFiles(string localDirectory, string localFilePath, string sourceExtension, string fileSeperator)
        {
            bool conversionStatus = false;
            switch (sourceExtension)
            {
                case ".txt":
                    conversionStatus = await ConvertTxtToCsv(localFilePath, localDirectory);
                    break;
                case ".xls":
                    conversionStatus = await ConvertXLSToCsv(localFilePath, localDirectory, fileSeperator);
                    break;
                case ".xlsx":
                    conversionStatus = await ConvertXLSXToCsv(localFilePath, localDirectory, fileSeperator);
                    break;
                case ".csv":
                    conversionStatus = true;
                    break;
                default:
                    Console.WriteLine($"Unsupported file format: {sourceExtension}");
                    break;
            }
            return conversionStatus;
        }

        async Task<bool> SendMail(string appName, string username, string mailcontent, string emailto, string trans_id, string mail_subject, string mailfrom, string cc, string bcc, string attachmentPath)
        {
            var jsonObject = new JObject();

            var mailJsonObj = new JObject();
            mailJsonObj.Add("axpapp", appName);
            mailJsonObj.Add("username", username);
            mailJsonObj.Add("scriptname", "axpeg_notification");
            mailJsonObj.Add("stype", "tstructs");
            mailJsonObj.Add("sname", trans_id);
            mailJsonObj.Add("recordid", "0");
            mailJsonObj.Add("trace", "false");
            mailJsonObj.Add("mailfrom", mailfrom);
            mailJsonObj.Add("mailto", emailto.Replace(";", ","));
            mailJsonObj.Add("cc", cc.Replace(";", ","));
            mailJsonObj.Add("bcc", bcc.Replace(";", ","));
            mailJsonObj.Add("subject", mail_subject);
            mailJsonObj.Add("body", mailcontent);
            mailJsonObj.Add("attachments", attachmentPath);
            mailJsonObj.Add("isnormalnotification", "true");

            var varlistObject = new JObject();
            var rowObject = new JObject();
            varlistObject.Add("row", rowObject);

            // Add "scriptsapi" and "varlist" objects to the outer object
            jsonObject.Add("scriptsapi", mailJsonObj);
            jsonObject.Add("varlist", varlistObject);
            string mailJsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            Console.WriteLine("Mail JSON: " + mailJsonString);
            string url = config.AppConfig["URL"];
            string method = "POST";

            if (method == "POST")
            {
                string Mediatype = "application/json";
                API _api = new API();
                try
                {
                    var apiResult = await _api.POSTData(url, mailJsonString, Mediatype);
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] }));

                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }));
                }
            }
            else
            {
                API _api = new API();
                try
                {
                    var apiResult = await _api.GetData(url);
                    Console.WriteLine(JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }));
                }
            }
            return true;
        }

        async Task<bool> ConvertFailureFiles(string localDirectory, string sourceDirectory, string failureFile, string sourceExtension, SftpClient sftpClient, string fileSeperator, string appName, string username, string mailcontent, string emailto, string trans_id, string mail_subject, string mailfrom, string cc, string bcc)
        {
            string failureFilePath = Path.Combine(localDirectory, failureFile);
            if (File.Exists(failureFilePath))
            {
                bool isConverted = false;
                string convertedFile = "";
                switch (sourceExtension)
                {
                    case ".txt":
                        isConverted = await ConvertCsvToTxt(localDirectory, failureFile);
                        if (isConverted)
                        {
                            Console.WriteLine($"Success in failure file conversion: {failureFile}");
                            convertedFile = Path.GetFileNameWithoutExtension(failureFile) + ".txt";
                        }
                        else
                            Console.WriteLine($"Error in failure file conversion: {failureFile}");
                        break;
                    case ".xls":
                        isConverted = await ConvertCsvToXLS(localDirectory, failureFile, Path.GetFileNameWithoutExtension(failureFile) + ".xls", fileSeperator);
                        if (isConverted)
                        {
                            Console.WriteLine($"Success in failure file conversion: {failureFile}");
                            convertedFile = Path.GetFileNameWithoutExtension(failureFile) + ".xls";
                        }
                        else
                            Console.WriteLine($"Error in failure file conversion: {failureFile}");
                        break;
                    case ".xlsx":
                        isConverted = await ConvertCsvToXLSX(localDirectory, failureFile, Path.GetFileNameWithoutExtension(failureFile) + ".xlsx", fileSeperator);
                        if (isConverted)
                        {
                            Console.WriteLine($"Success in failure file conversion: {failureFile}");
                            convertedFile = Path.GetFileNameWithoutExtension(failureFile) + ".xlsx";
                        }
                        else
                            Console.WriteLine($"Error in failure file conversion: {failureFile}");
                        break;
                    case ".csv":
                        isConverted = true;
                        Console.WriteLine($"CSV to CSV conversion skipped: {failureFile}");
                        convertedFile = Path.GetFileNameWithoutExtension(failureFile) + ".csv";
                        break;
                    default:
                        Console.WriteLine($"Unsupported file format: {sourceExtension}");
                        break;
                }
                if (isConverted && !string.IsNullOrEmpty(convertedFile))
                {
                    string convertedFilePath = Path.Combine(localDirectory, convertedFile);
                    await SendMail(appName, username, mailcontent, emailto, trans_id, mail_subject, mailfrom, cc, bcc, convertedFilePath);

                    if (sftpClient != null)
                    {
                        using (var stream = new FileStream(convertedFilePath, FileMode.Open, FileAccess.Read))
                        {
                            string targetFolder = Path.Combine(sourceDirectory, "Failure");
                            if (!sftpClient.Exists(targetFolder))
                            {
                                sftpClient.CreateDirectory(targetFolder);
                            }

                            sftpClient.UploadFile(stream, Path.Combine(targetFolder, convertedFile));
                        }
                    }
                    else
                    {
                        File.Copy(convertedFilePath, Path.Combine(sourceDirectory, convertedFile), true);
                    }
                    Console.WriteLine($"Uploaded: {convertedFilePath} to {Path.Combine(sourceDirectory, "Failure", convertedFile)}");

                }
                else
                    Console.WriteLine($"Error in failure file upload: {failureFile} to {sourceDirectory}");
            }
            else
                Console.WriteLine($"Failure file not exists: {failureFilePath}");
            return true;
        }

        async Task<bool> ConvertTxtToCsv(string txtFilePath, string localDirectory)
        {
            try
            {
                string csvFileName = Path.Combine(localDirectory, Path.GetFileNameWithoutExtension(txtFilePath) + ".csv");
                string csvContent = File.ReadAllText(txtFilePath);
                File.WriteAllText(csvFileName, csvContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting XLS to CSV: {ex.Message}");
                return false;
            }
        }

        async Task<bool> ConvertXLSXToCsv(string excelFilePath, string localDirectory, string fileSeperator)
        {
            try
            {
                using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new XSSFWorkbook(fileStream);
                    var worksheet = workbook.GetSheetAt(0);
                    string csvFileName = Path.Combine(localDirectory, Path.GetFileNameWithoutExtension(excelFilePath) + ".csv");
                    using (StreamWriter writer = new StreamWriter(csvFileName))
                    {
                        int rowCount = worksheet.LastRowNum + 1;

                        for (int row = 0; row < rowCount; row++)
                        {
                            var rowData = worksheet.GetRow(row);
                            if (rowData == null)
                                continue;

                            int colCount = rowData.LastCellNum;

                            var cellData = new object[colCount];
                            for (int col = 0; col < colCount; col++)
                            {
                                var cell = rowData.GetCell(col);
                                if (cell == null)
                                    cellData[col] = string.Empty;
                                else
                                {
                                    if (cell.CellType.ToString() != "String")
                                    {
                                        try
                                        {
                                            string dateStr = cell.DateCellValue.ToString();
                                            dateStr = dateStr.Replace(" 00:00:00", "");
                                            cellData[col] = dateStr;
                                        }
                                        catch
                                        {
                                            cellData[col] = cell;
                                        }
                                    }
                                    else
                                        cellData[col] = cell;
                                }
                            }

                            writer.WriteLine(string.Join(fileSeperator, cellData));
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Excel to CSV: {excelFilePath} - {ex.Message}");
                return false;
            }
        }

        async Task<bool> ConvertXLSToCsv(string excelFilePath, string localDirectory, string fileSeperator)
        {
            try
            {
                using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new HSSFWorkbook(fileStream);
                    var worksheet = workbook.GetSheetAt(0);
                    string csvFileName = Path.Combine(localDirectory, Path.GetFileNameWithoutExtension(excelFilePath) + ".csv");
                    using (StreamWriter writer = new StreamWriter(csvFileName))
                    {
                        int rowCount = worksheet.LastRowNum + 1;

                        for (int row = 0; row < rowCount; row++)
                        {
                            var rowData = worksheet.GetRow(row);
                            if (rowData == null)
                                continue;

                            int colCount = rowData.LastCellNum;

                            var cellData = new object[colCount];
                            for (int col = 0; col < colCount; col++)
                            {
                                var cell = rowData.GetCell(col);
                                if (cell == null)
                                    cellData[col] = string.Empty;
                                else
                                {
                                    if (cell.CellType.ToString() != "String")
                                    {
                                        try
                                        {
                                            string dateStr = cell.DateCellValue.ToString();
                                            dateStr = dateStr.Replace(" 00:00:00", "");
                                            cellData[col] = dateStr;
                                        }
                                        catch
                                        {
                                            cellData[col] = cell;
                                        }
                                    }
                                    else
                                        cellData[col] = cell;
                                }
                            }

                            writer.WriteLine(string.Join(fileSeperator, cellData));
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Excel to CSV: {excelFilePath} - {ex.Message}");
                return false;
            }
        }

        async Task<bool> ConvertCsvToXLSX(string localDirectory, string csvFile, string targetFile, string fileSeperator)
        {
            try
            {
                string csvFilePath = Path.Combine(localDirectory, csvFile);
                string excelFilePath = Path.Combine(localDirectory, targetFile);

                using (var workbook = new XSSFWorkbook())
                {
                    var sheet = workbook.CreateSheet("Sheet1");
                    var csvContent = File.ReadAllLines(csvFilePath);
                    for (int row = 0; row < csvContent.Length; row++)
                    {
                        var csvRow = csvContent[row].Split(fileSeperator);
                        var excelRow = sheet.CreateRow(row);
                        for (int col = 0; col < csvRow.Length; col++)
                        {
                            var cell = excelRow.CreateCell(col);
                            cell.SetCellValue(csvRow[col]);
                        }
                    }

                    using (var stream = new FileStream(excelFilePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(stream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting and uploading file: {csvFile} - {ex.Message}");
                return false;
            }
        }

        async Task<bool> ConvertCsvToXLS(string localDirectory, string csvFile, string targetFile, string fileSeperator)
        {
            try
            {
                string csvFilePath = Path.Combine(localDirectory, csvFile);
                string excelFilePath = Path.Combine(localDirectory, targetFile);

                using (var workbook = new HSSFWorkbook())
                {
                    var sheet = workbook.CreateSheet("Sheet1");
                    var csvContent = File.ReadAllLines(csvFilePath);
                    for (int row = 0; row < csvContent.Length; row++)
                    {
                        var csvRow = csvContent[row].Split(fileSeperator);
                        var excelRow = sheet.CreateRow(row);
                        for (int col = 0; col < csvRow.Length; col++)
                        {
                            var cell = excelRow.CreateCell(col);
                            cell.SetCellValue(csvRow[col]);
                        }
                    }

                    using (var stream = new FileStream(excelFilePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(stream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting and uploading file: {csvFile} - {ex.Message}");
                return false;
            }
        }

        async Task<bool> ConvertCsvToTxt(string localDirectory, string csvFile)
        {
            try
            {
                string csvFilePath = Path.Combine(localDirectory, csvFile);
                string txtFilePath = Path.Combine(localDirectory, Path.GetFileNameWithoutExtension(csvFile) + ".txt");
                if (File.Exists(csvFilePath))
                {
                    string csvContent = File.ReadAllText(csvFilePath);
                    File.WriteAllText(txtFilePath, csvContent);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting and uploading file: {csvFile} - {ex.Message}"); return false;
            }
        }

        async Task<SQLResult> ImportData(string appName, long id, string localDirectory, string localFileName, string csvFileName)
        {
            var context = new DataContext(configuration);
            var redis = new RedisHelper(configuration);
            Utils utils = new Utils(configuration, context, redis);
            string[] paramNames = { };
            DbType[] paramTypes = { };
            object[] paramValues = { };
            string sql = config.AppConfig["SFTPImportSQL"];
            //"select result from tbl_fn_hrms_attendance_downloaded_filepath_info($id$,'$localpath$','$sourcefile$','$csvfile$') as result";
            sql = sql.Replace("$id$", id.ToString()).Replace("$localpath$", localDirectory).Replace("$sourcefile$", localFileName).Replace("$csvfile$", csvFileName);
            Console.WriteLine("sql is :" + sql);
            Dictionary<string, string> dbConfig = await utils.GetDBConfigurations(appName);
            string connectionString = dbConfig["ConnectionString"];
            string dbType = dbConfig["DBType"];
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
            SQLResult table = new SQLResult();
            table = await dbHelper.ExecuteQueryAsyncs(sql, connectionString, paramNames, paramTypes, paramValues);
            return table;
        }

        async Task<string> CustomFileReadAsync(string filePath)
        {
            int maxRetries = 5;
            int retryDelayMilliseconds = 1000; // 1 second

            bool fileAccessed = false;
            int retryCount = 0;

            string fileResult = "";

            while (!fileAccessed && retryCount < maxRetries)
            {
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        fileAccessed = true;
                        byte[] b = new byte[1024];
                        UTF8Encoding temp = new UTF8Encoding(true);
                        StringBuilder sb = new StringBuilder();

                        while (fs.Read(b, 0, b.Length) > 0)
                        {
                            sb.Append(temp.GetString(b));
                        }

                        fileResult = sb.ToString();

                    }
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    retryCount++;
                    Thread.Sleep(retryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }

            if (!fileAccessed)
            {
                Console.WriteLine("Failed to access the file after multiple retries.");
            }

            return fileResult;
        }

        async Task<bool> CustomFileWriteAsync(string filePath, string fileContent)
        {
            int maxRetries = 5;
            int retryDelayMilliseconds = 1000; // 1 second

            bool fileAccessed = false;
            int retryCount = 0;

            while (!fileAccessed && retryCount < maxRetries)
            {
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        fileAccessed = true;
                        byte[] data = Encoding.UTF8.GetBytes(fileContent);
                        fs.Write(data, 0, data.Length);
                    }
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    retryCount++;
                    Thread.Sleep(retryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }

            if (!fileAccessed)
            {
                Console.WriteLine("Failed to access the file after multiple retries.");
                return false;
            }

            return true;
        }

        // Check if the IOException is due to a locked file
        static bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33; // 32: The process cannot access the file because it is being used by another process
        }

        rabbitMQConsumer.DoConsume(queueName, OnConsuming);
    }
}

