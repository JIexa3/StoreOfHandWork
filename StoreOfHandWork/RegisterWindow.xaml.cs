using System;
using System.Windows;
using StoreOfHandWork.Models;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using System.Windows.Input;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using System.Text;

namespace StoreOfHandWork
{
    public partial class RegisterWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly Regex _emailRegex = new Regex(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$");
        private readonly Regex _phoneRegex = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");

        public RegisterWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            // Настройка обработчиков событий
            EmailTextBox.TextChanged += (s, e) => ValidateEmail();
            PhoneTextBox.TextChanged += (s, e) => ValidatePhone();
            PhoneTextBox.LostFocus += (s, e) => FormatPhoneNumber();
            PhoneTextBox.TextChanged += PhoneTextBox_TextChanged;
        }

        private void EmailTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только буквы, цифры и специальные символы для email
            e.Handled = !IsValidEmailChar(e.Text);
        }

        private bool IsValidEmailChar(string text)
        {
            return text.All(c => char.IsLetterOrDigit(c) || c == '@' || c == '.' || c == '_' || c == '-');
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры для телефона
            e.Handled = !e.Text.All(char.IsDigit);
            if (!e.Handled)
            {
                string currentText = PhoneTextBox.Text;
                string digits = new string(currentText.Replace("+7", "").Where(char.IsDigit).ToArray());
                // Ограничиваем ввод 10 цифрами (не считая +7)
                if (digits.Length >= 10)
                {
                    e.Handled = true;
                }
            }
        }

        private void FormatPhoneNumber()
        {
            if (PhoneTextBox.Text.Length > 0)
            {
                // Убираем все нецифровые символы и +7 из начала номера
                string digits = new string(PhoneTextBox.Text.Replace("+7", "").Where(char.IsDigit).ToArray());
                
                // Форматируем номер только если есть цифры
                if (digits.Length > 0)
                {
                    StringBuilder formatted = new StringBuilder("+7");
                    
                    // Добавляем скобки только если есть минимум 3 цифры
                    if (digits.Length >= 3)
                    {
                        formatted.Append(" (").Append(digits.Substring(0, 3));
                        if (digits.Length > 3)
                        {
                            formatted.Append(") ");
                            formatted.Append(digits.Substring(3, Math.Min(3, digits.Length - 3)));
                            
                            if (digits.Length > 6)
                            {
                                formatted.Append("-");
                                formatted.Append(digits.Substring(6, Math.Min(2, digits.Length - 6)));
                                
                                if (digits.Length > 8)
                                {
                                    formatted.Append("-");
                                    formatted.Append(digits.Substring(8, Math.Min(2, digits.Length - 8)));
                                }
                            }
                        }
                    }
                    else
                    {
                        formatted.Append(" ").Append(digits);
                    }

                    PhoneTextBox.Text = formatted.ToString();
                    PhoneTextBox.SelectionStart = PhoneTextBox.Text.Length;
                }
            }
        }

        private void ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !_emailRegex.IsMatch(EmailTextBox.Text))
            {
                EmailTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                EmailTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
        }

        private void ValidatePhone()
        {
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text) || !_phoneRegex.IsMatch(PhoneTextBox.Text))
            {
                PhoneTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                PhoneTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация полей
                if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AddressTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Все поля обязательны для заполнения",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация email
                if (!_emailRegex.IsMatch(EmailTextBox.Text))
                {
                    MessageBox.Show("Введите корректный email адрес",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация телефона
                if (!_phoneRegex.IsMatch(PhoneTextBox.Text))
                {
                    MessageBox.Show("Введите корректный номер телефона",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, существует ли пользователь с таким email
                if (_context.Users.Any(u => u.Email.ToLower() == EmailTextBox.Text.ToLower()))
                {
                    MessageBox.Show("Пользователь с таким email уже существует",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем совпадение паролей
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Пароли не совпадают",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем нового пользователя
                var user = new User
                {
                    Name = NameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim().ToLower(),
                    Phone = PhoneTextBox.Text.Trim(),
                    Address = AddressTextBox.Text.Trim(),
                    PasswordHash = PasswordBox.Password,
                    Role = "User",
                    Status = "Активен",
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow,
                    IsEmailVerified = false,
                    EmailNotificationsEnabled = true,
                    SmsNotificationsEnabled = true
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Отправляем код подтверждения на email
                var verificationWindow = new EmailVerificationWindow(user);
                verificationWindow.Show();
                Close();
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Ошибка при сохранении в базу данных: {ex.InnerException?.Message ?? ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context.Dispose();
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!PhoneTextBox.Text.StartsWith("+7"))
            {
                PhoneTextBox.Text = "+7" + PhoneTextBox.Text.TrimStart('+', '7', ' ');
                PhoneTextBox.SelectionStart = PhoneTextBox.Text.Length;
            }
            FormatPhoneNumber();
        }
    }
}
