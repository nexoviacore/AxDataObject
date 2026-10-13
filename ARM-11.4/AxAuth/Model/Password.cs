namespace AxAuth
{
    public class ChangePasswordInput
    {
        public string AppName { get; set; }
        public string UserName { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string Seed { get; set; }
        public bool Trace { get; set; } = false;
    }

    public class ForgotPasswordInput
    {
        public string AppName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool Trace { get; set; } = false;
    }

    public class PasswordUserData
    {
        public string Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Active { get; set; }
    }
}
