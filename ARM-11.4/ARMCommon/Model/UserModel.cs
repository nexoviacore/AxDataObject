using System.ComponentModel.DataAnnotations;

namespace ARMCommon.Model
{
    public class UserModel
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
        public string? Email { get; set; }
    }
}
