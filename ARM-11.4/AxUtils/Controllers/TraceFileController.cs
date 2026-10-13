using Microsoft.AspNetCore.Mvc;
using System.Text;

[Route("api/v1")]
[ApiController]
public class TraceFileController : Controller
{
    public TraceFileController()
    {        
    }


    [HttpGet("GetTrace/{fileName}")]
    public async Task<IActionResult> GetTraceFile(string fileName)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Filename is required.");
            }

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var _filePath = Path.Combine(exeDir, "Logs");
            if (!Directory.Exists(_filePath))
                return BadRequest($"Logs folder not found.");

            var filePath = Path.Combine(_filePath, fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest($"File '{fileName}' not found.");
            }

            string content = await System.IO.File.ReadAllTextAsync(filePath);

            // ✅ Convert line breaks to <br> for HTML formatting
            var htmlContent = new StringBuilder();
            htmlContent.Append("<html><body>");
            htmlContent.Append(content.Replace("\n", "<br>").Replace("\r", ""));
            htmlContent.Append("</body></html>");

            return Content(htmlContent.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error reading file: {ex.Message}");
        }
    }



}
