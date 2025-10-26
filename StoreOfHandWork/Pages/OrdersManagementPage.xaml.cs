using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Models;
using StoreOfHandWork.Services;
using System.Windows.Threading;
using StoreOfHandWork.Data;

namespace StoreOfHandWork.Pages
{
    public partial class OrdersManagementPage : Page
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private ICollectionView _ordersView;
        private readonly Dictionary<int, DispatcherTimer> _orderTimers;

        public OrdersManagementPage(ApplicationDbContext context, IEmailService emailService)
        {
            InitializeComponent();
            _context = context;
            _emailService = emailService;
            _orderTimers = new Dictionary<int, DispatcherTimer>();
            Loaded += OrdersManagementPage_Loaded;
            Unloaded += OrdersManagementPage_Unloaded;
        }

        private void OrdersManagementPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Очищаем все таймеры при выгрузке страницы
            foreach (var timer in _orderTimers.Values)
            {
                timer.Stop();
            }
            _orderTimers.Clear();
        }

        private void OrdersManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации страницы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsNoTracking()
                    .ToList();

                OrdersDataGrid.ItemsSource = orders;
                _ordersView = CollectionViewSource.GetDefaultView(orders);
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OrdersDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var order = e.Row.Item as Order;
                if (order != null)
                {
                    try
                    {
                        var existingOrder = await _context.Orders
                            .Include(o => o.User)
                            .Include(o => o.OrderItems)
                            .FirstOrDefaultAsync(o => o.Id == order.Id);

                        if (existingOrder != null)
                        {
                            var oldStatus = existingOrder.Status;
                            
                            // Проверяем, можно ли изменить статус
                            if (oldStatus == OrderStatus.Новый && order.Status != OrderStatus.Новый)
                            {
                                // Проверяем, собраны ли все товары
                                var orderItems = await _context.OrderItems
                                    .Where(oi => oi.OrderId == order.Id)
                                    .ToListAsync();

                                if (!orderItems.All(oi => oi.IsCollected))
                                {
                                    MessageBox.Show("Нельзя изменить статус заказа, пока не собраны все товары на складе.",
                                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    
                                    // Отменяем изменение статуса
                                    LoadOrders();
                                    return;
                                }
                            }

                            existingOrder.Status = order.Status;

                            // Отправляем уведомление при любом изменении статуса
                            if (oldStatus != order.Status && existingOrder.User.EmailNotificationsEnabled)
                            {
                                try 
                                {
                                    await _emailService.SendOrderStatusChangeEmailAsync(
                                        existingOrder.User.Email,
                                        existingOrder.OrderNumber,
                                        order.Status.ToString());
                                }
                                catch (Exception emailEx)
                                {
                                    MessageBox.Show($"Ошибка при отправке уведомления: {emailEx.Message}\n" +
                                                  "Статус заказа был изменен, но уведомление не отправлено.",
                                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }

                            // Если статус изменился на "В обработке"
                            if (oldStatus != OrderStatus.ВОбработке && order.Status == OrderStatus.ВОбработке)
                            {
                                // Создаем таймер для автоматического обновления статуса через час
                                var timer = new DispatcherTimer
                                {
                                    Interval = TimeSpan.FromHours(1)
                                };

                                timer.Tick += (s, args) =>
                                {
                                    UpdateOrderStatus(existingOrder.Id);
                                    timer.Stop();
                                    _orderTimers.Remove(existingOrder.Id);
                                };

                                if (_orderTimers.ContainsKey(existingOrder.Id))
                                {
                                    _orderTimers[existingOrder.Id].Stop();
                                }

                                _orderTimers[existingOrder.Id] = timer;
                                timer.Start();
                            }

                            _context.SaveChanges();
                            MessageBox.Show("Изменения сохранены успешно.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadOrders();
                    }
                }
            }
        }

        private void UpdateOrderStatus(int orderId)
        {
            try
            {
                var order = _context.Orders.Find(orderId);
                if (order != null && order.Status == OrderStatus.ВОбработке)
                {
                    order.Status = OrderStatus.Доставлен;
                    _context.SaveChanges();
                    
                    // Обновляем UI в основном потоке
                    Dispatcher.Invoke(() =>
                    {
                        LoadOrders();
                        MessageBox.Show($"Статус заказа №{orderId} автоматически обновлен на 'Выполнен'",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при автоматическом обновлении статуса заказа: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var order = button?.DataContext as Order;
            if (order != null)
            {
                try
                {
                    var existingOrder = await _context.Orders
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.Id == order.Id);

                    if (existingOrder != null)
                    {
                        var oldStatus = existingOrder.Status;
                        existingOrder.Status = order.Status;

                        // Отправляем уведомление при любом изменении статуса
                        if (oldStatus != order.Status && existingOrder.User.EmailNotificationsEnabled)
                        {
                            try 
                            {
                                await _emailService.SendOrderStatusChangeEmailAsync(
                                    existingOrder.User.Email,
                                    existingOrder.OrderNumber,
                                    order.Status.ToString());
                            }
                            catch (Exception emailEx)
                            {
                                MessageBox.Show($"Ошибка при отправке уведомления: {emailEx.Message}\n" +
                                              "Статус заказа был изменен, но уведомление не отправлено.",
                                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        // Если статус изменился на "В обработке"
                        if (oldStatus != OrderStatus.ВОбработке && order.Status == OrderStatus.ВОбработке)
                        {
                            // Создаем таймер для автоматического обновления статуса через час
                            var timer = new DispatcherTimer
                            {
                                Interval = TimeSpan.FromHours(1)
                            };

                            timer.Tick += (s, args) =>
                            {
                                UpdateOrderStatus(existingOrder.Id);
                                timer.Stop();
                                _orderTimers.Remove(existingOrder.Id);
                            };

                            if (_orderTimers.ContainsKey(existingOrder.Id))
                            {
                                _orderTimers[existingOrder.Id].Stop();
                            }

                            _orderTimers[existingOrder.Id] = timer;
                            timer.Start();
                        }

                        _context.SaveChanges();
                        MessageBox.Show("Изменения сохранены успешно.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadOrders();
                }
            }
        }

        private void CollectOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Order order)
            {
                try
                {
                    var collectionWindow = new OrderCollectionWindow(order);
                    collectionWindow.ShowDialog();
                    
                    // Обновляем список заказов после закрытия окна сборки
                    LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии окна сборки заказа: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateStatusBar()
        {
            var totalOrders = _ordersView.Cast<Order>().Count();
            var pendingOrders = _ordersView.Cast<Order>().Count(o => o.Status == OrderStatus.Новый);
            var processingOrders = _ordersView.Cast<Order>().Count(o => o.Status == OrderStatus.ВОбработке);
            var completedOrders = _ordersView.Cast<Order>().Count(o => o.Status == OrderStatus.Доставлен);

            StatusBar.Text = $"Всего заказов: {totalOrders} | " +
                           $"Новых: {pendingOrders} | " +
                           $"В обработке: {processingOrders} | " +
                           $"Доставлено: {completedOrders}";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOrder = OrdersDataGrid.SelectedItem as Order;
            if (selectedOrder != null)
            {
                var button = FindChangeStatusButton(selectedOrder);
                if (button != null)
                {
                    button.IsEnabled = true;
                }
            }
        }

        private Button FindChangeStatusButton(Order order)
        {
            var row = OrdersDataGrid.ItemContainerGenerator.ContainerFromItem(order) as DataGridRow;
            if (row != null)
            {
                var cell = OrdersDataGrid.Columns[OrdersDataGrid.Columns.Count - 1].GetCellContent(row);
                if (cell != null)
                {
                    var panel = cell.FindName("ChangeStatusButton") as Button;
                    return panel;
                }
            }
            return null;
        }
    }
}
