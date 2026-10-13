using System.ComponentModel.DataAnnotations;

namespace ARMCommon.Model
{
    public class AxpertUsers
    {
        [Key]
        public int ID { get; set; }
        public string AppName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string? MobileNo { get; set; }
        public bool IsActive { get; set; }




    }
}
