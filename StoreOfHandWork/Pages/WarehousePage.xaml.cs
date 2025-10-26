using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Pages
{
    public partial class WarehousePage : Page
    {
        private readonly ApplicationDbContext _context;
        private Order _selectedOrder;
        private const string SearchPlaceholder = "Поиск по номеру заказа";

        public WarehousePage()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            StatusFilter.SelectionChanged += (s, e) => LoadOrders();
            SearchBox.TextChanged += (s, e) => LoadOrders();

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                // Получаем все заказы для отладки
                var allOrders = _context.Orders.ToList();
             

                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Применяем фильтр по статусу сборки
                if (StatusFilter.SelectedIndex > 0)
                {
                    switch (StatusFilter.SelectedIndex)
                    {
                        case 1: // Не собраны
                            query = query.Where(o => !o.OrderItems.Any(oi => oi.IsCollected));
                            break;
                        case 2: // Частично собраны
                            query = query.Where(o => o.OrderItems.Any(oi => oi.IsCollected) && 
                                                   o.OrderItems.Any(oi => !oi.IsCollected));
                            break;
                        case 3: // Полностью собраны
                            query = query.Where(o => o.OrderItems.All(oi => oi.IsCollected));
                            break;
                    }
                }

                // Применяем поиск
                var searchText = SearchBox.Text?.Trim();
                if (!string.IsNullOrEmpty(searchText) && searchText != SearchPlaceholder)
                {
                    query = query.Where(o => o.OrderNumber.Contains(searchText));
                }

                var orders = query.ToList();

                // Добавляем статус сборки
                foreach (var order in orders)
                {
                    if (!order.OrderItems.Any(oi => oi.IsCollected))
                    {
                        order.CollectionStatus = "Не собран";
                    }
                    else if (order.OrderItems.All(oi => oi.IsCollected))
                    {
                        order.CollectionStatus = "Полностью собран";
                    }
                    else
                    {
                        var collectedCount = order.OrderItems.Count(oi => oi.IsCollected);
                        var totalCount = order.OrderItems.Count;
                        order.CollectionStatus = $"Собрано {collectedCount} из {totalCount}";
                    }
                }

                OrdersGrid.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}\n\nСтек: {ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == SearchPlaceholder)
            {
                SearchBox.Text = string.Empty;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = SearchPlaceholder;
            }
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder = OrdersGrid.SelectedItem as Order;
        }

        private void CollectOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                var collectionWindow = new OrderCollectionWindow(order);
                if (collectionWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
        }
    }
} 