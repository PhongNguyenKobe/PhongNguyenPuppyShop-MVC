using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PhongNguyenPuppy_MVC.Helpers;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class DichVuGuiEmail : IDichVuGuiEmail
    {
        private readonly EmailSettings _emailSettings;

        public DichVuGuiEmail(IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
        }

        public void Gui(string emailNhan, string tieuDe, string noiDung)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail),
                Subject = tieuDe,
                Body = noiDung,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(emailNhan);

            smtpClient.Send(mailMessage);
        }
    }
}
