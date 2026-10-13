using ARM_APIs.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;


public class FileService : IFiles
{
    public async Task<string> GetFileByRecordId(string recordId, string filePath)
    {
        var result = new ARMResult();

        try
        {
            // Check if the folder exists
            if (!Directory.Exists(filePath))
            {
                result.result.Add("success", false);
                result.result.Add("error", "The specified folder does not exist.");
                return JsonConvert.SerializeObject(result.result);
            }

            // Search for files with the given recordId
            string[] files = Directory.GetFiles(filePath, $"{recordId}.*");

            if (files.Length == 0)
            {
                result.result.Add("success", false);
                result.result.Add("error", "File with the specified recordId does not exist.");
            }
            else
            {
                string fullPath = files[0];
                string fileName = Path.GetFileName(fullPath);

                byte[] fileBytes = await File.ReadAllBytesAsync(fullPath);
                string base64String = Convert.ToBase64String(fileBytes);

                result.result.Add("success", true);
                result.result.Add("filename", fileName);
                result.result.Add("base64", base64String);
            }
        }
        catch (Exception ex)
        {
            result.result.Add("success", false);
            result.result.Add("error", $"An error occurred: {ex.Message}");
        }

        return JsonConvert.SerializeObject(result.result);
    }



    public async Task<FileResponse> GetFile(string filename, string filePath)
    {
        var output = new FileResponse();

        try
        {
            string fullPath = Path.Combine(filePath, filename);

            if (!File.Exists(fullPath))
            {
                output.Success = false;
                output.Error = "File path does not exist";
            }
            else
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(fullPath);
                string base64String = Convert.ToBase64String(fileBytes);
                output.Success = true;
                output.Filename = filename;
                output.Base64 = base64String;
            }
        }
        catch (Exception ex)
        {
            output.Success = false;
            output.Error = $"An error occurred: {ex.Message}";
        }

        return output;
    }


    public async Task<MultiFileResponse> GetMultipleFiles(string[] filenames, string filePath)
    {
        var multiFileResponse = new MultiFileResponse
        {
            Success = true,
            Result = new List<FileResult>()
        };

        foreach (var filename in filenames)
        {
            var fileResult = new FileResult();
            try
            {
                string fullPath = Path.Combine(filePath, filename);

                if (!File.Exists(fullPath))
                {
                    fileResult.Success = false;
                    fileResult.Data = $"File '{filename}' does not exist.";
                    multiFileResponse.Success = false;
                }
                else
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(fullPath);
                    string base64String = Convert.ToBase64String(fileBytes);
                    fileResult.Success = true;
                    fileResult.Data = base64String;
                    fileResult.filename = filename;

                }
            }
            catch (Exception ex)
            {
                fileResult.Success = false;
                fileResult.Data = $"An error occurred with file '{filename}': {ex.Message}";
                multiFileResponse.Success = false;
            }

            multiFileResponse.Result.Add(fileResult);
        }

        return multiFileResponse;
    }


}

