using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Program
{
    class Program
    {
        private static IConfiguration configuration;

        static void Main(string[] args)
        {
            try
            {
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);
                dynamic config = JsonConvert.DeserializeObject(json);
                string queueName = config.AppConfig["QueueName"];

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                configuration = builder.Build();

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton(configuration);
                serviceCollection.AddSingleton<IConfiguration>(configuration);
                serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
                serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

                var serviceProvider = serviceCollection.BuildServiceProvider();
                var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
                var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();

                string apiurl = configuration["AppConfig:ApiURL"];
                string apikey = configuration["AppConfig:ApiKey"];
                string waNumber = configuration["AppConfig:waNumber"];


                async Task<string> OnConsuming(string message)
                {
                    try
                    {
                        WriteMessage("OnConsuming method called with message: " + message);

                        JObject jobobj = JObject.Parse(message);
                        string queuedataString = jobobj["queuedata"]?.ToString();
                        if (string.IsNullOrEmpty(queuedataString))
                        {
                            WriteMessage("queuedata is null or empty");
                            return "";
                        }

                        queuedataString = queuedataString.Replace("\\", "\\\\");
                        JObject queuedataObject = JsonConvert.DeserializeObject<JObject>(queuedataString);

                        string recipientType = queuedataObject.SelectToken("payload.submitdata.dataarray.data.dc1.row1.recipienttype")?.ToString();
                        string whatsappnumber = queuedataObject.SelectToken("payload.submitdata.dataarray.data.dc1.row1.recipientmob_indi")?.ToString();
                        string txtmsg1 = queuedataObject.SelectToken("payload.submitdata.dataarray.data.dc1.row1.txtmsg1")?.ToString();
                        string filebase64 = queuedataObject.SelectToken("payload.submitdata.dataarray.data.dc1.row1.AxpFile_uploadfile.file1.fileasbase64")?.ToString();
                        string filename = queuedataObject.SelectToken("payload.submitdata.dataarray.data.dc1.row1.AxpFile_uploadfile.file1.filename")?.ToString();

                        string attachment_folder = configuration["AppConfig:attachment_folder"];
                        string attachmentpath = configuration["AppConfig:attachment_path"];

                        string attachmentPath = Path.Combine(attachment_folder, "WhatsApp");
                        Directory.CreateDirectory(attachmentPath);

                        string guid = Guid.NewGuid().ToString();
                        string directoryPath = Path.Combine(attachmentPath, guid);



                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        string filePath = Path.Combine(directoryPath, filename);
                        byte[] fileContent = Convert.FromBase64String(filebase64);

                        await File.WriteAllBytesAsync(filePath, fileContent);




                        string attachment_link = $"{attachmentpath}/{guid}/{filename}";

                        WriteMessage("File Path:" + attachment_link);



                        var jsonPayload = $@"
                            {{
                                ""messaging_product"": ""whatsapp"",
                                ""recipient_type"": ""{recipientType}"",
                                ""to"": ""{whatsappnumber}"",
                                ""type"": ""template"",
                                ""template"": {{
                                    ""language"": {{
                                        ""code"": ""en""
                                    }},
                                    ""name"": ""test_document"",
                                    ""components"": [
                                        {{
                                            ""type"": ""header"",
                                            ""parameters"": [
                                                {{
                                                    ""type"": ""document"",
                                                    ""document"": {{
                                                        ""link"": ""{attachment_link}""
                                                    }}
                                                }}
                                            ]
                                        }},
                                        {{
                                            ""type"": ""body"",
                                            ""parameters"": [
                                                {{
                                                    ""type"": ""text"",
                                                    ""text"": ""{txtmsg1}""
                                                }}
                                            ]
                                        }}
                                    ]
                                }}
                            }}";

                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("apikey", apikey);
                            client.DefaultRequestHeaders.Add("wanumber", waNumber);

                            StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                            try
                            {
                                HttpResponseMessage response = await client.PostAsync(apiurl, content);

                                if (response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine("Message sent successfully.");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error during API call: {ex.Message}");
                            }
                        }

                        return "";
                    }
                    catch (Exception ex)
                    {
                        WriteMessage("Error in OnConsuming method: " + ex.Message);
                        return ex.Message;
                    }
                }

                rabbitMQConsumer.DoConsume(queueName, OnConsuming);
            }
            catch (Exception ex)
            {
                WriteMessage("Unhandled exception: " + ex.Message);
            }
        }

        static void WriteMessage(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}
