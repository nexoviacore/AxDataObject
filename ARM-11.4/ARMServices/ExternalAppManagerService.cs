using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

public class ExternalAppManagerService : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private Timer _timer;

    public ExternalAppManagerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromSeconds(_configuration.GetValue<int>("AppConfig:Interval")));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void Execute(object state)
    {
        try
        {
            var applicationsSection = _configuration.GetSection("Applications");

            if (applicationsSection != null)
            {
                foreach (var appInfo in applicationsSection.GetChildren())
                {
                    var appPath = appInfo.GetValue<string>("Path");
                    var desiredInstances = appInfo.GetValue<int>("Instances");

                    ManageApplication(appPath, desiredInstances);
                }
            }
            else
            {
                Console.WriteLine("Applications section in appsettings.json is missing or empty.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Execute method: {ex.Message}");
        }
    }

    private void ManageApplication(string appPath, int desiredInstances)
    {
        var appName = Path.GetFileNameWithoutExtension(appPath);

        try
        {
            if (!IsProcessRunning(appName, appPath))
            {
                Console.WriteLine($"Starting {appName}");
                for (int i = 0; i < desiredInstances; i++)
                {
                    StartProcess(appPath);
                }
            }
            else
            {
                Console.WriteLine($"{appName} is running.");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ManageApplication method for {appName}: {ex.Message}");
        }
    }

    private bool IsProcessRunning(string processName, string appPath)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Any(p => string.Equals(p.MainModule.FileName, appPath, StringComparison.OrdinalIgnoreCase));
    }

    private void StartProcess(string appPath)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }

                    using (StreamReader reader = process.StandardError)
                    {
                        string error = reader.ReadToEnd();
                        Console.WriteLine(error);
                    }

                    process.WaitForExit();
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start {appPath}: {ex.Message}");
        }
    }
}
