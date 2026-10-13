using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ARMApp
    {

        public string AppTitle { get; set; }

        [Key]
        public string AppName { get; set; }

        public string? AppLogo { get; set; }
        [NotMapped]
        public IFormFile? AppLogoBase64 { get; set; }
        public string AppColor { get; set; }
        public string AxpertScriptsUrl { get; set; }
        public string AxpertWebUrl { get; set; }
        public string? AxpertAppName { get; set; }
        public string DataBase { get; set; }
        public string? DBVersion { get; set; }

        public string? Sqlversion { get; set; }
        public string ConnectionName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string? RedisIP { get; set; }
        public string? RedisPassword { get; set; }

        public bool IsCitizenUsers { get; set; }
        public bool IsGeoFencing { get; set; }
        public string? ForceLoginDays { get; set; }
        public bool IsGeoTagging { get; set; }
        public bool EnableFingerPrint { get; set; }
        public bool EnablefacialRecognition { get; set; }

        public bool ForceLogin { get; set; }

        public string? PrivateKey { get; set; }
        public DateTime? modifiedon { get; set; }
        public string? AppDomain { get; set; }

    }

    public class App
    {
        public string ID { get; set; }
        public string Name { get; set; }

    }

    public class ARMModel
    {
        public string From { get; set; }
        public string SmtpServer { get; set; }
        public string Portno { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }

        public string UserTo { get; set; }
        public string UserFrom { get; set; }

        public string Message { get; set; }
        public string MobileNo { get; set; }

        public string AuthCode { get; set; }
        public string Admin { get; set; }

        public string? loginPassword { get; set; }
        public string AdminEmailId { get; set; }

        public int KeyInterval { get; set; }

        public string PrivateKey { get; set; }


    }


}
