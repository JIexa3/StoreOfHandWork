using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Models;
using StoreOfHandWork.Controls;
using StoreOfHandWork.Data;

namespace StoreOfHandWork
{
    public partial class ProfileWindow : Window, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _context;
        private string _email;
        private UserStatisticsControl _statisticsControl;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Email)));
            }
        }

        public ProfileWindow(User user)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _currentUser = _context.Users.Find(user.Id); // Получаем свежую копию из базы
            
            DataContext = this;
            MenuListBox.SelectedIndex = 0;
            
            LoadUserData();
            LoadOrderHistory();
            InitializeStatistics();
        }

        private void LoadUserData()
        {
            NameTextBox.Text = _currentUser.Name;
            EmailTextBox.Text = _currentUser.Email;
            Email = _currentUser.Email; // Для отображения в боковом меню
            PhoneTextBox.Text = _currentUser.Phone;
            AddressTextBox.Text = _currentUser.Address;
        }

        private void InitializeStatistics()
        {
            // Создаем контрол статистики только один раз
            if (_statisticsControl == null)
            {
                _statisticsControl = new UserStatisticsControl(_currentUser.Id);
                StatisticsControl.Content = _statisticsControl;
            }
        }

        private void RefreshStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (_statisticsControl != null)
            {
                _statisticsControl.RefreshData();
            }
        }

        private void LoadOrderHistory()
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.PickupPoint)
                .Where(o => o.UserId == _currentUser.Id);

            // Применяем фильтр по статусу, если выбран конкретный статус
            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                var selectedStatus = StatusFilterComboBox.SelectedItem as ComboBoxItem;
                if (selectedStatus != null)
                {
                    var statusText = selectedStatus.Content.ToString();
                    OrderStatus status;
                    if (Enum.TryParse(statusText, out status))
                    {
                        query = query.Where(o => o.Status == status);
                    }
                }
            }

            var orders = query.OrderByDescending(o => o.OrderDate).ToList();
            OrdersListView.ItemsSource = orders;
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadOrderHistory();
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            var selectedItem = e.AddedItems[0] as ListBoxItem;
            if (selectedItem == null) return;

            // Скрываем все панели
            PersonalInfoPanel.Visibility = Visibility.Collapsed;
            OrderHistoryPanel.Visibility = Visibility.Collapsed;
            SecurityPanel.Visibility = Visibility.Collapsed;
            StatisticsPanel.Visibility = Visibility.Collapsed;

            // Показываем выбранную панель
            if (selectedItem == PersonalInfoItem)
            {
                PersonalInfoPanel.Visibility = Visibility.Visible;
            }
            else if (selectedItem == OrderHistoryItem)
            {
                OrderHistoryPanel.Visibility = Visibility.Visible;
                LoadOrderHistory(); // Перезагружаем историю заказов при переключении на вкладку
            }
            else if (selectedItem == SecurityItem)
            {
                SecurityPanel.Visibility = Visibility.Visible;
            }
            else if (selectedItem == StatisticsItem)
            {
                StatisticsPanel.Visibility = Visibility.Visible;
            }
            else if (selectedItem == ReturnRequestsItem)
            {
                // Открываем окно возврата товаров
                OpenReturnRequestsWindow();
                
                // Восстанавливаем предыдущий выбор в меню
                if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ListBoxItem previousItem)
                {
                    MenuListBox.SelectedItem = previousItem;
                }
                else
                {
                    MenuListBox.SelectedItem = PersonalInfoItem;
                }
            }
        }
        
        private void OpenReturnRequestsWindow()
        {
            try
            {
                // Создаем окно для возврата товаров
                var returnWindow = new Window
                {
                    Title = "Возврат товаров",
                    Width = 1000,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                
                // Создаем фрейм и передаем в него страницу с текущим пользователем
                var frame = new Frame();
                var returnPage = new Pages.ReturnRequestsPage(_currentUser);
                frame.Content = returnPage;
                
                // Устанавливаем содержимое окна
                returnWindow.Content = frame;
                
                // Открываем окно
                returnWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии страницы возврата товаров: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Email обязателен для заполнения", 
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentUser.Name = NameTextBox.Text;
                _currentUser.Email = EmailTextBox.Text;
                Email = EmailTextBox.Text; // Обновляем email в боковом меню
                _currentUser.Phone = PhoneTextBox.Text;
                _currentUser.Address = AddressTextBox.Text;

                _context.SaveChanges();
                MessageBox.Show("Данные успешно сохранены", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сброс предыдущей ошибки
                PasswordErrorText.Text = string.Empty;
                PasswordErrorText.Visibility = Visibility.Collapsed;

                if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
                {
                    ShowPasswordError("Введите текущий пароль");
                    return;
                }

                // Проверяем текущий пароль
                if (CurrentPasswordBox.Password != _currentUser.PasswordHash)
                {
                    ShowPasswordError("Неверный текущий пароль");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    ShowPasswordError("Введите новый пароль");
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    ShowPasswordError("Пароли не совпадают");
                    return;
                }

                // Проверяем требования к новому паролю
                if (!ValidatePassword(NewPasswordBox.Password))
                {
                    return;
                }

                // Обновляем пароль пользователя в отдельной транзакции
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _currentUser.PasswordHash = NewPasswordBox.Password;
                        _context.SaveChanges();
                        transaction.Commit();

                        MessageBox.Show("Пароль успешно изменен", "Успех", 
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Очищаем поля после успешной смены пароля
                        CurrentPasswordBox.Clear();
                        NewPasswordBox.Clear();
                        ConfirmPasswordBox.Clear();
                        PasswordErrorText.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPasswordError($"Ошибка при смене пароля: {ex.Message}");
            }
        }

        private bool ValidatePassword(string password)
        {
            if (password.Length < 6)
            {
                ShowPasswordError("Пароль должен содержать минимум 6 символов");
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                ShowPasswordError("Пароль должен содержать хотя бы одну заглавную букву");
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                ShowPasswordError("Пароль должен содержать хотя бы одну строчную букву");
                return false;
            }

            if (!password.Any(ch => "!@#$%^&*(),.?\":{}|<>".Contains(ch)))
            {
                ShowPasswordError("Пароль должен содержать хотя бы один специальный символ (!@#$%^&*(),.?\":{}|<>)");
                return false;
            }

            return true;
        }

        private void ShowPasswordError(string message)
        {
            PasswordErrorText.Text = message;
            PasswordErrorText.Visibility = Visibility.Visible;
        }

        private void ViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                var detailsWindow = new OrderDetailsWindow(order);
                detailsWindow.ShowDialog();
            }
        }
    }
}
