using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace PhongNguyenPuppy_MVC.Helpers
{
    public class MyEmailHelper
    {
        private readonly EmailSettings _emailSettings;

        public MyEmailHelper(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendMailAsync(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password),
                EnableSsl = true
            };

            var message = new MailMessage(_emailSettings.FromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);// Gửi email bất đồng bộ,Gửi email mà không chặn luồng chính,Điều này giúp ứng dụng phản hồi nhanh hơn, đặc biệt khi gửi email mất vài giây
        }
    }
}
