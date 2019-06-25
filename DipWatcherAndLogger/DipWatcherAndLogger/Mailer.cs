using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Mail;
using System.Net;

namespace DipWatcherAndLogger
{
    static class Mailer
    {
        static public void SendMail(string body)
        {

            string HostAdd = ConfigurationManager.AppSettings["host"].ToString();
            string FromEmailid = "mzia@pekininsurance.com";
            string Pass = "bar@22kati";

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(FromEmailid); //From Email Id  
            mailMessage.Subject = ConfigurationManager.AppSettings["EmailSubject"].ToString(); //Subject of Email  
            mailMessage.Body = body; //body or message of Email  
            mailMessage.IsBodyHtml = true;
            string toEmails = ConfigurationManager.AppSettings["EmailsTo"].ToString();
            string cc = ConfigurationManager.AppSettings["EmailsCC"].ToString();
            string bcc = ConfigurationManager.AppSettings["EmailBCC"].ToString();
            string port = ConfigurationManager.AppSettings["SMTPPort"].ToString();

            string[] ToMuliId = toEmails.Split(',');
            foreach (string ToEMailId in ToMuliId)
            {
                mailMessage.To.Add(new MailAddress(ToEMailId)); //adding multiple TO Email Id  
            }


            string[] CCId = cc.Split(',');

            foreach (string CCEmail in CCId)
            {
                mailMessage.CC.Add(new MailAddress(CCEmail)); //Adding Multiple CC email Id  
            }

            string[] bccid = bcc.Split(',');

            foreach (string bccEmailId in bccid)
            {
                mailMessage.Bcc.Add(new MailAddress(bccEmailId)); //Adding Multiple BCC email Id  
            }
            SmtpClient smtp = new SmtpClient();   
            smtp.Host = HostAdd;              

            //network and security related credentials  

            smtp.EnableSsl = false;
            NetworkCredential NetworkCred = new NetworkCredential();
            NetworkCred.UserName = mailMessage.From.Address;
            NetworkCred.Password = Pass;
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = NetworkCred;
            smtp.Port = Convert.ToInt32(port);
            smtp.Send(mailMessage); //sending Email  
        }
    }
}
