using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using ARMCommon.ActionFilter;
using ARMCommon.Model;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMUtilsController : ControllerBase
    {

        [RequiredFieldsFilter("Source", "Target", "FileName", "FilePath", "Delimiter")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMConvertFile")]
        public async Task<IActionResult> ARMConvertFile(ConversionRequest request)
        {
            bool conversionStatus = false;
            ARMResult result = new ARMResult();
            string sourceFilePath = Path.Combine(request.FilePath, request.FileName);
            if (System.IO.File.Exists(sourceFilePath))
            {
                if (request.Target.ToLower() == "csv")
                {

                    string sourceExtension = Path.GetExtension(request.FileName);
                    switch (sourceExtension)
                    {
                        case ".txt":
                            conversionStatus = await ConvertTxtToCsv(sourceFilePath);
                            break;
                        case ".xls":
                            conversionStatus = await ConvertXLSToCsv(sourceFilePath, request.Delimiter);
                            break;
                        case ".xlsx":
                            conversionStatus = await ConvertXLSXToCsv(sourceFilePath, request.Delimiter);
                            break;

                        default:
                            Console.WriteLine($"Unsupported file format: {sourceExtension}");
                            break;
                    }
                    if (conversionStatus)
                    {
                        result.result.Add("message", "SUCCESS");
                        return Ok(result);
                    }
                    else
                    {
                        result.result.Add("message", "Not able to convert file");
                        result.result.Add("messagetype", "Custom");
                        return BadRequest(result);

                    }
                }
                else
                {
                    result.result.Add("message", "TARGETFILEUNSUPPORTABLE");
                    return BadRequest(result);
                }
            }
            else
            {
                result.result.Add("message", "FILENOTEXIST");
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }

        }
        async Task<bool> ConvertTxtToCsv(string txtFilePath)
        {
            try
            {
                string csvFileName = Path.GetFileNameWithoutExtension(txtFilePath) + ".csv";
                string csvContent = System.IO.File.ReadAllText(txtFilePath);
                System.IO.File.WriteAllText(csvFileName, csvContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting XLS to CSV: {ex.Message}");
                return false;
            }
        }
        async Task<bool> ConvertXLSXToCsv(string excelFilePath, string fileSeperator)
        {
            try
            {
                using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new XSSFWorkbook(fileStream);
                    var worksheet = workbook.GetSheetAt(0);
                    string csvFilePath = Path.GetDirectoryName(excelFilePath);
                    string csvFileName = Path.GetFileNameWithoutExtension(excelFilePath) + ".csv";
                    string csvFile = Path.Combine(csvFilePath, csvFileName);
                    using (StreamWriter writer = new StreamWriter(csvFile))
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

        async Task<bool> ConvertXLSToCsv(string excelFilePath, string fileSeperator)
        {
            try
            {
                using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new HSSFWorkbook(fileStream);
                    var worksheet = workbook.GetSheetAt(0);
                    string csvFilePath = Path.GetDirectoryName(excelFilePath);
                    string csvFileName = Path.GetFileNameWithoutExtension(excelFilePath) + ".csv";
                    string csvFile = Path.Combine(csvFilePath, csvFileName);
                    using (StreamWriter writer = new StreamWriter(csvFile))
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




    }

}




//using ARM_APIs.Interface;
//using ARMCommon.ActionFilter;
//using ARMCommon.Model;
//using Microsoft.AspNetCore.Mvc;


//[Route("api/v{version:apiVersion}")]
//[ApiVersion("1")]
//[ApiController]
//public class ARMUtilsController : Controller
//{
//    private readonly IARMUtils _ARMUtils;

//public ARMUtilsController(IARMUtils aRMUtils)
//    {
//        _ARMUtils = aRMUtils;
//    }

//    [RequiredFieldsFilter("Source","Target","FileName","FilePath","Delimiter")]
//    [ServiceFilter(typeof(ApiResponseFilter))]
//    [HttpPost("ARMConvertFile")]
//    public async Task<IActionResult> ARMConvertFile(ConversionRequest request)
//    {
//        bool conversionStatus = false;
//        ARMResult result = new ARMResult();
//        string sourceFilePath = Path.Combine(request.FilePath, request.FileName);
//        if (System.IO.File.Exists(sourceFilePath))
//        {
//            if (request.Target.ToLower() == "csv")
//            {
//                string sourceExtension = Path.GetExtension(request.FileName);
//                switch (sourceExtension)
//                {
//                    case ".txt":
//                        conversionStatus = await _ARMUtils.ConvertTxtToCsv(sourceFilePath);
//                        break;
//                    case ".xls":
//                        conversionStatus = await _ARMUtils.ConvertXLSToCsv(sourceFilePath, request.Delimiter);
//                        break;
//                    case ".xlsx":
//                        conversionStatus = await _ARMUtils.ConvertXLSXToCsv(sourceFilePath, request.Delimiter);
//                        break;
//                    default:
//                        Console.WriteLine($"Unsupported file format: {sourceExtension}");
//                        break;
//                }
//                if (conversionStatus)
//                {
//                    result.result.Add("message", "SUCCESS");
//                    return Ok(result);
//                }
//                else
//                {
//                    result.result.Add("message", "Not able to convert file");
//                    result.result.Add("messagetype", "Custom");
//                    return BadRequest(result);
//                }
//            }
//            else
//            {
//                result.result.Add("message", "TARGETFILEUNSUPPORTABLE");
//                return BadRequest(result);
//            }
//        }
//        else
//        {
//            result.result.Add("message", "FILENOTEXIST");
//            result.result.Add("messagetype", "Custom");
//            return BadRequest(result);
//        }
//    }
//}
