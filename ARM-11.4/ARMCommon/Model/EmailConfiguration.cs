using MimeKit;
namespace ARMCommon.Model
{
    public class EmailConfiguration
    {
        public string From { get; set; }
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public List<MailboxAddress>? Bcc { get; set; }
        public List<MailboxAddress>? Cc { get; set; }
        public string Subject { get; set; }
        public string? Content { get; set; }

        public Message(IEnumerable<string> to, IEnumerable<string>? bcc, IEnumerable<string>? cc, string subject, string? content)
        {
            To = new List<MailboxAddress>();
            Bcc = new List<MailboxAddress>();
            Cc = new List<MailboxAddress>();

            To?.AddRange(to.Select(x => new MailboxAddress(x, x)));
            if (bcc != null)
            {
                Bcc.AddRange(bcc.Select(x => new MailboxAddress(x, x)));
            }
            if (cc != null)
            {
                Cc.AddRange(cc.Select(x => new MailboxAddress(x, x)));
            }
            Subject = subject;
            Content = content;
        }
    }
}
