using System.Windows;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using StoreOfHandWork.Helpers;
using System.Linq;

namespace StoreOfHandWork
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly User _currentUser;

        public ChangePasswordWindow(User currentUser)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _currentUser = currentUser;
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmNewPassword = ConfirmNewPasswordBox.Password;

            // Проверка текущего пароля
            if (_currentUser.PasswordHash != currentPassword)
            {
                ErrorTextBlock.Text = "Неверный текущий пароль";
                return;
            }

            // Проверка совпадения паролей
            if (newPassword != confirmNewPassword)
            {
                ErrorTextBlock.Text = "Новые пароли не совпадают";
                return;
            }

            // Валидация нового пароля
            var (isValid, errorMessage) = PasswordValidator.ValidatePassword(newPassword);
            if (!isValid)
            {
                ErrorTextBlock.Text = errorMessage;
                return;
            }

            // Обновление пароля
            _currentUser.PasswordHash = newPassword;
            _context.SaveChanges();

            MessageBox.Show("Пароль успешно изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
