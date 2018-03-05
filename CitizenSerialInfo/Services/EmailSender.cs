using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Microsoft.Extensions.Options;
using CitizenSerialInfo.Models.ViewModels;

namespace CitizenSerialInfo.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public EmailSettings _emailSettings { get; }

        public string SendEmail(string email, string subject, string message)
        {
            string error = "";

            var cl = new SmtpClient(_emailSettings.PrimaryDomain, _emailSettings.PrimaryPort);

            var cred = new NetworkCredential(_emailSettings.UsernameEmail, _emailSettings.UsernamePassword);


            cl.EnableSsl = Convert.ToBoolean(true);
            cl.DeliveryMethod = SmtpDeliveryMethod.Network;
            cl.UseDefaultCredentials = false;
            cl.Credentials = cred;

            String htmlMessage = message;

            MailMessage mail = new MailMessage { IsBodyHtml = true };
            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlMessage, null, "text/html");

            //Add view to the Email Message
            mail.AlternateViews.Add(htmlView);

            mail.From = new MailAddress("test@test.md", "");

            //set the "to" email address
            mail.To.Add(email);

            //set the Email subject
            mail.Subject = subject;

            cl.Timeout = 20000;
            try
            {
                cl.Send(mail);

            }
            catch (Exception ex)
            {
                error=ex.StackTrace;
            }

            return error;
        }
    }
}
