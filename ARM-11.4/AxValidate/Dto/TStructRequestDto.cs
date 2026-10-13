//using System.ComponentModel.DataAnnotations;


//namespace AxValidate.Dto
//{
//    public class TStructRequestDto
//    {
//        [Required(ErrorMessage = "Project is Required")]
//          public string Project { get; set; } = string.Empty;
//        public string Trace { get; set; } = string.Empty;
//        public string Transid { get; set; } = string.Empty;
//        public string Recordid { get; set; } = string.Empty;
//        public DataWrapper Data { get; set; } = new();

//    }


//    public class DataWrapper
//    {
//        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Data { get; set; } = [];
//    }
//}

using System.ComponentModel.DataAnnotations;

namespace AxValidate.Dto
{
    public class TStructRequestDto
    {
        [Required(ErrorMessage = "ARMSessionId is Required")]
        public string ProjectName { get; set; } = string.Empty;

        public string ARMSessionId { get; set; } = string.Empty;


        public string Transid { get; set; } = string.Empty;


        public string Action { get; set; } = string.Empty;


        public string Keyfield { get; set; } = string.Empty;


        public string Keyvalue { get; set; } = string.Empty;

        public bool Trace { get; set; } = false;

        [Required(ErrorMessage = "Submitdata is Required")]
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Submitdata { get; set; } = new();
    }
}