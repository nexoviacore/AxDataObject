using ARMCommon.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ARMCommon.Helpers
{
    public class ServiceLog
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public ServiceLog(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private async Task<string> CreateOrUpdateLog(string status, bool IsMailSent)
        {
            try
            {
                var appConfigSection = _configuration.GetSection("AppConfig");
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                var otherInfoValue = JsonConvert.SerializeObject(appConfigSection.Get<Dictionary<string, object>>());
                Assembly assembly = Assembly.GetExecutingAssembly();
                string serviceName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                string exeName = assembly.Location;
                string exePath = Path.GetDirectoryName(exeName);
                string hostName = Dns.GetHostName();
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
                IPAddress ipv4Address = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                string ipv4AddressString = ipv4Address?.ToString();
                int processId = System.Diagnostics.Process.GetCurrentProcess().Id;

                try
                {
                    var existingRecord = await _context.ARMServiceLogs
                        .FirstOrDefaultAsync(r => r.ServiceName.ToLower() == serviceName.ToLower() &&
                                                  r.Server.ToLower() == ipv4AddressString.ToString().ToLower() &&
                                                  r.Folder.ToLower() == exePath.ToLower());

                    if (existingRecord != null)
                    {
                        existingRecord.Status = status;
                        existingRecord.LastOnline = DateTime.Now;
                        existingRecord.IsMailSent = IsMailSent;
                        existingRecord.Server = ipv4AddressString;
                        existingRecord.ServiceName = serviceName;
                        existingRecord.InstanceID = processId;

                    }
                    else
                    {
                        var newRecord = new ARMServiceLogs
                        {
                            ServiceName = serviceName,
                            Status = status,
                            Server = ipv4AddressString,
                            Folder = exePath,
                            InstanceID = processId,
                            LastOnline = DateTime.Now,
                            OtherInfo = otherInfoValue,
                            StartOnTime = DateTime.Now,
                            IsMailSent = IsMailSent
                        };
                        _context.ARMServiceLogs.Add(newRecord);
                    }

                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

                await _context.SaveChangesAsync();
                return "DATAUPDATED";
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }


        public Task<string> LogServiceStartedAsync()
        {
            return CreateOrUpdateLog("Started", false);
        }


    }
}
