using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Services
{
    public interface IEmailService
    {
        Task SendReturnStatusUpdateEmailAsync(ReturnRequest returnRequest);
        Task SendVerificationCodeAsync(string toEmail, string verificationCode);
        Task SendOrderStatusChangeEmailAsync(string recipientEmail, string orderNumber, string newStatus);
    }

    public class EmailService : IEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["Email:SmtpServer"];
            _smtpPort = int.Parse(configuration["Email:SmtpPort"]);
            _smtpUsername = configuration["Email:Username"];
            _smtpPassword = configuration["Email:Password"];
            _fromEmail = configuration["Email:FromEmail"];
        }

        public async Task SendVerificationCodeAsync(string toEmail, string verificationCode)
        {
            var subject = "Подтверждение регистрации";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Добро пожаловать в МАГАЗИН ТОВАРОВ РУЧНОЙ РАБОТЫ!</h2>
                    <p>Для завершения регистрации введите следующий код подтверждения:</p>
                    <h3 style='color: #6B4CE6; font-size: 24px; text-align: center; padding: 10px; background-color: #f5f5f5; border-radius: 5px;'>{verificationCode}</h3>
                    <p>Если вы не регистрировались на нашем сайте, просто проигнорируйте это письмо.</p>
                    <p>С уважением,<br>Команда магазина StoreOfHandWork</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOrderStatusChangeEmailAsync(string recipientEmail, string orderNumber, string newStatus)
        {
            var subject = $"Изменение статуса заказа №{orderNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Уведомление о статусе заказа</h2>
                    <p>Уважаемый клиент!</p>
                    <p>Статус вашего заказа №{orderNumber} был изменен на ""{newStatus}"".</p>
                    <p>С уважением,<br>Команда магазина StoreOfHandWork</p>
                </body>
                </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        public async Task SendReturnStatusUpdateEmailAsync(ReturnRequest returnRequest)
        {
            var subject = $"Обновление статуса заявки на возврат №{returnRequest.Id}";
            var body = GenerateEmailBody(returnRequest);
            await SendEmailAsync(returnRequest.User.Email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);
                await client.SendMailAsync(message);
            }
        }

        private string GenerateEmailBody(ReturnRequest returnRequest)
        {
            var statusText = GetStatusText(returnRequest.Status);
            var productName = returnRequest.OrderItem?.Product?.Name ?? "товар";
            
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Уведомление о статусе возврата</h2>
                    <p>Уважаемый клиент!</p>
                    <p>Статус вашей заявки на возврат №{returnRequest.Id} был обновлен.</p>
                    <p><strong>Новый статус:</strong> {statusText}</p>
                    <p><strong>Товар:</strong> {productName}</p>
                    <p><strong>Тип возврата:</strong> {(returnRequest.Type == ReturnType.Возврат ? "Возврат денежных средств" : "Обмен товара")}</p>
                    {GetAdditionalInstructions(returnRequest.Status)}
                    <p>С уважением,<br>Команда магазина StoreOfHandWork</p>
                </body>
                </html>";
        }

        private string GetStatusText(ReturnStatus status)
        {
            return status switch
            {
                ReturnStatus.ЗаявкаОтправлена => "Заявка отправлена",
                ReturnStatus.Одобрено => "Заявка одобрена",
                ReturnStatus.Отклонено => "Заявка отклонена",
                ReturnStatus.ТоварПолучен => "Товар получен",
                ReturnStatus.ВозвратЗавершен => "Возврат средств выполнен",
                ReturnStatus.ОбменЗавершен => "Обмен оформлен",
                ReturnStatus.Отменено => "Заявка отменена",
                _ => status.ToString()
            };
        }

        private string GetAdditionalInstructions(ReturnStatus status)
        {
            return status switch
            {
                ReturnStatus.Одобрено => @"
                    <div style='background-color: #f5f5f5; padding: 15px; margin: 10px 0;'>
                        <h3>Инструкции по возврату:</h3>
                        <ol>
                            <li>Упакуйте товар в оригинальную или подходящую упаковку</li>
                            <li>Приложите копию заявки на возврат</li>
                            <li>Доставьте товар в пункт выдачи</li>
                        </ol>
                    </div>",
                ReturnStatus.ВозвратЗавершен => @"
                    <div style='background-color: #f5f5f5; padding: 15px; margin: 10px 0;'>
                        <p>Возврат денежных средств будет выполнен в течение 3-5 рабочих дней.</p>
                    </div>",
                _ => string.Empty
            };
        }

        public static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
