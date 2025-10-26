using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using StoreOfHandWork.Models;
using StoreOfHandWork.Data;

namespace StoreOfHandWork
{
    public partial class OrderManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private Order _selectedOrder;

        public OrderManagementWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            LoadOrders();

            StatusFilter.SelectionChanged += (s, e) => LoadOrders();
            SearchBox.TextChanged += (s, e) => LoadOrders();
        }

        private void LoadOrders()
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems.Select(oi => oi.Product))
                .AsQueryable();

            // Применяем фильтр по статусу
            if (StatusFilter.SelectedIndex > 0)
            {
                var status = (OrderStatus)(StatusFilter.SelectedIndex - 1);
                query = query.Where(o => o.Status == status);
            }

            // Применяем поиск
            var searchText = SearchBox.Text?.Trim();
            if (!string.IsNullOrEmpty(searchText) && searchText != "Поиск по номеру заказа или трек-номеру")
            {
                query = query.Where(o => o.OrderNumber.Contains(searchText) || 
                                       o.TrackingNumber.Contains(searchText));
            }

            OrdersGrid.ItemsSource = query.OrderByDescending(o => o.OrderDate).ToList();
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder = OrdersGrid.SelectedItem as Order;
            if (_selectedOrder != null)
            {
                UpdateOrderDetails();
            }
        }

        private void UpdateOrderDetails()
        {
            OrderNumberText.Text = $"Номер заказа: {_selectedOrder.OrderNumber}";
            TrackingNumberText.Text = $"Трек номер: {_selectedOrder.TrackingNumber}";
            OrderDateText.Text = $"Дата заказа: {_selectedOrder.OrderDate:dd.MM.yyyy HH:mm}";
            StatusText.Text = $"Статус: {_selectedOrder.Status}";
            CustomerText.Text = $"Покупатель: {_selectedOrder.User.Name}";
            AddressText.Text = $"Адрес доставки: {_selectedOrder.ShippingAddress}";
            
            if (!string.IsNullOrEmpty(_selectedOrder.PickupAddress))
            {
                AddressText.Text += $"\nАдрес выдачи: {_selectedOrder.PickupAddress}";
            }

            // Загружаем товары заказа с актуальными данными
            var orderItems = _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == _selectedOrder.Id)
                .ToList();

            OrderItemsListBox.ItemsSource = orderItems;

            // Проверяем, все ли товары собраны
            var allItemsCollected = orderItems.All(oi => oi.IsCollected);
            if (allItemsCollected && _selectedOrder.Status == OrderStatus.Новый)
            {
                MessageBox.Show("Все товары собраны. Можно менять статус заказа на 'В обработке'.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ChangeStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            // Проверяем, можно ли менять статус
            if (_selectedOrder.Status == OrderStatus.Новый)
            {
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == _selectedOrder.Id)
                    .ToList();

                if (!orderItems.All(oi => oi.IsCollected))
                {
                    MessageBox.Show("Нельзя изменить статус заказа, пока не собраны все товары.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var statusWindow = new ChangeStatusWindow(_selectedOrder.Status);
            if (statusWindow.ShowDialog() == true)
            {
                _selectedOrder.Status = statusWindow.SelectedStatus;
                
                // Если заказ доставлен, запросим адрес выдачи
                if (_selectedOrder.Status == OrderStatus.Доставлен && 
                    string.IsNullOrEmpty(_selectedOrder.PickupAddress))
                {
                    SetPickupAddressButton_Click(sender, e);
                }

                _context.SaveChanges();
                LoadOrders();
                UpdateOrderDetails();
            }
        }

        private void SetPickupAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            var dialog = new InputDialog("Укажите адрес выдачи заказа", 
                                       _selectedOrder.PickupAddress ?? "");
            if (dialog.ShowDialog() == true)
            {
                _selectedOrder.PickupAddress = dialog.ResponseText;
                _context.SaveChanges();
                UpdateOrderDetails();
            }
        }

        private void PrintOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Здесь можно добавить логику печати заказа
                MessageBox.Show("Печать заказа...");
            }
        }

        private void OrderItemCollectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is OrderItem orderItem)
            {
                try
                {
                    // Обновляем статус сборки товара
                    var item = _context.OrderItems.Find(orderItem.Id);
                    if (item != null)
                    {
                        item.IsCollected = checkBox.IsChecked ?? false;
                        _context.SaveChanges();

                        // Обновляем информацию о заказе
                        UpdateOrderDetails();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении статуса сборки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }

    public class InputDialog : Window
    {
        private TextBox responseTextBox;

        public string ResponseText
        {
            get { return responseTextBox.Text; }
            set { responseTextBox.Text = value; }
        }

        public InputDialog(string question, string defaultAnswer = "")
        {
            Title = "Ввод данных";
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var questionText = new TextBlock
            {
                Text = question,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(questionText, 0);
            grid.Children.Add(questionText);

            responseTextBox = new TextBox
            {
                Margin = new Thickness(10),
                Text = defaultAnswer
            };
            Grid.SetRow(responseTextBox, 1);
            grid.Children.Add(responseTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okButton.Click += (s, e) =>
            {
                DialogResult = true;
                Close();
            };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 75,
                Height = 25
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            buttonPanel.Children.Add(cancelButton);
        }
    }
} 