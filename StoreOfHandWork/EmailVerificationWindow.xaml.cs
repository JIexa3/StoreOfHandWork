using System;
using System.Windows;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using StoreOfHandWork.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace StoreOfHandWork
{
    public partial class EmailVerificationWindow : Window
    {
        private readonly User _user;
        private readonly string _verificationCode;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public EmailVerificationWindow(User user)
        {
            InitializeComponent();
            _user = user;
            _context = App.Current.Services.GetRequiredService<ApplicationDbContext>();
            _emailService = App.Current.Services.GetRequiredService<IEmailService>();
            _verificationCode = EmailService.GenerateVerificationCode();
            SendVerificationCode();
        }

        private async void SendVerificationCode()
        {
            try
            {
                await _emailService.SendVerificationCodeAsync(_user.Email, _verificationCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке кода подтверждения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (VerificationCodeTextBox.Text == _verificationCode)
            {
                try
                {
                    var user = _context.Users.FirstOrDefault(u => u.Id == _user.Id);
                    if (user != null)
                    {
                        user.IsEmailVerified = true;
                        _context.SaveChanges();

                        MessageBox.Show("Email успешно подтвержден!", 
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        var loginWindow = new LoginWindow();
                        loginWindow.Show();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Пользователь не найден в базе данных",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Неверный код подтверждения. Попробуйте еще раз.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResendButton_Click(object sender, RoutedEventArgs e)
        {
            SendVerificationCode();
            MessageBox.Show("Новый код подтверждения отправлен на ваш email.", 
                "Отправлено", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
