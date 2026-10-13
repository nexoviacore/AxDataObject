
using System;
using System.Collections.Generic;
using System.Xml;


namespace ARMCommon.Helpers
{
    public class AxPropsManager
    {
        public string UpdatedOn { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string AppVersion { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedUsername { get; set; }

        public string ImgFolder { get; set; }
        public EnvVars EnvironmentVars { get; set; }
        public Dictionary<string, string> Constants { get; set; } = new();
        public Dictionary<string, List<string>> Variables { get; set; } = new();
        //public MailSettings Mail { get; set; }
        public PasswordSettings Password { get; set; }

        public static AxPropsManager ParseFromXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var root = doc.DocumentElement;

            var config = new AxPropsManager
            {
                UpdatedOn = root.GetAttribute("updatedon"),
                Name = root.GetAttribute("name"),
                Caption = root.GetAttribute("caption"),
                AppVersion = root.GetAttribute("appverno"),
                UpdatedUsername = root.GetAttribute("updusername"),
                UpdatedBy = root.GetAttribute("updatedby"),
                EnvironmentVars = new EnvVars(),
                //Mail = new MailSettings(),
                Password = new PasswordSettings()
            };

            var props = root.SelectSingleNode("props");
            config.ImgFolder = props.SelectSingleNode("imgsfolder")?.InnerText;

            // Env Vars
            var env = props.SelectSingleNode("envvar");
            if (env != null)
            {
                config.EnvironmentVars.Currency = env.SelectSingleNode("currency")?.InnerText;
                config.EnvironmentVars.SiteNo = env.SelectSingleNode("siteno")?.InnerText;
                config.EnvironmentVars.Millions = env.SelectSingleNode("millions")?.InnerText;
                config.EnvironmentVars.PostAuto = env.SelectSingleNode("postauto")?.InnerText;
                config.EnvironmentVars.FinYrSt = env.SelectSingleNode("finyrst")?.InnerText;
                config.EnvironmentVars.FinYrEd = env.SelectSingleNode("finyred")?.InnerText;
                config.EnvironmentVars.OraErrFrom = env.SelectSingleNode("oraerrfrom")?.InnerText;
                config.EnvironmentVars.OraErrTo = env.SelectSingleNode("oraerrto")?.InnerText;
                config.EnvironmentVars.CurSep = env.SelectSingleNode("cursep")?.InnerText;
            }

            // App Vars - Constants and Variables
            var appvars = props.SelectSingleNode("appvars");
            if (appvars != null)
            {
                foreach (XmlNode node in appvars.ChildNodes)
                {
                    var name = node.Name.Trim();
                    var value = node.Attributes["value"]?.Value;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        config.Constants[name] = value;
                    }
                    else if (node.HasChildNodes)
                    {
                        List<string> lList = new List<string>();
                        var lNodes = node.SelectNodes("l");

                        if (lNodes != null)
                        {
                            foreach (XmlNode lNode in lNodes)
                            {
                                if (lNode != null && !string.IsNullOrEmpty(lNode.InnerText))
                                {
                                    lList.Add(lNode.InnerText);
                                }

                            }

                            config.Variables[name] = lList;
                        }
                    }
                }
            }

            //// Mail Settings
            //var mail = props.SelectSingleNode("mail");
            //if (mail != null)
            //{
            //    config.Mail.Host = mail.SelectSingleNode("host")?.InnerText;
            //    config.Mail.Port = mail.SelectSingleNode("port")?.InnerText;
            //    config.Mail.UserId = mail.SelectSingleNode("userid")?.InnerText;
            //    config.Mail.Password = mail.SelectSingleNode("password")?.InnerText;
            //}

            // Password Settings
            var pwd = props.SelectSingleNode("pwdsetting");
            if (pwd != null)
            {

                if (int.TryParse(pwd.SelectSingleNode("maxlogintry")?.InnerText, out int maxLoginTry))
                {
                    config.Password.MaxLoginTry = maxLoginTry;
                }

                if (int.TryParse(pwd.SelectSingleNode("pwdexpdays")?.InnerText, out int pwdExpDays))
                {
                    config.Password.PwdExpDays = pwdExpDays;
                }

                if (int.TryParse(pwd.SelectSingleNode("pwdalertdays")?.InnerText, out int pwdAlertDays))
                {
                    config.Password.PwdAlertDays = pwdAlertDays;
                }

                if (int.TryParse(pwd.SelectSingleNode("pwdminchars")?.InnerText, out int pwdMinChars))
                {
                    config.Password.PwdMinChars = pwdMinChars;
                }

                if (int.TryParse(pwd.SelectSingleNode("pwdprevnos")?.InnerText, out int pwdPrevNos))
                {
                    config.Password.PwdPrevNos = pwdPrevNos;
                }

                config.Password.IsPwdAlphaNum = pwd.SelectSingleNode("ispwdalphanum")?.InnerText ?? "F";
                config.Password.PwdAES = pwd.SelectSingleNode("pwdaes")?.InnerText ?? "F";
            }

            return config;
        }
    }

    public class EnvVars
    {
        public string Currency { get; set; } = "";
        public string SiteNo { get; set; } = "";
        public string Millions { get; set; } = "";
        public string PostAuto { get; set; } = "";
        public string FinYrSt { get; set; } = "";
        public string FinYrEd { get; set; } = "";
        public string OraErrFrom { get; set; } = "";
        public string OraErrTo { get; set; } = "";
        public string CurSep { get; set; } = "";
    }

    public class MailSettings
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class PasswordSettings
    {
        public int MaxLoginTry { get; set; } = 0;
        public int PwdExpDays { get; set; } = 0;
        public int PwdAlertDays { get; set; } = 0;
        public int PwdMinChars { get; set; } = 0;
        public int PwdPrevNos { get; set; } = 0;
        public string IsPwdAlphaNum { get; set; } = "F";
        public string PwdAES { get; set; } = "F";
    }

    public class AxpertProperties
    {
        public string? SmtpHost { get; set; }
        public short? SmtpPort { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPwd { get; set; }
        public long? AxpSiteNo { get; set; }
        public string? AmtInMillions { get; set; }
        public string? CurrSeperator { get; set; }
        public string? LastLogin { get; set; }
        public string? AutoGen { get; set; }
        public string? CustomFrom { get; set; }
        public string? CustomTo { get; set; }
        public byte? LoginAttempt { get; set; }
        public short? PwdExp { get; set; }
        public short? PwdChange { get; set; }
        public byte? PwdMinChar { get; set; }
        public byte? PwdReuse { get; set; }
        public string? PwdAlphaNum { get; set; }
        public string? PwdEncrypt { get; set; }
        public short? PwdMaxChar { get; set; }
        public byte? PwdCapChar { get; set; }
        public byte? PwdSmallChar { get; set; }
        public byte? PwdNumChar { get; set; }
        public byte? PwdSplChar { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
        public string? SmsContent { get; set; }
        public string? OtpAuth { get; set; }
        public string? OtpChars { get; set; }
        public string? OtpExpiry { get; set; }
    }

}
