using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Models;

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

        /// <summary>
        /// Gửi email tới nhiều người nhận (async)
        /// </summary>
        public async Task GuiEmailAsync(List<string> danhSachEmail, string tieuDe, string noiDung)
        {
            if (danhSachEmail == null || danhSachEmail.Count == 0)
                throw new ArgumentException("Danh sách email không được rỗng");

            if (string.IsNullOrEmpty(tieuDe) || string.IsNullOrEmpty(noiDung))
                throw new ArgumentException("Tiêu đề và nội dung không được để trống");

            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password);
                smtpClient.EnableSsl = true;
                smtpClient.Timeout = 20000;

                // Gửi email tới từng người một để tránh lỗi
                var failedEmails = new List<string>();

                foreach (var email in danhSachEmail)
                {
                    try
                    {
                        using (var mailMessage = new MailMessage(_emailSettings.FromEmail, email))
                        {
                            mailMessage.Subject = tieuDe;
                            mailMessage.Body = noiDung;
                            mailMessage.IsBodyHtml = true;

                            await smtpClient.SendMailAsync(mailMessage);

                            // Delay nhỏ để tránh bị rate limit
                            await Task.Delay(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi gửi email tới {email}: {ex.Message}");
                        failedEmails.Add(email);
                    }
                }

                // Nếu có email gửi thất bại, ném exception
                if (failedEmails.Count > 0)
                {
                    var failedCount = failedEmails.Count;
                    var successCount = danhSachEmail.Count - failedCount;
                    throw new Exception(
                        $"Gửi email thành công: {successCount}/{danhSachEmail.Count}. " +
                        $"Email gửi thất bại: {string.Join(", ", failedEmails.Take(5))}" +
                        (failedEmails.Count > 5 ? $" và {failedEmails.Count - 5} email khác" : "")
                    );
                }
            }
        }
    }
}