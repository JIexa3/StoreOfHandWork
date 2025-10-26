using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Models;
using StoreOfHandWork.Data;

namespace StoreOfHandWork.Pages
{
    public partial class UsersManagementPage : Page
    {
        private ApplicationDbContext _context;
        private ICollectionView _usersView;

        public List<string> UserRoles { get; } = new List<string>
        {
            "Admin",
            "User"
        };

        public List<string> UserStatuses { get; } = new List<string>
        {
            "Активен",
            "Заблокирован"
        };

        public UsersManagementPage()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += UsersManagementPage_Loaded;
        }

        private void UsersManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _context = new ApplicationDbContext();

                // Инициализация фильтров
                if (RoleFilter != null)
                {
                    RoleFilter.SelectedIndex = 0;
                }
                if (StatusFilter != null)
                {
                    StatusFilter.SelectedIndex = 0;
                }

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации страницы: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                if (_context == null)
                {
                    _context = new ApplicationDbContext();
                }

                var users = _context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.CartItems)
                    .ToList();

                // Применяем фильтры, если они выбраны
                if (RoleFilter?.SelectedItem is ComboBoxItem selectedRole && 
                    selectedRole.Content.ToString() != "Все роли")
                {
                    users = users.Where(u => u.Role == selectedRole.Content.ToString()).ToList();
                }

                if (StatusFilter?.SelectedItem is ComboBoxItem selectedStatus && 
                    selectedStatus.Content.ToString() != "Все статусы")
                {
                    users = users.Where(u => u.Status == selectedStatus.Content.ToString()).ToList();
                }

                // Устанавливаем источник данных для DataGrid
                if (UsersDataGrid != null)
                {
                    UsersDataGrid.ItemsSource = users;
                    _usersView = CollectionViewSource.GetDefaultView(users);
                    if (_usersView != null)
                    {
                        _usersView.Filter = UserFilter;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(SearchBox?.Text))
                return true;

            if (item is User user)
            {
                var searchText = SearchBox.Text.ToLower();
                return user.Email?.ToLower().Contains(searchText) == true ||
                       user.Name?.ToLower().Contains(searchText) == true ||
                       user.Phone?.ToLower().Contains(searchText) == true;
            }

            return false;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _usersView?.Refresh();
        }

        private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUsers();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUsers();
        }

        private void UsersDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var user = e.Row.Item as User;
                if (user != null)
                {
                    try
                    {
                        var existingUser = _context.Users.Find(user.Id);
                        if (existingUser != null)
                        {
                            existingUser.Email = user.Email;
                            existingUser.Name = user.Name;
                            existingUser.Phone = user.Phone;
                            existingUser.Role = user.Role;
                            existingUser.Status = user.Status;

                            _context.SaveChanges();
                            MessageBox.Show("Изменения сохранены успешно.", "Успех", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", 
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadUsers(); // Перезагружаем данные в случае ошибки
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.DataContext as User;
            if (user != null)
            {
                try
                {
                    // Загружаем пользователя со всеми связанными данными
                    var existingUser = _context.Users
                        .Include(u => u.Orders)
                        .Include(u => u.CartItems)
                        .FirstOrDefault(u => u.Id == user.Id);

                    if (existingUser != null)
                    {
                        // Обновляем только необходимые поля
                        existingUser.Status = user.Status;
                        existingUser.Role = user.Role;
                        existingUser.Name = user.Name;
                        existingUser.Email = user.Email;
                        existingUser.Phone = user.Phone;

                        _context.SaveChanges();
                        MessageBox.Show("Изменения сохранены успешно.", "Успех", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // Перезагружаем список для обновления отображения
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadUsers(); // Перезагружаем данные в случае ошибки
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.DataContext as User;
            if (user != null)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пользователя {user.Email}? Это действие также удалит все заказы и товары в корзине пользователя.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Загружаем пользователя со всеми связанными данными
                        var existingUser = _context.Users
                            .Include(u => u.Orders)
                                .ThenInclude(o => o.OrderItems)
                            .Include(u => u.CartItems)
                            .FirstOrDefault(u => u.Id == user.Id);

                        if (existingUser != null)
                        {
                            _context.Users.Remove(existingUser);
                            _context.SaveChanges();
                            
                            LoadUsers();
                            MessageBox.Show("Пользователь успешно удален.", "Успех", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", 
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadUsers(); // Перезагружаем данные в случае ошибки
                    }
                }
            }
        }

        private void ViewUserOrders_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                try
                {
                    var userWithOrders = _context.Users
                        .Include(u => u.Orders)
                        .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                        .FirstOrDefault(u => u.Id == user.Id);

                    if (userWithOrders != null)
                    {
                        var details = new System.Text.StringBuilder();
                        details.AppendLine($"Заказы пользователя: {user.Email}");
                        details.AppendLine($"Всего заказов: {userWithOrders.Orders.Count}");
                        details.AppendLine();

                        foreach (var order in userWithOrders.Orders.OrderByDescending(o => o.OrderDate))
                        {
                            details.AppendLine($"Заказ №{order.OrderNumber}");
                            details.AppendLine($"Дата: {order.OrderDate:dd.MM.yyyy HH:mm}");
                            details.AppendLine($"Статус: {order.Status}");
                            details.AppendLine($"Сумма: {order.TotalAmount:C}");
                            details.AppendLine("Товары:");
                            foreach (var item in order.OrderItems)
                            {
                                details.AppendLine($"- {item.Product.Name}: {item.Quantity} шт. x {item.Price:C} = {(item.Quantity * item.Price):C}");
                            }
                            details.AppendLine();
                        }

                        MessageBox.Show(details.ToString(), "История заказов", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке заказов пользователя: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
