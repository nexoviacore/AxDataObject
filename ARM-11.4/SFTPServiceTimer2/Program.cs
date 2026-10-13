using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Renci.SshNet;
using System.Text;

public class SFTPServiceTimer2
{
    private static string logFileName;
    private static Timer timer;


    public static async Task Main(string[] args)
    {
        var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
        var json = System.IO.File.ReadAllText(appSettingsPath);
        dynamic config = JsonConvert.DeserializeObject(json);

        var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();
        logFileName = $"SFTPService_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.log";
        int intervalInSeconds = int.Parse(configuration["AppConfig:ReadInterval"]);
        TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);
        timer = new Timer(Execute, null, TimeSpan.Zero, interval);
        Console.ReadKey();
    }


    private static void Execute(object state)
    {
        ProcessFiles().Wait();
    }

    public static async Task ProcessFiles()
    {
        var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        var sourceFolder = configuration["SFTP:SourcePath"];
        var destinationFolder = configuration["SFTP:DestinationPath"];
        var failureFolder = configuration["SFTP:FailurePath"];
        var duplicateFolder = configuration["SFTP:DuplicatePath"];
        string username = configuration["SFTP:Username"];
        string password = configuration["SFTP:Password"];
        string endpoint = configuration["SFTP:Endpoint"];
        int port = int.Parse(configuration["SFTP:Port"]);
        var project = configuration["AppConfig:Project"];
        string traceValue = configuration["SFTP:Trace"];
        string apiUsername = configuration["SFTP:apiusername"];
        string apiPassword = configuration["SFTP:apipassword"];
        var apiUrl = configuration["SFTP:apiURL"];
        var tablename = configuration["SFTP:tablename"];
        var inserttablename = configuration["SFTP:LogTableName"];
        var service_name = configuration["SFTP:servicename"];

        logFileName = $"SFTPService_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.log";
        try
        {
            using (var sftp = new SftpClient(endpoint, port, username, password))
            {
                sftp.Connect();
                WriteLog("Connected to the SFTP server");
                WriteMessage("Connected to the SFTP server");

                var files = sftp.ListDirectory(sourceFolder);
                foreach (var file in files)
                {
                    if (file.IsDirectory || file.IsSymbolicLink)
                    {
                        continue;
                    }


                    var filename = file.Name;
                    var sourceFilePath = Path.Combine(sourceFolder, filename);

                    // File exists in destination
                    var destinationFilePath = Path.Combine(destinationFolder, filename);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_hh:mm:ss");
                    var timestampedDuplicateFilePath = Path.Combine(duplicateFolder, $"{Path.GetFileNameWithoutExtension(filename)}_{timestamp}{Path.GetExtension(filename)}");

                    if (sftp.Exists(destinationFilePath))
                    {
                        sftp.RenameFile(sourceFilePath, timestampedDuplicateFilePath);
                        await InsertData(project, apiUsername, apiPassword, apiUrl, filename, "{\"Success\": \"true\", \"Message\":\"File moved to Duplicate folder.\"}", service_name, inserttablename, traceValue);
                        WriteMessage($"APIResult: {{\"Success\": \"true\", \"Message\":\"File moved to Duplicate folder.\", \"Filename\":\"{filename}\"}}");
                        WriteLog($"File {filename} moved to duplicate folder due to duplicate check.", filename);
                        continue;
                    }

                    // If new file, call API
                    var fileData = sftp.ReadAllBytes(sourceFilePath);
                    string fileContent = Encoding.UTF8.GetString(fileData);
                    var response = await SubmitApi(apiUrl, tablename, apiUsername, apiPassword, fileContent, filename, project, traceValue);

                    if (response.IsSuccess)
                    {
                        sftp.RenameFile(sourceFilePath, destinationFilePath);
                        await InsertData(project, apiUsername, apiPassword, apiUrl, filename, "{\"Success\": \"true\", \"Message\":\"File moved to destination folder.\"}", service_name, inserttablename, traceValue);
                        WriteMessage($"APIResult: {{\"Success\": \"true\", \"Message\":\"File moved to destination folder.\", \"Filename\":\"{filename}\"}}");
                        WriteLog($"File {filename} moved to destination folder.", filename);
                    }
                    else
                    {
                        var timestampedFailureFilePath = Path.Combine(failureFolder, $"{Path.GetFileNameWithoutExtension(filename)}_{timestamp}{Path.GetExtension(filename)}");
                        sftp.RenameFile(sourceFilePath, timestampedFailureFilePath);
                        WriteMessage($"APIResult: {{\"Success\": \"false\", \"Message\":\"File moved to failure folder due to API error.\", \"Filename\":\"{filename}\", \"ErrorMessage\":\"{response.Error}\"}}");
                        WriteLog($"File {filename} moved to failure folder due to API error: {response.Error}", filename);
                    }
                }

                sftp.Disconnect();
                WriteLog("Disconnected from the SFTP server.");
                WriteMessage("Disconnected from the SFTP server.");

                int intervalInSeconds = int.Parse(configuration["AppConfig:ReadInterval"]);
                TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);
                var nextOccurrence = DateTime.Now.Add(interval);
                Console.WriteLine($"Next Job scheduled for: {nextOccurrence.ToShortDateString()} - {nextOccurrence.ToString("hh:mm:ss tt")}");
                WriteLog($"Next Job scheduled At: {nextOccurrence.ToShortDateString()} - {nextOccurrence:hh:mm:ss tt}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}{ex.StackTrace}");
        }
    }
    static void WriteLog(string message, string level = "INFO")
    {
        try
        {
            string appFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "SFTPLogs");

            string logFilePath = Path.Combine(appFolderPath, logFileName);


            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }


            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logging failed: {ex.Message}{ex.StackTrace}");
        }
    }


    static void WriteLog(string message)
    {
        try
        {
            string appFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "SFTPLogs");

            string logFilePath = Path.Combine(appFolderPath, logFileName);
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logging failed: {ex.Message}{ex.StackTrace}");
        }
    }


    static void WriteMessage(string message)
    {
        Console.WriteLine(DateTime.Now.ToString() + " - " + message);
    }

    public static async Task<ApiResponse> SubmitApi(string apiUrl, string tablename, string apiUsername, string apiPassword, string fileContent, string fileName, string project, string traceValue)
    {
        string sftpDownloadId = Autogenerate.GeneratedownloadId();
        string docId = Autogenerate.GeneratedocId();

        using (var client = new HttpClient())
        {
            try
            {
                var payload = new Dictionary<string, object>
            {
                { "_parameters", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "getchoices", new Dictionary<string, object>
                            {
                                { "axpapp", project },
                                { "username", apiUsername },
                                { "password", apiPassword },
                                { "s", "" },
                                { "sql", $"INSERT INTO {tablename} ({tablename}id, docid, file_name, sftp_textfile) VALUES ('{sftpDownloadId}', '{docId}', '{fileName}', '{fileContent}')" },
                                { "direct", false },
                                { "params", "" },
                                { "trace", traceValue }
                            }}
                        }
                    }
                }
            };

                var jsonContent = JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);
                WriteMessage("API Input: " + jsonContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                WriteMessage("API Response: " + responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse(true, responseContent);
                }
                else
                {
                    return new ApiResponse(false, responseContent);
                }
            }
            catch (Exception ex)
            {
                WriteMessage("API Error: " + ex.Message);
                return new ApiResponse(false, ex.Message);
            }
        }
    }

    public static async Task<ApiResponse> InsertData(string project, string apiUsername, string apiPassword, string apiUrl, string fileName, string apiResponseContent, string service_name, string inserttablename, string traceValue)
    {
        string status = "Success";
        string sftpDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var client = new HttpClient())
        {
            try
            {
                var payload = new Dictionary<string, object>
            {
                { "_parameters", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "getchoices", new Dictionary<string, object>
                            {
                                { "axpapp", project },
                                { "username", apiUsername },
                                { "password", apiPassword },
                                { "s", "" },
                                { "sql", $@"INSERT INTO {inserttablename} (service_name, sftp_datetime, filename, status, api_response) VALUES ('{service_name}', '{sftpDateTime}', '{fileName}', '{status}', '{apiResponseContent}')" },
                                { "direct", false },
                                { "params", "" },
                                { "trace", traceValue }
                            }}
                        }
                    }
                }
            };

                var jsonContent = JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);
                WriteMessage("API Input for InsertData: " + jsonContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                WriteMessage("API Response from InsertData: " + responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse(true, responseContent);
                }
                else
                {
                    return new ApiResponse(false, responseContent);
                }
            }
            catch (Exception ex)
            {
                WriteMessage("API Error in InsertData: " + ex.Message);
                return new ApiResponse(false, ex.Message);
            }
        }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public ApiResponse(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }
    }
    public class Autogenerate
    {
        public static string GeneratedocId()
        {
            string prefix = "SFTP";
            long randomSuffix = new Random().NextInt64(1000000000000000L, 9999999999999999L);
            return $"{prefix}{randomSuffix}";
        }

        public static string GeneratedownloadId()
        {
            Random random = new Random();
            long part1 = (long)random.Next(100000000, 999999999);
            long part2 = (long)random.Next(100000000, 999999999);
            string combined = $"{part1}{part2}";

            if (combined.Length > 16)
            {
                combined = combined.Substring(0, 15);
            }
            return combined;
        }
    }
}
