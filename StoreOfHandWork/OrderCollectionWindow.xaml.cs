using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using StoreOfHandWork.Services;
using Microsoft.Extensions.DependencyInjection;

namespace StoreOfHandWork
{
    public partial class OrderCollectionWindow : Window
    {
        private Order _order;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICartService _cartService;
        private bool _saveSuccessful;

        public OrderCollectionWindow(Order order)
        {
            InitializeComponent();
            _context = App.Current.Services.GetRequiredService<ApplicationDbContext>();
            _emailService = App.Current.Services.GetRequiredService<IEmailService>();
            _cartService = App.Current.Services.GetRequiredService<ICartService>();
            _saveSuccessful = false;
            
            LoadOrderData(order.Id);
        }

        private void LoadOrderData(int orderId)
        {
            // Загружаем заказ со всеми связанными данными
            _order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .First(o => o.Id == orderId);

            // Заполняем информацию о заказе
            OrderNumberText.Text = $"Заказ №{_order.OrderNumber}";
            OrderDateText.Text = $"от {_order.OrderDate:dd.MM.yyyy HH:mm}";

            // Заполняем таблицу товаров
            ItemsGrid.ItemsSource = _order.OrderItems;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, все ли товары собраны
                if (!_order.OrderItems.All(item => item.IsCollected))
                {
                    MessageBox.Show("Необходимо собрать все товары перед сохранением",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем наличие достаточного количества товаров с учетом корзин
                foreach (var orderItem in _order.OrderItems)
                {
                    var availableQuantity = await _cartService.GetAvailableQuantity(orderItem.ProductId);
                    if (availableQuantity < orderItem.Quantity)
                    {
                        MessageBox.Show($"Недостаточно товара '{orderItem.Product.Name}' на складе. " +
                            $"Требуется: {orderItem.Quantity}, Доступно: {availableQuantity} " +
                            $"(часть товаров находится в корзинах других пользователей)",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Уменьшаем количество товаров на складе
                        foreach (var orderItem in _order.OrderItems)
                        {
                            var product = await _context.Products.FindAsync(orderItem.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity -= orderItem.Quantity;
                                _context.Entry(product).State = EntityState.Modified;
                            }
                        }

                        // Обновляем статус заказа на "В обработке"
                        _order.Status = OrderStatus.ВОбработке;
                        _context.Entry(_order).State = EntityState.Modified;
                
                        await _context.SaveChangesAsync();

                        // Отправляем email до завершения транзакции
                        try
                        {
                            await _emailService.SendOrderStatusChangeEmailAsync(
                                _order.User.Email,
                                _order.OrderNumber,
                                "Заказ собран и готов к выдаче");
                        }
                        catch (Exception emailEx)
                        {
                            // Логируем ошибку отправки email, но продолжаем выполнение
                            MessageBox.Show($"Статус заказа обновлен, но возникла ошибка при отправке уведомления: {emailEx.Message}",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        // Завершаем транзакцию после всех операций
                        await transaction.CommitAsync();
                        DialogResult = true;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            DialogResult = _saveSuccessful;
        }
    }
} 