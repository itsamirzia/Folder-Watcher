using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace DipWatcherAndLogger
{
    static class Mailer
    {
        /// <summary>
        /// Send an SMTP email
        /// </summary>
        /// <param name="body"></param>
        static public void SendMail(string body)
        {
            string HostAdd = ConfigurationManager.AppSettings["host"].ToString();
            string FromEmailid = ConfigurationManager.AppSettings["Forwarder"].ToString();
            string FromPassword = ConfigurationManager.AppSettings["ForwarderPassword"].ToString();

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(FromEmailid); //From Email Id  
            mailMessage.Subject = ConfigurationManager.AppSettings["EmailSubject"].ToString(); //Subject of Email  
            mailMessage.Body = body; //body or message of Email  
            mailMessage.IsBodyHtml = true;
            string toEmails = ConfigurationManager.AppSettings["EmailsTo"].ToString();
            string cc = ConfigurationManager.AppSettings["EmailsCC"].ToString();
            string bcc = ConfigurationManager.AppSettings["EmailsBCC"].ToString();
            string port = ConfigurationManager.AppSettings["SMTPPort"].ToString();

            if (toEmails.Trim() != string.Empty)
            {
                string[] ToMuliId = toEmails.Split(',');
                foreach (string ToEMailId in ToMuliId)
                {
                    mailMessage.To.Add(new MailAddress(ToEMailId)); //adding multiple TO Email Id  
                }
            }

            if (cc.Trim() != string.Empty)
            {
                string[] CCId = cc.Split(',');

                foreach (string CCEmail in CCId)
                {
                    mailMessage.CC.Add(new MailAddress(CCEmail)); //Adding Multiple CC email Id  
                }
            }

            if (bcc.Trim() != string.Empty)
            {
                string[] bccid = bcc.Split(',');

                foreach (string bccEmailId in bccid)
                {
                    mailMessage.Bcc.Add(new MailAddress(bccEmailId)); //Adding Multiple BCC email Id  
                }
            }
            SmtpClient smtp = new SmtpClient();   
            smtp.Host = HostAdd;
            smtp.EnableSsl = false;
            NetworkCredential NetworkCred = new NetworkCredential();
            NetworkCred.UserName = FromEmailid;
            NetworkCred.Password = FromPassword;
            smtp.Credentials = NetworkCred;
            smtp.Port = Convert.ToInt32(port);
            try
            {
                smtp.Send(mailMessage); //sending Email  
            }
            catch(Exception ex)
            {
                Logger.AddtoWritingQueue.Enqueue("Error : Email send failed - " + ex.Message + " at " + System.DateTime.Now.ToString("MMddyyyy HH: mm:ss"));
                Thread.Sleep(50);
            }
        }
    }
}
