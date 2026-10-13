using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

[Route("api/v{version:apiVersion}")]
[ApiVersion("1")]
[ApiController]
public class FileController : Controller
{
    private readonly IFiles _fileService;
    private readonly IConfiguration _config;

    public FileController(IFiles fileService, IConfiguration configuration)
    {
        _fileService = fileService;
        _config = configuration;
    }

    [RequiredFieldsFilter("RecordId", "FilePath")]
    [HttpPost("GetFileByRecordId")]
    public async Task<IActionResult> GetFileByRecordId(FileRequest request)
    {
        try
        {
            var result = await _fileService.GetFileByRecordId(request.RecordId, request.FilePath);


            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error in fetching data");
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in fetching data: {ex.Message}");
        }
    }

    [RequiredFieldsFilter("Filename", "FilePath")]
    [HttpPost("GetFileByFileName")]
    public async Task<IActionResult> GetFileByFileName(GetFilerequest request)
    {
        var result = await _fileService.GetFile(request.Filename, request.FilePath);

        if (result != null)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest("Error in fetching data");
        }
    }


    [RequiredFieldsFilter("Filenames", "FilePath")]
    [HttpPost("GetMultipleFilesByFileName")]
    public async Task<IActionResult> GetMultipleFilesByFileName(MultiFiles request)
    {
        var result = await _fileService.GetMultipleFiles(request.Filenames, request.FilePath);

        if (result != null)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest("Error in fetching data");
        }

    }

    [HttpGet("GetTraceFile/{fileName}")]
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
