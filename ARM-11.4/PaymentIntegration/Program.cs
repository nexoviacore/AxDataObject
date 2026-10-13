//JB
using ARMCommon.Helpers;
using ARMCommon.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace PaymentIntegration
{
    internal class Program
    {
        private static IHttpClientFactory _httpClientFactory;
        private static Timer timer;
        private static string paymentprocessurl;
        private static string connectionString;

        private static int intervalInSeconds;
        private static IConfiguration configuration;

        static async Task Main(string[] args)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            dynamic config = JsonConvert.DeserializeObject(json);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            connectionString = configuration.GetConnectionString("WebApiDatabase");
            string _accessTokenUrl = configuration["AppConfig:AccessTokenUrl"];
            string paymentUrl = configuration["AppConfig:PaymentRequestUrl"];
            string paymentProcessUrl = configuration["AppConfig:PaymentProcessUrl"];
            string sourcefrom = configuration["AppConfig:sourcefrom"];
            string _username = configuration["AppConfig:username"];
            string _password = configuration["AppConfig:password"];
            string _clientid = configuration["AppConfig:client_id"];
            string _scope = configuration["AppConfig:scope"];
            string _granttype = configuration["AppConfig:grant_type"];
            string _clientsecret = configuration["AppConfig:client_secret"];
            string sql = configuration["AppConfig:sql"];
            string hostName = Dns.GetHostName();
            IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
            IPAddress ipv4Address = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            string ipv4AddressString = ipv4Address?.ToString();
            string logouturl = configuration["AppConfig:LogOutUrl"];

            string project = configuration["AppConfig:project"];

            intervalInSeconds = int.Parse(configuration["AppConfig:ReadInterval"]);
            TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);

            timer = new Timer(async _ => await Execute(logouturl, project, _accessTokenUrl, ipv4AddressString, sourcefrom, paymentUrl, paymentProcessUrl, sql, _username, _password, _clientid, _scope, _granttype, _clientsecret), null, TimeSpan.Zero, interval);
            Console.ReadKey();
        }

        static async Task Execute(string logouturl, string project, string _accessTokenUrl, string ipv4AddressString, string sourcefrom, string paymentUrl, string paymentProcessUrl, string sql, string _username, string _password, string _clientid, string _scope, string _granttype, string _clientsecret)
        {
            var auth = new AccessTokenRequestStruct
            {
                username = _username,
                password = _password,
                client_id = _clientid,
                scope = _scope,
                grant_type = _granttype,
                client_secret = _clientsecret
            };

            var dataTable = await GetDBDetails(connectionString, sql);
            foreach (DataRow row in dataTable.Rows)
            {
                string masterid = row["MASTERID"].ToString();
                string company = row["COMPANYID"].ToString();
                string jsonString = row["jsonstr"].ToString();

                jsonString = jsonString.Replace("},\"valDate\"", ",\"valDate\"").Trim();
                JObject jsonObject = JObject.Parse(jsonString);

                string cbcReference = jsonObject["cbcReference"]?.ToString();
                string masterId = masterid;

                await ProcessPayment(_clientsecret, _clientid, logouturl, masterId, auth, _accessTokenUrl, ipv4AddressString, sourcefrom, paymentUrl, jsonString, paymentProcessUrl, company, cbcReference);

            }



            intervalInSeconds = int.Parse(configuration["AppConfig:ReadInterval"]);
            TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);

            var nextOccurrence = DateTime.Now.Add(interval);
            Console.WriteLine($"Next Job scheduled for: {nextOccurrence.ToShortDateString()} - {nextOccurrence:hh:mm:ss tt}");
        }

        static async Task ProcessPayment(string _clientsecret, string _clientid, string logouturl, string masterId, AccessTokenRequestStruct auth, string _accessTokenUrl, string ipv4AddressString, string sourcefrom, string paymentUrl, string jsonString, string paymentProcessUrl, string company, string cbcReference)
        {
            var aceesstokenresult = await GetToken(masterId, auth, _accessTokenUrl, ipv4AddressString, sourcefrom);
            string token = aceesstokenresult?.TOKEN;
            string refresh_token = aceesstokenresult?.REFRESHTOKEN;

            if (string.IsNullOrWhiteSpace(token))
            {
                WriteMessage("Token is null or empty.");
                return;
            }

            WriteMessage($"Access Token API Result: {token}");

            try
            {
                var paymentRequestApiResult = await PaymentRequestAPI(masterId, token, paymentUrl, jsonString, ipv4AddressString, sourcefrom);
                var responseString = paymentRequestApiResult.PaymentRequestData?.RESPONSESSTRING;
                var statuscode = paymentRequestApiResult.PaymentRequestData?.STATUSCODE;
                var success = paymentRequestApiResult.PaymentRequestData?.success;



                if (statuscode == 200 && success == true)
                {
                    try
                    {
                        var paymentProcessApiResult = await PaymentProcessAPI(masterId, token, paymentProcessUrl, company, cbcReference, ipv4AddressString, sourcefrom);
                        var status = paymentProcessApiResult?.PAYMENTPROCESSDATA?.success.ToString();


                        if (status != "fail")
                        {
                            var responseStringPaymentProcess = paymentProcessApiResult?.PAYMENTPROCESSDATA?.RESPONSESSTRING;
                            string paymentProcessSerializedResponse = JsonConvert.SerializeObject(responseStringPaymentProcess, Formatting.Indented);
                            WriteMessage($"Payment Process API Result: {paymentProcessSerializedResponse}");

                        }
                        else
                        {
                            WriteMessage("Payment process status is 'fail'; not calling the third API.");
                            LogoutAPI(logouturl, _clientid, refresh_token, _clientsecret, sourcefrom, masterId, ipv4AddressString);
                            WriteMessage("Logout Successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteMessage($"An error occurred while processing PaymentProcessAPI: {ex.Message}{ex.StackTrace}");
                    }
                }
                else
                {
                    if (statuscode != 200)
                    {
                        WriteMessage($"Payment Request API Failed with status code {statuscode}. Skipping Payment Process API call.");
                        LogoutAPI(logouturl, _clientid, refresh_token, _clientsecret, sourcefrom, masterId, ipv4AddressString);
                        WriteMessage("Logout Successfully");
                    }
                    else if (success == false)
                    {
                        WriteMessage("Payment Request API returned success = false. Skipping Payment Process API call.");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMessage($"An error occurred while processing PaymentRequestAPI: {ex.Message}\n{ex.StackTrace}");
                await LogoutAPI(logouturl, _clientid, refresh_token, _clientsecret, sourcefrom, masterId, ipv4AddressString);
                WriteMessage("Logout Successfully");
            }

            await LogoutAPI(logouturl, _clientid, refresh_token, _clientsecret, sourcefrom, masterId, ipv4AddressString);
            WriteMessage("Logout Successfully");
        }



        public static async Task<TokenResult> GetToken(string masterId, AccessTokenRequestStruct auth, string accessTokenUrl, string ipv4AddressString, string sourcefrom)

        {
            try
            {
                var client = _httpClientFactory.CreateClient("SecureClient");

                var formContent = new FormUrlEncodedContent(new[]
                {
                            new KeyValuePair<string, string>("username", auth.username),
                            new KeyValuePair<string, string>("password", auth.password),
                            new KeyValuePair<string, string>("client_id", auth.client_id),
                            new KeyValuePair<string, string>("scope", auth.scope),
                            new KeyValuePair<string, string>("grant_type", auth.grant_type),
                            new KeyValuePair<string, string>("client_secret", auth.client_secret)
                        });


                var requestId = Guid.NewGuid().ToString();

                var requestData = new AxRequest
                {
                    //91d2dab2-46ef-4505-b692-305cd3e4f173
                    //91d2dab2-46ef-4505-b692-305cd3e4f173
                    REQUESTID = requestId,
                    REQUESTRECEIVEDTIME = DateTime.Now,
                    SOURCEFROM = sourcefrom,
                    REQUESTSTRING = "",
                    HEADERS = formContent.Headers.ToString(),
                    PARAMS = "NULL",
                    AUTHZ = "NULL",
                    CONTENTTYPE = formContent.Headers.ContentType?.ToString(),
                    CONTENTLENGTH = formContent.Headers.ContentLength?.ToString(),
                    HOST = new Uri(accessTokenUrl).Host,
                    URL = accessTokenUrl,
                    ENDPOINT = new Uri(accessTokenUrl).AbsolutePath,
                    REQUESTMETHOD = "POST",
                    USERNAME = auth.username,
                    ADDITIONALDETAILS = masterId,
                    SOURCEMACHINEIP = ipv4AddressString,
                    APINAME = "AccessToken"
                };


                await InsertRequestData(connectionString, requestData.REQUESTID, DateTimeOffset.Now, requestData.SOURCEFROM, requestData.REQUESTSTRING, requestData.HEADERS, requestData.PARAMS, requestData.AUTHZ, requestData.CONTENTTYPE, requestData.CONTENTLENGTH, requestData.HOST, requestData.URL, requestData.ENDPOINT, requestData.REQUESTMETHOD, requestData.USERNAME, requestData.ADDITIONALDETAILS, requestData.SOURCEMACHINEIP, requestData.APINAME);

                var startTime = DateTime.Now;
                HttpResponseMessage response;

                try
                {
                    response = await client.PostAsync(accessTokenUrl, formContent);
                }
                catch (HttpRequestException httpEx)
                {

                    WriteMessage("Request error: " + httpEx.Message);
                    throw;
                }

                var endTime = DateTime.Now;
                var executionTime = (endTime - startTime).TotalMilliseconds.ToString() + "ms";
                var responseContent = await response.Content.ReadAsStringAsync();


                string accessToken = null;
                string refreshtoken = null;
                try
                {
                    var json = JObject.Parse(responseContent);
                    accessToken = json["access_token"]?.ToString();
                    refreshtoken = json["refresh_token"]?.ToString();
                }
                catch (JsonException jsonEx)
                {

                    WriteMessage("JSON parsing error: " + jsonEx.Message);

                    throw;
                }
                var responseData = new AxRequest
                {
                    RESPONSEID = requestId,
                    RESPONSESENTTIME = DateTimeOffset.Now.DateTime,
                    STATUSCODE = (int)response.StatusCode,
                    RESPONSESSTRING = responseContent,
                    HEADERS = response.Headers.ToString(),
                    CONTENTTYPE = response.Content.Headers.ContentType?.ToString(),
                    CONTENTLENGTH = response.Content.Headers.ContentLength?.ToString(),
                    ERRORDDETAILS = response.IsSuccessStatusCode ? "" : response.ReasonPhrase,
                    ENDPOINT = new Uri(accessTokenUrl).AbsolutePath,
                    REQUESTMETHOD = "POST",
                    USERNAME = auth.username,
                    ADDITIONALDETAILS = masterId,
                    REQUESTID = requestId,
                    EXECUTIONTIME = executionTime
                };

                await InsertResponseData(connectionString, responseData.RESPONSEID, DateTimeOffset.Now, responseData.STATUSCODE, responseData.RESPONSESSTRING, responseData.HEADERS, responseData.CONTENTTYPE, responseData.CONTENTLENGTH, responseData.ERRORDDETAILS, responseData.ENDPOINT, responseData.REQUESTMETHOD, responseData.USERNAME, responseData.ADDITIONALDETAILS, responseData.REQUESTID, responseData.EXECUTIONTIME);

                return new TokenResult
                {
                    TOKEN = accessToken,
                    REFRESHTOKEN = refreshtoken,
                    REQUESTDATA = requestData
                };
            }
            catch (Exception ex)
            {
                WriteMessage("Error in GetToken: " + ex.Message);
                throw;
            }
        }


        public static async Task<TokenResult> PaymentRequestAPI(string masterId, string token, string paymentUrl, string json2, string ipv4AddressString, string sourcefrom)
        {
            var reqID = Guid.NewGuid().ToString();
            var client = _httpClientFactory.CreateClient("SecureClient");

            var requestData = new AxRequest
            {

                REQUESTID = reqID,
                REQUESTRECEIVEDTIME = DateTime.Now,
                SOURCEFROM = sourcefrom,
                REQUESTSTRING = json2,
                HEADERS = "NULL",
                PARAMS = "NULL",
                AUTHZ = "NULL",
                CONTENTTYPE = "application/json",
                CONTENTLENGTH = json2.Length.ToString(),
                HOST = new Uri(paymentUrl).Host,
                URL = paymentUrl,
                ENDPOINT = new Uri(paymentUrl).AbsolutePath,
                REQUESTMETHOD = "POST",
                USERNAME = "NULL",
                ADDITIONALDETAILS = masterId,
                SOURCEMACHINEIP = ipv4AddressString,
                APINAME = "PaymentRequest"

            };

            await InsertRequestData(connectionString, requestData.REQUESTID, DateTimeOffset.Now, requestData.SOURCEFROM, requestData.REQUESTSTRING, requestData.HEADERS, requestData.PARAMS, requestData.AUTHZ, requestData.CONTENTTYPE, requestData.CONTENTLENGTH, requestData.HOST, requestData.URL, requestData.ENDPOINT, requestData.REQUESTMETHOD, requestData.USERNAME, requestData.ADDITIONALDETAILS, requestData.SOURCEMACHINEIP, requestData.APINAME);
            HttpResponseMessage postPaymentResponse = null;
            string postPaymentResponseContent = string.Empty;
            string executionTime = string.Empty;
            string errorDetails = string.Empty;

            try
            {

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var httpContent = new StringContent(json2, Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                try
                {
                    postPaymentResponse = await client.PostAsync(paymentUrl, httpContent);
                }
                catch (HttpRequestException httpEx)
                {
                    WriteMessage("Request error: " + httpEx.Message);
                    errorDetails = httpEx.Message;
                    throw;
                }
                var endTime = DateTime.UtcNow;
                var executionDuration = endTime - startTime;
                executionTime = Math.Round(executionDuration.TotalMilliseconds).ToString() + "ms";

                postPaymentResponseContent = await postPaymentResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                WriteMessage("Error in PaymentRequestAPI: " + ex.Message);
                errorDetails = ex.Message;
            }
            finally
            {
                var responseData = new AxRequest
                {
                    REQUESTID = reqID,
                    REQUESTRECEIVEDTIME = requestData.REQUESTRECEIVEDTIME,
                    SOURCEFROM = requestData.SOURCEFROM,
                    REQUESTSTRING = requestData.REQUESTSTRING,
                    HEADERS = postPaymentResponse?.Headers.ToString() ?? "",
                    AUTHZ = token,
                    PARAMS = "NULL",
                    CONTENTTYPE = postPaymentResponse?.Content.Headers.ContentType?.ToString() ?? "",
                    CONTENTLENGTH = postPaymentResponse?.Content.Headers.ContentLength?.ToString() ?? "",
                    HOST = new Uri(paymentUrl).Host,
                    URL = paymentUrl,
                    ENDPOINT = new Uri(paymentUrl).AbsolutePath,
                    REQUESTMETHOD = requestData.REQUESTMETHOD,
                    USERNAME = "NULL",
                    ADDITIONALDETAILS = masterId,
                    SOURCEMACHINEIP = requestData.SOURCEMACHINEIP,
                    APINAME = requestData.APINAME,
                    RESPONSEID = reqID,
                    RESPONSESENTTIME = DateTimeOffset.Now.DateTime,
                    RESPONSESSTRING = postPaymentResponseContent,
                    STATUSCODE = (int)(postPaymentResponse?.StatusCode ?? HttpStatusCode.InternalServerError),
                    EXECUTIONTIME = executionTime,
                    ERRORDDETAILS = errorDetails
                };

                await InsertResponseData(connectionString, responseData.RESPONSEID, DateTimeOffset.Now, responseData.STATUSCODE, responseData.RESPONSESSTRING, responseData.HEADERS, responseData.CONTENTTYPE, responseData.CONTENTLENGTH, responseData.ERRORDDETAILS, responseData.ENDPOINT, responseData.REQUESTMETHOD, responseData.USERNAME, responseData.ADDITIONALDETAILS, responseData.REQUESTID, responseData.EXECUTIONTIME);
            }

            return new TokenResult
            {
                PaymentRequestData = new AxRequest
                {
                    REQUESTID = reqID,
                    RESPONSESSTRING = postPaymentResponseContent,
                    STATUSCODE = (int?)postPaymentResponse?.StatusCode ?? 500
                }
            };
        }


        public static async Task<TokenResult> PaymentProcessAPI(string masterId, string token, string paymentProcessUrl, string company, string cbcreference, string ipv4AddressString, string sourcefrom)
        {
            var requestJson = new
            {
                COMPANY = company,
                CBCREF = cbcreference
            };

            string jsonString = JObject.FromObject(requestJson).ToString();

            var rid = Guid.NewGuid().ToString();
            var requestData = new AxRequest
            {
                REQUESTID = rid,
                REQUESTRECEIVEDTIME = DateTime.Now,
                SOURCEFROM = sourcefrom,
                REQUESTSTRING = jsonString,
                AUTHZ = token,
                HEADERS = "NULL",
                PARAMS = "NULL",
                CONTENTTYPE = "application/json",
                CONTENTLENGTH = jsonString.Length.ToString(),
                HOST = new Uri(paymentProcessUrl).Host,
                URL = paymentProcessUrl,
                ENDPOINT = new Uri(paymentProcessUrl).AbsolutePath,
                REQUESTMETHOD = "POST",
                USERNAME = "NULL",
                ADDITIONALDETAILS = masterId,
                SOURCEMACHINEIP = ipv4AddressString,
                APINAME = "PaymentProcess",
            };

            await InsertRequestData(connectionString, requestData.REQUESTID, DateTimeOffset.Now, requestData.SOURCEFROM, requestData.REQUESTSTRING, requestData.HEADERS, requestData.PARAMS, requestData.AUTHZ, requestData.CONTENTTYPE, requestData.CONTENTLENGTH, requestData.HOST, requestData.URL, requestData.ENDPOINT, requestData.REQUESTMETHOD, requestData.USERNAME, requestData.ADDITIONALDETAILS, requestData.SOURCEMACHINEIP, requestData.APINAME);
            HttpResponseMessage postPaymentResponse = null;
            string postPaymentResponseContent = string.Empty;
            string executionTime = string.Empty;
            string errorDetails = string.Empty;

            try
            {
                var client = _httpClientFactory.CreateClient("SecureClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                try
                {
                    postPaymentResponse = await client.PostAsync(paymentProcessUrl, httpContent);
                }
                catch (HttpRequestException httpEx)
                {
                    WriteMessage("Request error: " + httpEx.Message);
                    errorDetails = httpEx.Message;
                    throw;
                }
                var endTime = DateTime.UtcNow;
                var executionDuration = endTime - startTime;
                executionTime = Math.Round(executionDuration.TotalMilliseconds).ToString() + "ms";

                postPaymentResponseContent = await postPaymentResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                WriteMessage("Error in PaymentProcessAPI: " + ex.Message);
                errorDetails = ex.Message;
            }
            finally
            {
                var responseData = new AxRequest
                {
                    REQUESTID = rid,
                    REQUESTRECEIVEDTIME = requestData.REQUESTRECEIVEDTIME,
                    SOURCEFROM = requestData.SOURCEFROM,
                    REQUESTSTRING = requestData.REQUESTSTRING,
                    HEADERS = postPaymentResponse?.Headers.ToString() ?? "",
                    AUTHZ = token,
                    PARAMS = "NULL",
                    CONTENTTYPE = postPaymentResponse?.Content.Headers.ContentType?.ToString() ?? "",
                    CONTENTLENGTH = postPaymentResponse?.Content.Headers.ContentLength?.ToString() ?? "",
                    HOST = new Uri(paymentProcessUrl).Host,
                    URL = paymentProcessUrl,
                    ENDPOINT = new Uri(paymentProcessUrl).AbsolutePath,
                    REQUESTMETHOD = requestData.REQUESTMETHOD,
                    USERNAME = "NULL",
                    ADDITIONALDETAILS = masterId,
                    SOURCEMACHINEIP = requestData.SOURCEMACHINEIP,
                    APINAME = requestData.APINAME,
                    RESPONSEID = rid,
                    RESPONSESENTTIME = DateTime.Now,
                    RESPONSESSTRING = postPaymentResponseContent,
                    STATUSCODE = (int)(postPaymentResponse?.StatusCode ?? HttpStatusCode.InternalServerError),
                    EXECUTIONTIME = executionTime,
                    ERRORDDETAILS = errorDetails
                };

                await InsertResponseData(connectionString, responseData.RESPONSEID, DateTimeOffset.Now, responseData.STATUSCODE, responseData.RESPONSESSTRING, responseData.HEADERS, responseData.CONTENTTYPE, responseData.CONTENTLENGTH, responseData.ERRORDDETAILS, responseData.ENDPOINT, responseData.REQUESTMETHOD, responseData.USERNAME, responseData.ADDITIONALDETAILS, responseData.REQUESTID, responseData.EXECUTIONTIME);
            }

            return new TokenResult
            {
                PAYMENTPROCESSDATA = new AxRequest
                {
                    REQUESTID = rid,
                    RESPONSESSTRING = postPaymentResponseContent,
                    STATUSCODE = (int)(postPaymentResponse?.StatusCode ?? HttpStatusCode.InternalServerError)
                }
            };
        }



        public static async Task<TokenResult> LogoutAPI(string logouturl, string _clientid, string refresh_token, string _clientsecret, string sourcefrom, string masterid, string ipv4AddressString)
        {
            var rid = Guid.NewGuid().ToString();
            var requestData = new AxRequest
            {
                REQUESTID = rid,
                REQUESTRECEIVEDTIME = DateTime.Now,
                SOURCEFROM = sourcefrom,
                REQUESTSTRING = "NULL",
                AUTHZ = "NULL",
                HEADERS = "NULL",
                PARAMS = "NULL",
                CONTENTTYPE = "application/x-www-form-urlencoded",
                CONTENTLENGTH = "NULL",
                HOST = new Uri(logouturl).Host,
                URL = logouturl,
                ENDPOINT = new Uri(logouturl).AbsolutePath,
                REQUESTMETHOD = "POST",
                USERNAME = "NULL",
                ADDITIONALDETAILS = masterid,
                SOURCEMACHINEIP = ipv4AddressString,
                APINAME = "LogoutAPI",
            };

            // Prepare content for POST request
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("client_id", _clientid),
        new KeyValuePair<string, string>("refresh_token", refresh_token),
        new KeyValuePair<string, string>("client_secret", _clientsecret)
        });

            // Update content length
            requestData.CONTENTLENGTH = content.Headers.ContentLength?.ToString() ?? "0";

            // Insert request data
            await InsertRequestData(connectionString, requestData.REQUESTID, DateTimeOffset.Now, requestData.SOURCEFROM, requestData.REQUESTSTRING, requestData.HEADERS, requestData.PARAMS, requestData.AUTHZ, requestData.CONTENTTYPE, requestData.CONTENTLENGTH, requestData.HOST, requestData.URL, requestData.ENDPOINT, requestData.REQUESTMETHOD, requestData.USERNAME, requestData.ADDITIONALDETAILS, requestData.SOURCEMACHINEIP, requestData.APINAME);

            HttpResponseMessage response = null;
            string responseContent = string.Empty;
            string executionTime = string.Empty;
            string errorDetails = string.Empty;

            try
            {
                var client = _httpClientFactory.CreateClient("SecureClient");

                // Send the request and measure execution time
                var startTime = DateTime.UtcNow;
                response = await client.PostAsync(logouturl, content);
                var endTime = DateTime.UtcNow;
                executionTime = Math.Round((endTime - startTime).TotalMilliseconds).ToString() + "ms";

                // Ensure a successful response
                response.EnsureSuccessStatusCode();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException httpEx)
            {
                errorDetails = httpEx.Message;

                throw;
            }
            finally
            {
                // Prepare response data for logging
                var responseData = new AxRequest
                {
                    REQUESTID = rid,
                    REQUESTRECEIVEDTIME = requestData.REQUESTRECEIVEDTIME,
                    SOURCEFROM = requestData.SOURCEFROM,
                    RESPONSESSTRING = responseContent,
                    HEADERS = "NULL",
                    AUTHZ = "NULL",
                    PARAMS = "NULL",
                    CONTENTTYPE = response?.Content.Headers.ContentType?.ToString() ?? "",
                    CONTENTLENGTH = response?.Content.Headers.ContentLength?.ToString() ?? "",
                    HOST = new Uri(logouturl).Host,
                    URL = logouturl,
                    ENDPOINT = new Uri(logouturl).AbsolutePath,
                    REQUESTMETHOD = requestData.REQUESTMETHOD,
                    USERNAME = "NULL",
                    ADDITIONALDETAILS = masterid,
                    SOURCEMACHINEIP = ipv4AddressString,
                    APINAME = requestData.APINAME,
                    RESPONSEID = rid,
                    RESPONSESENTTIME = DateTime.Now,
                    STATUSCODE = (int)(response?.StatusCode ?? HttpStatusCode.InternalServerError),
                    EXECUTIONTIME = executionTime,
                    ERRORDDETAILS = errorDetails



                };

                await InsertResponseData(connectionString, responseData.RESPONSEID, DateTimeOffset.Now, responseData.STATUSCODE, responseData.RESPONSESSTRING, responseData.HEADERS, responseData.CONTENTTYPE, responseData.CONTENTLENGTH, errorDetails, responseData.ENDPOINT, responseData.REQUESTMETHOD, responseData.USERNAME, responseData.ADDITIONALDETAILS, responseData.REQUESTID, responseData.EXECUTIONTIME);
            }

            return new TokenResult
            {
                LogoutData = new AxRequest
                {
                    REQUESTID = rid,
                    RESPONSESSTRING = responseContent,
                    STATUSCODE = (int)(response?.StatusCode ?? HttpStatusCode.InternalServerError)
                }
            };
        }


        static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);
            //services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
            //services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

            services.AddHttpClient("SecureClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        {
                            return true;
                        }
                    };
                });

            services.AddHttpClient();
        }
        public class AccessTokenRequestStruct
        {
            public string client_secret { get; set; }
            public string client_id { get; set; }
            public string grant_type { get; set; }
            public string password { get; set; }
            public string scope { get; set; }
            public string username { get; set; }
        }



        static void WriteMessage(string message)
        {
            Console.WriteLine(DateTime.Now.ToString() + " - " + message);
        }
        public static async Task InsertRequestData(string connectionString, string requestid, DateTimeOffset requestreceivedtime, string sourcefrom, string requeststring, string headers, string @params, string authz, string contenttype, string contentlength, string host, string url, string endpoint, string requestmethod, string username, string additionaldetails, string sourcemachineip, string apiname)
        {
            try
            {
                string requestreceivedtimeFormatted = requestreceivedtime.ToString("yyyy-MM-dd hh:mm:ss.fff");
                string dbType = "oracle";
                string sql = Constants_SQL.INSERTREQUESTSTRING_ORACLE;
                sql = string.Format(sql, requestid, requestreceivedtimeFormatted, sourcefrom, headers, @params, authz, contenttype, contentlength, host, url, endpoint, requestmethod, username, additionaldetails, sourcemachineip, apiname);
                WriteMessage("REQUEST INSERT QUERY:" + sql);
                WriteMessage("REQUEST INSERT REQUEST_STRING:" + requeststring);
                string[] paramName = { "@requeststr" };
                DbType[] paramType = { DbType.String };
                object[] paramValue = { requeststring };

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            catch (Exception ex)
            {
                WriteMessage($"Error in InsertRequestData for {apiname}: {ex.Message}{ex.StackTrace}");
                throw;
            }
        }

        public static async Task InsertResponseData(string connectionString, string responseid, DateTimeOffset responsesenttime, int statuscode, string responsestring, string headers, string contenttype, string contentlength, string errordetails, string endpoint, string requestmethod, string username, string additionaldetails, string requestid, string executiontime)
        {
            try
            {
                string responsereceivedtimeFormatted = responsesenttime.ToString("yyyy-MM-dd hh:mm:ss.fff");
                string dbType = "oracle";
                string sql = Constants_SQL.INSERTRESPONSESTRING_ORACLE;
                sql = string.Format(sql, responseid, responsereceivedtimeFormatted, statuscode, headers, contenttype, contentlength, errordetails, endpoint, requestmethod, username, additionaldetails, requestid, executiontime);
                WriteMessage("RESPONSE INSERT QUERY:" + sql);
                WriteMessage("RESPONSE INSERT RESPONSE_STRING:" + responsestring);
                string[] paramName = { "@responsestr" };
                DbType[] paramType = { DbType.String };
                object[] paramValue = { responsestring };

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            catch (Exception ex)
            {
                WriteMessage($"Error in InsertResponseData : {ex.Message}{ex.StackTrace}");
                throw;
            }
        }


        static async Task<DataTable> GetDBDetails(string connectionString, string sql)
        {
            try
            {
                string dbType = "oracle";
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                DataTable dataTable = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}{ex.StackTrace}");
            }

            return new DataTable();
        }

    }

    public class AxRequest
    {
        public bool success { get; set; }

        public string? REQUESTID { get; set; }
        public DateTime? REQUESTRECEIVEDTIME { get; set; }
        public string? SOURCEFROM { get; set; }
        public string? REQUESTSTRING { get; set; }
        public string? HEADERS { get; set; }
        public string? PARAMS { get; set; }
        public string? AUTHZ { get; set; }
        public string? CONTENTTYPE { get; set; }
        public string? CONTENTLENGTH { get; set; }
        public string? HOST { get; set; }
        public string? URL { get; set; }
        public string? ENDPOINT { get; set; }
        public string? REQUESTMETHOD { get; set; }
        public string? USERNAME { get; set; }
        public string? ADDITIONALDETAILS { get; set; }
        public string? SOURCEMACHINEIP { get; set; }
        public string? APINAME { get; set; }
        public string? RESPONSEID { get; set; }
        public DateTime? RESPONSESENTTIME { get; set; }
        public string? RESPONSESSTRING { get; set; }
        public int STATUSCODE { get; set; }
        public string? EXECUTIONTIME { get; set; }
        public string? ERRORDDETAILS { get; set; }
    }

    public class TokenResult
    {
        public string TOKEN { get; set; }

        public string REFRESHTOKEN { get; set; }
        public AxRequest REQUESTDATA { get; set; }
        public AxRequest PaymentRequestData { get; set; }
        public AxRequest PAYMENTPROCESSDATA { get; set; }

        public AxRequest LogoutData { get; set; }
    }

    public class PaymentRequestData
    {
        public string? status { get; set; }
        public string? success { get; set; }
    }


}

