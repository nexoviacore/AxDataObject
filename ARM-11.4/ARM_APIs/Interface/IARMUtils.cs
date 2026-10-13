namespace ARM_APIs.Interface
{
    public interface IARMUtils
    {
     abstract Task<bool> ConvertXLSToCsv(string sourceFilePath ,string delimeter);

        abstract Task<bool> ConvertTxtToCsv(string sourceFilePath);
        abstract Task<bool> ConvertXLSXToCsv(string sourceFilePath, string delimeter);

    }
}
