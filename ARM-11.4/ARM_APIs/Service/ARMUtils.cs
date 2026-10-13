using ARM_APIs.Interface;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;

namespace ARM_APIs.Service
{
    public class ARMUtils : IARMUtils
    {
        public async Task<bool> ConvertXLSToCsv(string excelFilePath, string fileSeparator)
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

                            writer.WriteLine(string.Join(fileSeparator, cellData));
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


        public async Task<bool> ConvertTxtToCsv(string txtFilePath)
        {
            try
            {
                string csvFileName = Path.GetFileNameWithoutExtension(txtFilePath) + ".csv";
                string csvContent = File.ReadAllText(txtFilePath);
                File.WriteAllText(csvFileName, csvContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting XLS to CSV: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConvertXLSXToCsv(string excelFilePath, string fileSeparator)
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

                            writer.WriteLine(string.Join(fileSeparator, cellData));
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
