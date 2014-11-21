using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class EMail
    {
        public static bool SendEMail(string toAddress, string subject, string body)
        {
            bool result = false;
            if (
                !string.IsNullOrEmpty(Properties.Settings.Default.smtpServer)
                && !string.IsNullOrEmpty(Properties.Settings.Default.smtpAccountName)
                && !string.IsNullOrEmpty(Properties.Settings.Default.smtpAccountPassword)
                && Properties.Settings.Default.smtpPort!=0
                )
            try
            {
                using (MailMessage mm = new MailMessage())
                {
                    mm.From = new MailAddress("no-reply@globalcaching.eu", "globalcaching.eu");
                    mm.To.Add(new MailAddress(toAddress, toAddress));
                    mm.Subject = subject;
                    mm.Body = body;
                    mm.IsBodyHtml = false;

                    NetworkCredential cred = new NetworkCredential(Properties.Settings.Default.smtpAccountName, Properties.Settings.Default.smtpAccountPassword);
                    using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(Properties.Settings.Default.smtpServer))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.EnableSsl = Properties.Settings.Default.smtpUseSSL;
                        smtp.Credentials = cred;
                        smtp.Port = Properties.Settings.Default.smtpPort;

                        smtp.Send(mm);
                    }
                }
                result = true;
            }
            catch
            {
            }
            return result;
        }
    }
}
