using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Core.Extensions;
using DKIM;

namespace Core.Utilities
{
    public static class Mail
    {
        public static DateTime LastEmailSent = DateTime.Now;
        public static void SendMeAnEmailEvery3mins(string subject, string body)
        {
            if (DateTime.Now.AddMinutes(3) > LastEmailSent)
            {
                SendMeAnEmail(subject, body);
                LastEmailSent = DateTime.Now;
            }
        }

        public static bool SendMeAnEmail(string subject, string body, string replyToName = null, string replyToEmail = null)
        {
            return SendEmailInternal("dzlotrg@gmail.com", subject, body, replyToName, replyToEmail);
        }

        public static bool SendEmail(string to, string subject, string body)
        {
            return SendEmailInternal(to, subject, body, null, null);
        }

        private static bool SendEmailInternal(string to, string subject, string body, string replyToName, string replyToEmail)
        {
            try
            {
                if (Debugger.IsAttached || Configuration.GetSMTPUserName().IsNullOrEmpty())
                {
                    HttpContext.Current.Response.Write(subject + " " + body);
                    return false;
                }

                var privateKey = PrivateKeySigner.Create(Configuration.GetDomainKey());

                using (var client = new SmtpClient())
                {
                    var message = new MailMessage();
                    message.To.Add(new MailAddress(to));

                    if (!replyToEmail.IsNullOrEmpty())
                    {
                        ActionExtensions.TryAction(() => message.ReplyToList.Add(new MailAddress(replyToEmail, replyToName)));
                    }

                    message.From = new MailAddress("do-not-reply@rssheap.com", "rssheap");
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    var domainKeySigner = new DomainKeySigner(privateKey, "rssheap.com", "abc", new string[] { "From", "To", "Subject" });
                    message.DomainKeySign(domainKeySigner);

                    var dkimSigner = new DkimSigner(privateKey, "rssheap.com", "abc", new string[] { "From", "To", "Subject" });
                    message.DkimSign(dkimSigner);

                    client.Host = "192.99.232.179";
                    client.Port = 25;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(Configuration.GetSMTPUserName(), Configuration.GetSMTPPassword());
                    client.EnableSsl = false;

                    client.Send(message);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void SendEmailWithDKIP(string subject, string body)
        {
            if (Configuration.GetSMTPUserName().IsNullOrEmpty()) return;

            var privateKey = PrivateKeySigner.Create(Configuration.GetDomainKey());

            var msg = new MailMessage();
            msg.To.Add(new MailAddress("dzlotrg@gmail.com"));
            msg.From = new MailAddress("do-not-reply@rssheap.com", "rssheap");
            msg.Subject = subject;
            msg.Body = body;

            var domainKeySigner = new DomainKeySigner(privateKey, "rssheap.com", "abc", new string[] { "From", "To", "Subject" });
            msg.DomainKeySign(domainKeySigner);

            var dkimSigner = new DkimSigner(privateKey, "rssheap.com", "abc", new string[] { "From", "To", "Subject" });
            msg.DkimSign(dkimSigner);

            var client = new SmtpClient
            {
                Host = "192.99.232.179",
                Port = 25,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Configuration.GetSMTPUserName(), Configuration.GetSMTPPassword()),
                EnableSsl = false
            };

            try
            {
                client.Send(msg);
            }
            catch
            {
                string bp = string.Empty;
            }
        }
    }
}
