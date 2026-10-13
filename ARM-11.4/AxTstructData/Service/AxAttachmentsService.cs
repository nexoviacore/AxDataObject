using ARMCommon.Helpers;
using ARMCommon.Interface;
using System.Data;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AxTstructData
{
    public class AxAttachmentsService : IAxAttachmentsService
    {
        private Utils _common;
        private IAxAttachmentsRepository _attachmentRepo;
        private readonly IARMLogTraceService _traceService;
        private string _axpImagePath = string.Empty;
        private string _axpImageServer = string.Empty;
        private string _axpAttachmentPath = string.Empty;
        private int _axpAttachmentSize = 0;

        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp", ".jpg", ".jpeg", ".pjpeg", ".png", ".gif",
            ".doc", ".docx", ".xml", ".xlsx", ".ppt", ".pdf",
            ".txt", ".xls", ".pwd", ".tiff", ".tif", ".dib",
            ".jfif", ".heic", ".xlxs", ".csv", ".html", ".css",
            ".js", ".htm", ".eml", ".msg", ".pptx"
        };

        private static readonly HashSet<string> _blockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".bat", ".reg", ".com", ".php", ".py", ".svg"
        };

        private static readonly HashSet<string> _disallowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-msdownload", "application/vnd.microsoft.portable-executable",
            "application/x-sh", "application/x-bat", "application/java-archive",
            "application/zip", "application/javascript", "text/javascript",
            "application/x-python", "application/x-ruby", "text/x-php",
            "application/x-rar-compressed", "application/x-7z-compressed",
            "application/x-iso9660-image", "application/x-virtualbox-ova"
        };

        private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".gif", ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".heic", ".jfif"
        };

        public AxAttachmentsService(Utils common, IAxAttachmentsRepository attachmentRepo, IARMLogTraceService traceService)
        {
            _common = common;
            _attachmentRepo = attachmentRepo;
            _traceService = traceService;
        }

        // ZIP magic bytes are shared by DOCX/XLSX/PPTX; these extensions are valid Office Open XML.
        private static readonly HashSet<string> _zipBasedOfficeExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".docx", ".xlsx", ".pptx", ".xlxs"
        };

        private static string DetectMimeType(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return "application/octet-stream";

            // EXE / DLL â MZ header
            if (bytes[0] == 0x4D && bytes[1] == 0x5A)
                return "application/x-msdownload";

            // PDF
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return "application/pdf";

            // PNG
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                return "image/png";

            // JPEG
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";

            // GIF
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
                return "image/gif";

            // BMP
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
                return "image/bmp";

            // TIFF (little-endian and big-endian)
            if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
                return "image/tiff";

            // ZIP / DOCX / XLSX / PPTX / JAR â caller must cross-check extension
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
                return "application/zip";

            // RAR
            if (bytes.Length >= 6 &&
                bytes[0] == 0x52 && bytes[1] == 0x61 && bytes[2] == 0x72 && bytes[3] == 0x21 &&
                bytes[4] == 0x1A && bytes[5] == 0x07)
                return "application/x-rar-compressed";

            // 7-Zip
            if (bytes.Length >= 6 &&
                bytes[0] == 0x37 && bytes[1] == 0x7A && bytes[2] == 0xBC && bytes[3] == 0xAF &&
                bytes[4] == 0x27 && bytes[5] == 0x1C)
                return "application/x-7z-compressed";

            // OLE2 compound document â old .doc / .xls / .ppt (all permitted)
            if (bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0)
                return "application/msword";

            // Text-based dangerous formats â inspect first 512 bytes
            string head = Encoding.UTF8.GetString(bytes, 0, Math.Min(512, bytes.Length));

            if (head.TrimStart().StartsWith("<?php", StringComparison.OrdinalIgnoreCase))
                return "text/x-php";

            if (head.StartsWith("#!"))
                return "application/x-sh";

            if (head.TrimStart().StartsWith("@echo", StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(head, @"^\s*(REM |::|ECHO |SET |IF |FOR |GOTO )", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline))
                return "application/x-bat";

            return "application/octet-stream";
        }

        private bool ValidateFile(string fileName, string base64Data)
        {
            if (_axpAttachmentSize <= 0)
            {
                var attachmetSizeInMB = _common._appSettingsHelper.GetFileConfig(_common._appName).AxAttachmentSize;
                Int32.TryParse(attachmetSizeInMB, out _axpAttachmentSize);

                if (_axpAttachmentSize == null || _axpAttachmentSize <= 0)
                    _axpAttachmentSize = 1;
            }

            string ext = Path.GetExtension(fileName)?.ToLower();

            if (string.IsNullOrEmpty(fileName))
            {
                _traceService.LogMessage("File name is required.");
                throw new Exception("File name is required.");
            }

            byte[] fileBytes;
            try { fileBytes = Convert.FromBase64String(base64Data); }
            catch
            {
                _traceService.LogMessage("File data is not valid base64.");
                throw new Exception("File data is not valid base64.");
            }

            if (string.IsNullOrEmpty(fileName)) 
            {
                _traceService.LogMessage("File name is required.");
                throw new Exception("File name is required.");
            }

            if (!IsFileTypeValid(fileName))
            {
                throw new Exception("Invalid File.");
            }
            else if (fileBytes.LongLength > _axpAttachmentSize * 1024 * 1024)
            {
                _traceService.LogMessage($"File could not be uploaded. Filesize is more than {_axpAttachmentSize} MB.");
                throw new Exception($"File could not be uploaded. Filesize is more than {_axpAttachmentSize} MB.");
            }
            else if(fileName.Length > 260)
            {
                _traceService.LogMessage($"Too many characters in the filename");
                throw new Exception($"Too many characters in the filename");
            }
            else if (_blockedExtensions.Contains(ext) || !_allowedExtensions.Contains(ext))
            {
                _traceService.LogMessage($"Invalid File Extension.");
                throw new Exception($"Invalid File Extension.");
            }
            else if (fileName.Contains(","))
            {
                _traceService.LogMessage($"Character ',' restricted in uploading File Names. Please rename and upload.");
                throw new Exception($"Character ',' restricted in uploading File Names. Please rename and upload.");
            }                      

            //TODO -> Mimetype detection.

            //string detectedMime = DetectMimeType(fileBytes);

            //// ZIP magic bytes are also used by Office Open XML â allow those extensions through
            //if (detectedMime == "application/zip" && _zipBasedOfficeExtensions.Contains(ext))
            //    return (true, string.Empty);

            //if (_disallowedMimeTypes.Contains(detectedMime))
            //    return (false, $"File content was identified as '{detectedMime}', which is not allowed.");

            return true;
        }

        public AxAttachmentResponse GetImage(AxAttachments attachment, bool saveImageInDB = false)
        {
            var response = new AxAttachmentResponse();
            if (saveImageInDB)
                response = GetImageFromDB(attachment);
            else
                response = GetImageFromFolder(attachment);

            return response;
        }

        public AxAttachmentResponse GetImageFromDB(AxAttachments attachment)
        {
            var response = new AxAttachmentResponse();
            var fileData = _attachmentRepo.GetImageFromDB(attachment).GetAwaiter().GetResult();
            if (fileData != null && fileData.data != null && fileData.data.Rows.Count > 0)
            {
                using (var ms = new MemoryStream())
                {
                    response.FileType = fileData.data.Rows[0]["ftype"]?.ToString() ?? "";

                    foreach (DataRow row in fileData.data.Rows)
                    {
                        byte[] chunk = (byte[])row["img"];
                        ms.Write(chunk, 0, chunk.Length);
                    }

                    var fileBytes = ms.ToArray();
                    response.FileData = Convert.ToBase64String(fileBytes);
                    response.DataFormat = "BASE64";
                    response.AttachmentType = "IMAGE";
                    response.FileName = attachment.FieldName + "." + response.FileType.ToLower();
                }
            }
            else
            {
                response = null;
            }

            return response;
        }

        public AxAttachmentResponse GetImageFromFolder(AxAttachments attachment)
        {
            var response = new AxAttachmentResponse();

            if (_common._globalVars.ContainsKey("AxpImagePath"))
                _axpImagePath = _common._globalVars["AxpImagePath"];

            if (_common._globalVars.ContainsKey("Axpimageserver"))
                _axpImageServer = _common._globalVars["Axpimageserver"];

            bool isFolder = false;
            if (!string.IsNullOrEmpty(_axpImagePath) && (_axpImagePath.Contains(":") || _axpImagePath.StartsWith(@"\\")))
                isFolder = true;

            if (!isFolder)
                _axpImagePath = _axpImageServer + @"\" + _axpImagePath;

            if (attachment.RecordId != 0)
            {
                string filePath = string.Empty;

                if (!string.IsNullOrEmpty(_axpImagePath))
                {
                    filePath = Path.Combine(_axpImagePath, attachment.TransId, attachment.FieldName);
                }
                else
                    return null;

                GetAuthentication(filePath, () => {
                    DirectoryInfo di = new DirectoryInfo(filePath);

                    if (di.Exists)
                    {
                        FileInfo[] files = di.GetFiles();

                        foreach (var file in files)
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);

                            if (fileNameWithoutExt == attachment.RecordId.ToString())
                            {
                                byte[] fileBytes = File.ReadAllBytes(file.FullName);
                                string base64 = Convert.ToBase64String(fileBytes);
                                response = new AxAttachmentResponse();
                                response.FileType = file.Extension.Trim('.');
                                response.DataFormat = "BASE64";
                                response.AttachmentType = attachment.AttachmentType;
                                response.FileName = attachment.FieldName + "." + response.FileType.ToLower();
                                response.FileData = base64;

                                break;
                            }
                        }
                    }
                });
            }

            return response;
        }

        public List<AxAttachmentResponse> GetAttachments(AxAttachments attachment)
        {
            var attachmentsResult = new List<AxAttachmentResponse>();
            if (_common._globalVars.ContainsKey("AxpAttachmentPath"))
                _axpAttachmentPath = _common._globalVars["AxpAttachmentPath"];
            if (string.IsNullOrEmpty(_axpAttachmentPath))
            {
                attachmentsResult = GetAttachmentsFromDB(attachment);
            }
            else
            {
                attachmentsResult = GetAttachmentsFromFolder(attachment);
            }

            return attachmentsResult;
        }

        public List<AxAttachmentResponse> GetAttachmentsFromDB(AxAttachments attachment)
        {
            var attachmentsResult = new List<AxAttachmentResponse>();

            var fileData = _attachmentRepo.GetHeaderAttachmentsFromDB(attachment).GetAwaiter().GetResult();
            if (fileData != null && fileData.data != null && fileData.data.Rows.Count > 0)
            {
                using (var ms = new MemoryStream())
                {
                    foreach (DataRow row in fileData.data.Rows)
                    {
                        byte[] chunk = (byte[])row["template"];
                        ms.Write(chunk, 0, chunk.Length);

                        var response = new AxAttachmentResponse();
                        var fileBytes = ms.ToArray();
                        response.FileType = Path.GetExtension(row["filename"].ToString()).TrimStart('.');
                        response.FileData = Convert.ToBase64String(fileBytes);
                        response.DataFormat = "BASE64";
                        response.AttachmentType = "HEADERATTACHMENT";
                        response.FileName = row["filename"].ToString();

                        attachmentsResult.Add(response);
                    }
                }
            }

            return attachmentsResult;
        }

        public List<AxAttachmentResponse> GetAttachmentsFromFolder(AxAttachments attachment)
        {
            var attachmentsResult = new List<AxAttachmentResponse>();

            if (attachment.RecordId != 0)
            {
                string filePath = string.Empty;

                if (!string.IsNullOrEmpty(_axpAttachmentPath))
                {
                    filePath = Path.Combine(_axpAttachmentPath, attachment.TransId);
                }
                else
                    return null;

                string authenticationStatus = string.Empty;

                GetAuthentication(filePath, () => {
                    DirectoryInfo di = new DirectoryInfo(filePath);

                    if (di.Exists)
                    {
                        FileInfo[] files = di.GetFiles();

                        foreach (var file in files)
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);

                            if (fileNameWithoutExt.StartsWith(attachment.RecordId.ToString() + "-"))
                            {
                                byte[] fileBytes = File.ReadAllBytes(file.FullName);
                                string base64 = Convert.ToBase64String(fileBytes);

                                var response = new AxAttachmentResponse();
                                response.FileType = file.Extension.Trim('.');
                                response.DataFormat = "BASE64";
                                response.AttachmentType = attachment.AttachmentType;
                                response.FileName = file.Name.Replace(attachment.RecordId.ToString() + "-", "");
                                response.FileData = base64;

                                attachmentsResult.Add(response);
                            }
                        }
                    }
                });
            }

            return attachmentsResult;
        }

        public List<AxAttachmentResponse> GetAxpFilesFromFolder(AxAttachments attachment)
        {
            var attachmentsResult = new List<AxAttachmentResponse>();

            if (string.IsNullOrEmpty(attachment.FilePath))
            {
                if (_common._globalVars.ContainsKey("AxpImagePath"))
                    _axpImagePath = _common._globalVars["AxpImagePath"];

                if (_common._globalVars.ContainsKey("Axpimageserver"))
                    _axpImageServer = _common._globalVars["Axpimageserver"];

                bool isFolder = false;
                if (!string.IsNullOrEmpty(_axpImagePath) && (_axpImagePath.Contains(":") || _axpImagePath.StartsWith(@"\\")))
                    isFolder = true;

                if (!isFolder)
                    _axpImagePath = _axpImageServer + @"\" + _axpImagePath;

                if (!string.IsNullOrEmpty(_axpImagePath))
                    attachment.FilePath = _axpImagePath;
                else
                {
                    var fileUploadConfig = _common._appSettingsHelper.GetFileConfig(_common._appName);
                    attachment.FilePath = fileUploadConfig.FileUploadPath;
                }
            }

            if (attachment.RecordId != 0)
            {
                if (string.IsNullOrEmpty(attachment.FilePath))
                    return null;

                string authenticationStatus = string.Empty;

                GetAuthentication(attachment.FilePath, () => {
                    DirectoryInfo di = new DirectoryInfo(attachment.FilePath);

                    if (di.Exists)
                    {
                        foreach (var fileName in attachment.FileName.Split(','))
                        {
                            var filePath = Path.Combine(attachment.FilePath, fileName);
                            if (File.Exists(filePath))
                            {
                                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                                string base64 = Convert.ToBase64String(fileBytes);

                                var response = new AxAttachmentResponse
                                {
                                    FileType = Path.GetExtension(fileName).Trim('.'),
                                    DataFormat = "BASE64",
                                    AttachmentType = attachment.AttachmentType,
                                    FileName = fileName,
                                    FileData = base64
                                };

                                attachmentsResult.Add(response);
                            }
                        }
                    }
                });
            }

            return attachmentsResult;
        }

        public bool SaveImages(AxAttachments attachment, bool saveImageInDB = false, bool deleteExistingImage = false)
        {
            if (saveImageInDB)
                return WriteImagesToDB(attachment, deleteExistingImage);
            else
                return WriteImagesToFolder(attachment);
        }

        public bool WriteImagesToDB(AxAttachments attachment, bool deleteExistingImage = false)
        {
            if (attachment.RecordId == 0 || (!attachment.Delete && string.IsNullOrEmpty(attachment.FileData)))
                return false;           

            if (!attachment.Delete)
            {
                var valid = ValidateFile(attachment.FileName, attachment.FileData);
                if (!valid) return false;

                string ext = Path.GetExtension(attachment.FileName)?.ToLower();
                if (!_imageExtensions.Contains(ext)) return false;
            }

            byte[] fileBytes = Convert.FromBase64String(attachment.FileData);
            string fileType = Path.GetExtension(attachment.FileName)?.Trim('.') ?? string.Empty;

            if (deleteExistingImage || attachment.Delete)
            {
                var deleteResult = _attachmentRepo.DeleteImagesFromDB(attachment).GetAwaiter().GetResult();
                if (deleteResult == null)
                    return false;
            }

            if (attachment.Delete)
                return true;

            var writeResult = _attachmentRepo.WriteImagesToDB(attachment, fileBytes, 1, fileType).GetAwaiter().GetResult();
            return writeResult != null;
        }

        public bool WriteImagesToFolder(AxAttachments attachment)
        {
            if (attachment.RecordId == 0)
                return false;

            if (!attachment.Delete && string.IsNullOrEmpty(attachment.FileData))
                return false;

            if (!attachment.Delete)
            {
                var valid = ValidateFile(attachment.FileName, attachment.FileData);
                if (!valid) return false;

                string ext = Path.GetExtension(attachment.FileName)?.ToLower();
                if (!_imageExtensions.Contains(ext)) return false;
            }

            if (_common._globalVars.ContainsKey("AxpImagePath"))
                _axpImagePath = _common._globalVars["AxpImagePath"];

            if (_common._globalVars.ContainsKey("Axpimageserver"))
                _axpImageServer = _common._globalVars["Axpimageserver"];

            bool isFolder = false;
            if (!string.IsNullOrEmpty(_axpImagePath) && (_axpImagePath.Contains(":") || _axpImagePath.StartsWith(@"\\")))
                isFolder = true;

            if (!isFolder)
                _axpImagePath = _axpImageServer + @"\" + _axpImagePath;

            if (string.IsNullOrEmpty(_axpImagePath))
                return false;

            string authenticationStatus = string.Empty;

            bool result = false;
            GetAuthentication(_axpImagePath, () => {
                string dirPath = Path.Combine(_axpImagePath, attachment.TransId, attachment.FieldName);
                DirectoryInfo di = new DirectoryInfo(dirPath);

                if (!di.Exists)
                    di.Create();

                foreach (var existingFile in di.GetFiles())
                {
                    if (Path.GetFileNameWithoutExtension(existingFile.Name) == attachment.RecordId.ToString())
                        existingFile.Delete();
                }

                if (attachment.Delete)
                {
                    result = true;
                }
                else
                {
                    string fileType = Path.GetExtension(attachment.FileName)?.Trim('.') ?? string.Empty;
                    string targetFileName = attachment.RecordId.ToString() + (string.IsNullOrEmpty(fileType) ? "" : "." + fileType.ToLower());
                    string targetFilePath = Path.Combine(dirPath, targetFileName);

                    byte[] fileBytes = Convert.FromBase64String(attachment.FileData);
                    File.WriteAllBytes(targetFilePath, fileBytes);

                    result = true;
                }
            });

            return result;
        }

        public bool SaveAttachments(AxAttachments attachment)
        {
            if (_common._globalVars.ContainsKey("AxpAttachmentPath"))
                _axpAttachmentPath = _common._globalVars["AxpAttachmentPath"];
            if (string.IsNullOrEmpty(_axpAttachmentPath))
                return WriteAttachmentsToDB(attachment);
            else
                return WriteAttachmentsToFolder(attachment);
        }

        public bool WriteAttachmentsToFolder(AxAttachments attachment)
        {
            if (attachment.RecordId == 0)
                return false;

            if (!attachment.Delete)
            {
                var valid = ValidateFile(attachment.FileName, attachment.FileData);
                if (!valid) return false;
            }

            if (_common._globalVars.ContainsKey("AxpAttachmentPath"))
                _axpAttachmentPath = _common._globalVars["AxpAttachmentPath"];

            if (string.IsNullOrEmpty(_axpAttachmentPath))
                return false;

            string authenticationStatus = string.Empty;

            bool result = false;
            GetAuthentication(_axpAttachmentPath, () => {
                string dirPath = Path.Combine(_axpAttachmentPath, attachment.TransId);
                DirectoryInfo di = new DirectoryInfo(dirPath);

                if (!di.Exists)
                    di.Create();

                if ((!attachment.Delete && string.IsNullOrEmpty(attachment.FileData)) || string.IsNullOrEmpty(attachment.FileName))
                    result =  false;

                string recordPrefix = attachment.RecordId.ToString() + "-";
                if (attachment.Delete)
                {
                    var filePath = Path.Combine(dirPath, recordPrefix + attachment.FileName);

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    result = true;
                }
                else
                {
                    string savedFileName = recordPrefix + attachment.FileName;
                    string targetFilePath = Path.Combine(dirPath, savedFileName);

                    byte[] fileBytes = Convert.FromBase64String(attachment.FileData);
                    File.WriteAllBytes(targetFilePath, fileBytes);

                    result = true;
                }
            });

            return result;
        }

        public bool WriteAttachmentsToDB(AxAttachments attachment)
        {
            if (attachment.RecordId == 0 || (!attachment.Delete && string.IsNullOrEmpty(attachment.FileData)))
                return false;

            if (!attachment.Delete)
            {
                var valid = ValidateFile(attachment.FileName, attachment.FileData);
                if (!valid) return false;
            }

            byte[] fileBytes = Convert.FromBase64String(attachment.FileData);
            string fileType = Path.GetExtension(attachment.FileName)?.Trim('.') ?? string.Empty;

            if (attachment.Delete)
            {
                var deleteResult = _attachmentRepo.DeleteHeaderAttachmentsFromDB(attachment).GetAwaiter().GetResult();
                if (deleteResult == null)
                    return false;

                return true;
            }
            
            var writeResult = _attachmentRepo.InsertHeaderAttachmentsToDB(attachment, fileBytes, 1, fileType).GetAwaiter().GetResult();
            return writeResult != null;
        }

        public bool WriteAxpFilesToFolder(AxAttachments attachment)
        {
            if (attachment.RecordId == 0)
                return false;

            if (!attachment.Delete)
            {
                var valid = ValidateFile(attachment.FileName, attachment.FileData);
                if (!valid) return false;
            }

            if (string.IsNullOrEmpty(attachment.FilePath))
            {
                if (_common._globalVars.ContainsKey("AxpImagePath"))
                    _axpImagePath = _common._globalVars["AxpImagePath"];

                if (_common._globalVars.ContainsKey("Axpimageserver"))
                    _axpImageServer = _common._globalVars["Axpimageserver"];

                bool isFolder = false;
                if (!string.IsNullOrEmpty(_axpImagePath) && (_axpImagePath.Contains(":") || _axpImagePath.StartsWith(@"\\")))
                    isFolder = true;

                if (!isFolder)
                    _axpImagePath = _axpImageServer + @"\" + _axpImagePath;

                if (!string.IsNullOrEmpty(_axpImagePath))
                    attachment.FilePath = _axpImagePath;
                else
                {
                    var fileUploadConfig = _common._appSettingsHelper.GetFileConfig(_common._appName);
                    attachment.FilePath = fileUploadConfig.FileUploadPath;
                }
            }

            if (string.IsNullOrEmpty(attachment.FilePath))
                return false;

            string authenticationStatus = string.Empty;

            bool result = false;
            GetAuthentication(attachment.FilePath, () => {

                DirectoryInfo di = new DirectoryInfo(attachment.FilePath);

                if (!di.Exists)
                    di.Create();

                if ((!attachment.Delete && string.IsNullOrEmpty(attachment.FileData)) || string.IsNullOrEmpty(attachment.FileName))
                    result = false;
                else
                {
                    string targetFilePath = Path.Combine(attachment.FilePath, attachment.FileName);

                    // Overwrite any existing file with the same name
                    if (File.Exists(targetFilePath))
                        File.Delete(targetFilePath);

                    if (attachment.Delete)
                        result = true;
                    else
                    {
                        byte[] fileBytes = Convert.FromBase64String(attachment.FileData);
                        File.WriteAllBytes(targetFilePath, fileBytes);
                        result = true;
                    }
                }
            });

            return result;
        }

        public void GetAuthentication(string uncPath, Action action)
        {
            string user = _common._appSettingsHelper.GetFileConfig(_common._appName).FileServerMapUsername;
            string pwd = _common._appSettingsHelper.GetFileConfig(_common._appName).FileServerMapPwd;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pwd))
            {
                action();
                return;
            }
            string sharePath = GetSharePath(uncPath);
            var credentials = new NetworkCredential(user, pwd);
            using (new NetworkConnection(sharePath, credentials))
            {
                action();
            }
        }

        private string GetSharePath(string fullPath)
        {
            if (!fullPath.StartsWith(@"\\"))
                throw new Exception("Invalid UNC path");

            var parts = fullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                throw new Exception("Invalid UNC format");
            return $"\\\\{parts[0]}\\{parts[1]}";
        }

        public bool IsFileTypeValid(string fileName)
        {
            bool isValid = true;
            fileName = Path.GetFileName(fileName);
            string[] fileExtnStr = fileName.Split('.');

            //if (file.ContentType == "application/x-msdownload") //TODO - After mimetype
            {
                string fileExtn = fileExtnStr[fileExtnStr.Length - 1].ToString().ToLower();
                if (fileExtn == "dll" || fileExtn == "exe" || fileExtn == "bat" || fileExtn == "reg" || fileExtn == "com")
                {
                    isValid = false;
                }
            }
            return isValid;
        }
    }
}
