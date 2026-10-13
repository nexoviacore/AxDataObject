namespace ARMCommon.Helpers
{
    public class FileOperation
    {
        private IConfiguration _configuration;
        private string _fileread;

        public FileOperation()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _fileread = _configuration["AppConfig:IsRead"];
        }

    }

}
