namespace ARMCommon.Helpers
{
    public class ARMLog
    {
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "APILog.txt");

        // Method to log messages to the file
        public static void WriteLog(string message)
        {
            try
            {
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logMessage = $"[{currentTime}] {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log: {ex.Message}");
            }
        }
    }
}
