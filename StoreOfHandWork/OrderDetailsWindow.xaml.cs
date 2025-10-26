using System.Windows;
using System.Linq;
using StoreOfHandWork.Models;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Converters;
using StoreOfHandWork.Data;

namespace StoreOfHandWork
{
    public partial class OrderDetailsWindow : Window
    {
        private readonly Order _order;
        private readonly ApplicationDbContext _context;
        private readonly OrderStatusConverter _statusConverter;

        public OrderDetailsWindow(Order order)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _statusConverter = new OrderStatusConverter();
            
            // Загружаем заказ со всеми связанными данными
            _order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.PickupPoint)
                .FirstOrDefault(o => o.Id == order.Id);

            if (_order == null)
            {
                MessageBox.Show("Заказ не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            try
            {
                OrderNumberTextBlock.Text = $"Заказ №{_order.OrderNumber}";
                OrderDateTextBlock.Text = $"Дата: {_order.OrderDate:dd.MM.yyyy HH:mm}";
                StatusTextBlock.Text = $"Статус: {_statusConverter.Convert(_order.Status, typeof(string), null, null)}";
                CustomerEmailTextBlock.Text = $"Email покупателя: {_order.User?.Email ?? "Не указан"}";
                
                // Формируем информацию об адресе
                var addressInfo = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(_order.ShippingAddress))
                {
                    addressInfo.AppendLine($"Адрес доставки: {_order.ShippingAddress}");
                }
                
                // Добавляем информацию о пункте выдачи
                if (_order.PickupPoint != null)
                {
                    addressInfo.AppendLine($"Адрес пункта выдачи: {_order.PickupPoint.Address}");
                    if (!string.IsNullOrEmpty(_order.PickupPoint.WorkingHours))
                    {
                        addressInfo.AppendLine($"Время работы: {_order.PickupPoint.WorkingHours}");
                    }
                    if (!string.IsNullOrEmpty(_order.PickupPoint.Phone))
                    {
                        addressInfo.AppendLine($"Телефон: {_order.PickupPoint.Phone}");
                    }
                }
                
                ShippingAddressTextBlock.Text = addressInfo.ToString().TrimEnd();
                TotalAmountTextBlock.Text = $"Общая сумма: {_order.TotalAmount:N2} ₽";

                var orderItems = _order.OrderItems?
                    .Select(item => new
                    {
                        ProductName = item.Product?.Name ?? "Неизвестный продукт",
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TotalAmount = item.Price * item.Quantity
                    })
                    .ToList();

                OrderItemsDataGrid.ItemsSource = orderItems;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей заказа: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }
}
