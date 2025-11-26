using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PhongNguyenPuppy_MVC.Services;

namespace PhongNguyenPuppy_MVC.Helpers
{
    public class MyEmailHelper
    {
        private readonly EmailSettings _emailSettings;
        private readonly IViewRenderService _viewRenderService;

        public MyEmailHelper(IOptions<EmailSettings> emailSettings, IViewRenderService viewRenderService)
        {
            _emailSettings = emailSettings.Value;
            _viewRenderService = viewRenderService;
        }

        public async Task SendMailAsync(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password),
                EnableSsl = true,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var message = new MailMessage(_emailSettings.FromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            try
            {
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Log lỗi ra console để debug (tốt hơn là dùng ILogger)
                Console.WriteLine($"SendMailAsync failed to {toEmail}: {ex.Message}");
                throw; // rethrow để caller có thể bắt và xử lý (đã bắt ở controller registration)
            }
        }
        public async Task SendTemplateAsync<TModel>(string viewPath, TModel model, string toEmail, string subject)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("toEmail is required", nameof(toEmail));

            // Render Razor view to HTML string
            string body = await _viewRenderService.RenderToStringAsync(viewPath, model);

            // Send rendered HTML
            await SendMailAsync(toEmail, subject, body);
        }
    }
}
