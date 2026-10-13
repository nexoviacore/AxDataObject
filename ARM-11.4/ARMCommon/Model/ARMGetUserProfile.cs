namespace ARMCommon.Model
{
    public class ARMGetUserProfile
    {
        public string ARMSessionId { get; set; }
    }

    public class ARMUpdateUserProfile
    {
        public string? ARMSessionId { get; set; }
        public string email { get; set; }
        public string? mobileno { get; set; }
        public string? regid { get; set; }
        public string? otp { get; set; }

        public string? username { get; set; }
    }
}
