using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private TextBox? _emailTextBox;
        private PasswordBox? _passwordBox;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _emailTextBox = FindName("EmailTextBox") as TextBox;
            _passwordBox = FindName("PasswordBox") as PasswordBox;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_emailTextBox == null || _passwordBox == null)
                {
                    MessageBox.Show("Ошибка инициализации элементов управления",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string email = _emailTextBox.Text?.Trim() ?? "";
                string password = _passwordBox.Password ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Пожалуйста, введите email и пароль",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = _context.Users.FirstOrDefault(u => 
                    u.Email.ToLower() == email.ToLower() && 
                    u.PasswordHash == password);

                if (user == null)
                {
                    MessageBox.Show("Неверный email или пароль",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.Status == "Заблокирован")
                {
                    MessageBox.Show("Ваш аккаунт заблокирован. Обратитесь к администратору.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Window mainWindow;
                if (user.Role == "Admin")
                {
                    mainWindow = new AdminWindow(user);
                }
                else
                {
                    mainWindow = new MainWindow(user);
                }

                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            Close();
        }
    }
}
