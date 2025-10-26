using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork
{
    public partial class CartWindow : Window
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _context;
        private readonly MainWindow _mainWindow;
        private ObservableCollection<CartItem> _cartItems;

        public CartWindow(User user, MainWindow mainWindow)
        {
            InitializeComponent();
            _currentUser = user;
            _mainWindow = mainWindow;
            _context = new ApplicationDbContext();

            LoadCartItems();
        }

        private void LoadCartItems()
        {
            try
            {
                var cartItems = _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.User)
                    .Where(ci => ci.UserId == _currentUser.Id)
                    .ToList();

                _cartItems = new ObservableCollection<CartItem>(cartItems);
                CartItemsListView.ItemsSource = _cartItems;
                UpdateTotalAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке корзины: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotalAmount()
        {
            var total = _cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
            TotalAmountTextBlock.Text = $"{total:N2} ₽";
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is CartItem cartItem)
            {
                if (int.TryParse(textBox.Text, out int quantity))
                {
                    if (quantity <= 0)
                    {
                        textBox.Text = "1";
                        return;
                    }

                    try
                    {
                        // Проверяем наличие товара на складе
                        var product = _context.Products.Find(cartItem.ProductId);
                        if (product != null && quantity > product.StockQuantity)
                        {
                            MessageBox.Show($"Невозможно установить количество {quantity}. В наличии: {product.StockQuantity} шт.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            textBox.Text = product.StockQuantity.ToString();
                            return;
                        }

                        // Создаем новый контекст для обновления
                        using (var updateContext = new ApplicationDbContext())
                        {
                            var itemToUpdate = updateContext.CartItems.Find(cartItem.Id);
                            if (itemToUpdate != null)
                            {
                                itemToUpdate.Quantity = quantity;
                                updateContext.SaveChanges();
                                
                                // Обновляем локальный объект
                                cartItem.Quantity = quantity;
                                UpdateTotalAmount();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении количества: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadCartItems(); // Перезагружаем корзину в случае ошибки
                    }
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CartItem cartItem)
            {
                try
                {
                    using (var updateContext = new ApplicationDbContext())
                    {
                        // Загружаем элемент корзины вместе со связанными данными
                        var itemToDelete = updateContext.CartItems
                            .Include(ci => ci.Product)
                            .Include(ci => ci.User)
                            .FirstOrDefault(ci => ci.Id == cartItem.Id);

                        if (itemToDelete != null)
                        {
                            updateContext.CartItems.Remove(itemToDelete);
                            updateContext.SaveChanges();

                            // Обновляем UI
                            _cartItems.Remove(cartItem);
                            UpdateTotalAmount();
                            _mainWindow.UpdateCartCount(_cartItems.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении товара из корзины: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCartItems(); // Перезагружаем корзину в случае ошибки
                }
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CartItem cartItem)
            {
                if (cartItem.Quantity > 1)
                {
                    try
                    {
                        using (var updateContext = new ApplicationDbContext())
                        {
                            var itemToUpdate = updateContext.CartItems
                                .Include(ci => ci.Product)
                                .FirstOrDefault(ci => ci.Id == cartItem.Id);

                            if (itemToUpdate != null)
                            {
                                itemToUpdate.Quantity--;
                                updateContext.SaveChanges();

                                // Обновляем локальный объект
                                cartItem.Quantity = itemToUpdate.Quantity;
                                CartItemsListView.Items.Refresh();
                                UpdateTotalAmount();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при уменьшении количества: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadCartItems();
                    }
                }
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CartItem cartItem)
            {
                try
                {
                    using (var updateContext = new ApplicationDbContext())
                    {
                        var itemToUpdate = updateContext.CartItems
                            .Include(ci => ci.Product)
                            .FirstOrDefault(ci => ci.Id == cartItem.Id);

                        if (itemToUpdate != null && itemToUpdate.Product != null)
                        {
                            // Проверяем наличие товара
                            if (itemToUpdate.Quantity >= itemToUpdate.Product.StockQuantity)
                            {
                                MessageBox.Show($"Невозможно увеличить количество. В наличии: {itemToUpdate.Product.StockQuantity} шт.",
                                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            itemToUpdate.Quantity++;
                            updateContext.SaveChanges();

                            // Обновляем локальный объект
                            cartItem.Quantity = itemToUpdate.Quantity;
                            CartItemsListView.Items.Refresh();
                            UpdateTotalAmount();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при увеличении количества: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCartItems();
                }
            }
        }

        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_cartItems.Any())
                {
                    MessageBox.Show("Корзина пуста", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверяем наличие всех товаров на складе
                using (var checkContext = new ApplicationDbContext())
                {
                    foreach (var cartItem in _cartItems)
                    {
                        var product = checkContext.Products.Find(cartItem.ProductId);
                        if (product == null || cartItem.Quantity > product.StockQuantity)
                        {
                            MessageBox.Show($"Товар '{product?.Name ?? "Неизвестный товар"}' недоступен в требуемом количестве.\n" +
                                          $"Требуется: {cartItem.Quantity}\n" +
                                          $"На складе: {product?.StockQuantity ?? 0}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                // Выбор пункта выдачи
                var pickupPointWindow = new PickupPointSelectionWindow();
                if (pickupPointWindow.ShowDialog() != true)
                {
                    MessageBox.Show("Необходимо выбрать пункт выдачи", 
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем новый контекст для операции создания заказа
                using (var orderContext = new ApplicationDbContext())
                {
                    using (var transaction = orderContext.Database.BeginTransaction())
                    {
                        try
                        {
                            // Создаем новый заказ
                            var order = new Order
                            {
                                UserId = _currentUser.Id,
                                ShippingAddress = _currentUser.Address ?? "Не указан",
                                PickupPointId = pickupPointWindow.SelectedPickupPoint.Id,
                                PickupAddress = pickupPointWindow.SelectedPickupPoint.Address,
                                TotalAmount = _cartItems.Sum(i => i.Product.Price * i.Quantity),
                                Status = OrderStatus.Новый,
                                OrderDate = DateTime.Now
                            };

                            // Проверяем, что все обязательные поля заполнены
                            if (string.IsNullOrEmpty(order.OrderNumber))
                            {
                                order.OrderNumber = order.GenerateOrderNumber();
                            }
                            if (string.IsNullOrEmpty(order.TrackingNumber))
                            {
                                order.TrackingNumber = order.GenerateTrackingNumber();
                            }

                            // Проверяем, что пункт выдачи выбран
                            if (order.PickupPointId == 0 || string.IsNullOrEmpty(order.PickupAddress))
                            {
                                throw new Exception("Не выбран пункт выдачи");
                            }

                            // Сначала добавляем заказ
                            orderContext.Orders.Add(order);
                            try
                            {
                                await orderContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Ошибка при сохранении заказа: {ex.Message}", ex);
                            }

                            // Затем добавляем товары
                            var orderItems = new List<OrderItem>();
                            foreach (var item in _cartItems)
                            {
                                var product = await orderContext.Products.FindAsync(item.ProductId);
                                if (product != null)
                                {
                                    var orderItem = new OrderItem
                                    {
                                        OrderId = order.Id,
                                        ProductId = item.ProductId,
                                        Quantity = item.Quantity,
                                        Price = product.Price,
                                        IsCollected = false
                                    };
                                    orderItems.Add(orderItem);
                                }
                            }

                            // Добавляем все элементы заказа одним вызовом
                            orderContext.OrderItems.AddRange(orderItems);
                            try
                            {
                                await orderContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Ошибка при сохранении элементов заказа: {ex.Message}", ex);
                            }

                            // Очищаем корзину
                            var cartItemsToDelete = orderContext.CartItems
                                .Where(ci => ci.UserId == _currentUser.Id)
                                .ToList();
                            orderContext.CartItems.RemoveRange(cartItemsToDelete);
                            try
                            {
                                await orderContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Ошибка при очистке корзины: {ex.Message}", ex);
                            }

                            transaction.Commit();

                            // Очищаем локальную коллекцию
                            _cartItems.Clear();
                            UpdateTotalAmount();
                            _mainWindow.UpdateCartCount(0);

                            MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            var innerException = ex.InnerException != null ? 
                                $"\nВнутреннее исключение: {ex.InnerException.Message}" +
                                (ex.InnerException.InnerException != null ? $"\nДополнительные детали: {ex.InnerException.InnerException.Message}" : "") 
                                : "";
                            MessageBox.Show($"Ошибка при создании заказа: {ex.Message}{innerException}", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            LoadCartItems(); // Перезагружаем корзину в случае ошибки
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCartItems(); // Перезагружаем корзину в случае ошибки
            }
        }

        private void ContinueShopping_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 4)}";
        }

        private string GenerateTrackingNumber()
        {
            return $"TRK-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 4)}";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _context.Dispose();
        }
    }
}
