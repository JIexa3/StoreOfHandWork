using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReturnRequestsPage.xaml
    /// </summary>
    public partial class ReturnRequestsPage : Page
    {
        private readonly ApplicationDbContext _context;
        private User _currentUser;
        private List<Order> _userOrders = new List<Order>();  // Инициализация пустым списком
        private ReturnPolicy _activeReturnPolicy = new ReturnPolicy();  // Инициализация пустой политикой

        public ReturnRequestsPage(User currentUser)
        {
            // Инициализация контекста базы данных - сначала создаем контекст, только потом инициализируем UI
            _context = new ApplicationDbContext();
            _currentUser = currentUser;
            
            // Инициализация компонентов интерфейса
            InitializeComponent();
            
            try
            {
                // Заполнение ComboBox для причин возврата
                if (ReturnReasonComboBox != null)
                {
                    ReturnReasonComboBox.ItemsSource = Enum.GetValues(typeof(ReturnReason))
                        .Cast<ReturnReason>()
                        .Select(r => new { Value = r, Display = GetReturnReasonDisplay(r) })
                        .ToList();
                    ReturnReasonComboBox.DisplayMemberPath = "Display";
                    ReturnReasonComboBox.SelectedValuePath = "Value";
                    ReturnReasonComboBox.SelectedIndex = 0;
                }

                // Загрузка заказов пользователя
                LoadUserOrders();
                
                // Загрузка активной политики возврата
                LoadReturnPolicy();
                
                // Загрузка существующих запросов на возврат
                LoadUserReturnRequests();
                
                // Настройка фильтров и сортировки
                if (StatusFilterComboBox != null)
                    StatusFilterComboBox.SelectedIndex = 0;
                    
                if (SortComboBox != null)
                    SortComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации страницы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка заказов пользователя
        private void LoadUserOrders()
        {
            try
            {
                // Проверка на null для избежания NullReferenceException
                if (OrdersComboBox == null)
                    return;
                    
                _userOrders = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == _currentUser.Id && 
                           (o.Status == OrderStatus.Доставлен || o.Status == OrderStatus.Отправлен))
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                OrdersComboBox.ItemsSource = _userOrders.Select(o => new 
                { 
                    Order = o, 
                    Display = $"Заказ №{o.OrderNumber} от {o.OrderDate:dd.MM.yyyy}" 
                }).ToList();
                OrdersComboBox.DisplayMemberPath = "Display";
                OrdersComboBox.SelectedValuePath = "Order";

                if (_userOrders.Any())
                {
                    OrdersComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка активной политики возврата
        private void LoadReturnPolicy()
        {
            try
            {
                // Проверка на инициализацию UI элемента
                if (ReturnPolicyTextBlock == null)
                    return;
                    
                var activePolicy = _context.ReturnPolicies
                    .Where(rp => rp.IsActive)
                    .OrderByDescending(rp => rp.LastUpdated)
                    .FirstOrDefault();

                _activeReturnPolicy = activePolicy ?? new ReturnPolicy
                {
                    Title = "По умолчанию",
                    ReturnPeriodDays = 14,
                    GeneralConditions = "Товар должен быть в оригинальной упаковке и не иметь следов использования."
                };

                if (_activeReturnPolicy != null)
                {
                    // ReturnPolicyTextBlock уже проверен на null выше
                    ReturnPolicyTextBlock.Text = 
                        $"Срок возврата: {_activeReturnPolicy.ReturnPeriodDays} дней\n\n" +
                        $"{_activeReturnPolicy.GeneralConditions}\n\n" +
                        $"Условия возврата средств:\n{_activeReturnPolicy.RefundPolicy}\n\n" +
                        $"Условия обмена товара:\n{_activeReturnPolicy.ExchangePolicy}";

                    if (!string.IsNullOrEmpty(_activeReturnPolicy.ExcludedCategories))
                    {
                        ReturnPolicyTextBlock.Text += $"\n\nКатегории товаров, которые не подлежат возврату:\n{_activeReturnPolicy.ExcludedCategories}";
                    }
                }
                else
                {
                    ReturnPolicyTextBlock.Text = "Правила возврата не настроены. Пожалуйста, обратитесь к администратору.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке правил возврата: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка запросов на возврат пользователя
        private void LoadUserReturnRequests()
        {
            try
            {
                var returnRequests = _context.ReturnRequests
                    .Include(rr => rr.OrderItem)
                    .ThenInclude(oi => oi.Product)
                    .Where(rr => rr.UserId == _currentUser.Id)
                    .OrderByDescending(rr => rr.RequestDate)
                    .ToList();

                ApplyFiltersAndSort(returnRequests);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке запросов на возврат: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Применение фильтров и сортировки к списку запросов
        private void ApplyFiltersAndSort(List<ReturnRequest> requests)
        {
            // Применение фильтра по статусу
            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                ReturnStatus selectedStatus = (ReturnStatus)(StatusFilterComboBox.SelectedIndex - 1);
                requests = requests.Where(r => r.Status == selectedStatus).ToList();
            }

            // Применение сортировки
            switch (SortComboBox.SelectedIndex)
            {
                case 0: // По дате (новые)
                    requests = requests.OrderByDescending(r => r.RequestDate).ToList();
                    break;
                case 1: // По дате (старые)
                    requests = requests.OrderBy(r => r.RequestDate).ToList();
                    break;
                case 2: // По статусу
                    requests = requests.OrderBy(r => r.Status).ToList();
                    break;
            }

            ReturnRequestsDataGrid.ItemsSource = requests;
        }

        // Обработчик изменения выбранного заказа
        private void OrdersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersComboBox.SelectedItem != null)
            {
                var selectedOrder = (OrdersComboBox.SelectedItem as dynamic).Order as Order;
                
                // Заполнение списка товаров из выбранного заказа
                OrderItemsComboBox.ItemsSource = selectedOrder.OrderItems.Select(oi => new 
                { 
                    OrderItem = oi, 
                    Display = $"{oi.Product.Name} (x{oi.Quantity})" 
                }).ToList();
                OrderItemsComboBox.DisplayMemberPath = "Display";
                OrderItemsComboBox.SelectedValuePath = "OrderItem";
                
                if (selectedOrder.OrderItems.Any())
                {
                    OrderItemsComboBox.SelectedIndex = 0;
                }

                // Загрузка списка товаров для обмена
                LoadExchangeProducts();
            }
        }

        // Загрузка товаров для обмена
        private void LoadExchangeProducts()
        {
            try
            {
                var exchangeProducts = _context.Products
                    .Where(p => p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToList();

                ExchangeProductComboBox.ItemsSource = exchangeProducts.Select(p => new 
                { 
                    Product = p, 
                    Display = $"{p.Name} - {p.Price:C}" 
                }).ToList();
                ExchangeProductComboBox.DisplayMemberPath = "Display";
                ExchangeProductComboBox.SelectedValuePath = "Product";
                
                if (exchangeProducts.Any())
                {
                    ExchangeProductComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке товаров для обмена: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавляем метод для проверки инициализации элементов управления
        private bool IsControlInitialized(params object[] controls)
        {
            foreach (var control in controls)
            {
                if (control == null)
                    return false;
            }
            return true;
        }

        // Обработчик изменения типа возврата
        private void ReturnTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Добавляем проверку на null для избежания NullReferenceException
            if (!IsControlInitialized(ExchangeRadioButton, ExchangeProductLabel, ExchangeProductComboBox))
                return;
                
            try
            {
                bool isExchange = ExchangeRadioButton.IsChecked ?? false;
                ExchangeProductLabel.Visibility = isExchange ? Visibility.Visible : Visibility.Collapsed;
                ExchangeProductComboBox.Visibility = isExchange ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении типа возврата: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки отмены
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Очистка формы
        private void ClearForm()
        {
            if (OrdersComboBox.Items.Count > 0)
                OrdersComboBox.SelectedIndex = 0;
                
            ReturnReasonComboBox.SelectedIndex = 0;
            CommentsTextBox.Text = string.Empty;
            RefundRadioButton.IsChecked = true;
        }

        // Обработчик кнопки отправки запроса
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OrderItemsComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите товар для возврата.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedOrderItem = (OrderItemsComboBox.SelectedItem as dynamic).OrderItem as OrderItem;
                var selectedOrder = (OrdersComboBox.SelectedItem as dynamic).Order as Order;
                
                // Проверка срока возврата
                if (_activeReturnPolicy != null)
                {
                    TimeSpan timeSinceOrder = DateTime.Now - selectedOrder.OrderDate;
                    if (timeSinceOrder.TotalDays > _activeReturnPolicy.ReturnPeriodDays)
                    {
                        MessageBox.Show($"Срок возврата для этого заказа истек. Максимальный срок возврата - {_activeReturnPolicy.ReturnPeriodDays} дней.",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Проверка наличия уже существующего запроса на возврат для этого товара
                var existingRequest = _context.ReturnRequests
                    .FirstOrDefault(rr => rr.OrderItemId == selectedOrderItem.Id && 
                                  rr.Status != ReturnStatus.Отклонено && 
                                  rr.Status != ReturnStatus.Отменено);
                
                if (existingRequest != null)
                {
                    MessageBox.Show("Для этого товара уже существует активный запрос на возврат.",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создание нового запроса на возврат
                var returnRequest = new ReturnRequest
                {
                    OrderItemId = selectedOrderItem.Id,
                    UserId = _currentUser.Id,
                    Reason = (ReturnReason)ReturnReasonComboBox.SelectedValue,
                    AdditionalComments = CommentsTextBox.Text,
                    Type = ExchangeRadioButton.IsChecked == true ? ReturnType.Обмен : ReturnType.Возврат
                };

                // Если выбран обмен, добавляем информацию о товаре для обмена
                if (returnRequest.Type == ReturnType.Обмен && ExchangeProductComboBox.SelectedItem != null)
                {
                    var exchangeProduct = (ExchangeProductComboBox.SelectedItem as dynamic).Product as Product;
                    returnRequest.ExchangeProductId = exchangeProduct.Id;
                    
                    // Проверяем, не дешевле ли новый товар старого (тогда нужно вернуть разницу)
                    if (exchangeProduct.Price < selectedOrderItem.Product.Price)
                    {
                        decimal refundAmount = selectedOrderItem.Product.Price - exchangeProduct.Price;
                        
                        // Показываем окно с информацией о возврате разницы
                        MessageBoxResult result = MessageBox.Show(
                            $"Выбранный товар для обмена дешевле на {refundAmount:C}. \n\nПосле подтверждения администратором разница будет возвращена на вашу карту. \n\nВведите реквизиты карты?",
                            "Возврат разницы",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                            
                        if (result == MessageBoxResult.Yes)
                        {
                            // Запрашиваем номер карты с маской
                            var cardInputWindow = new CardInputWindow($"Введите номер карты для возврата {refundAmount:C}");
                            bool? cardResult = cardInputWindow.ShowDialog();
                            
                            if (cardResult.HasValue && cardResult.Value && cardInputWindow.IsConfirmed)
                            {
                                string cardNumber = cardInputWindow.CardNumber;
                                
                                // Запрашиваем дополнительные данные
                                string expiryDate = GetUserInput("Введите срок действия карты (ММ/ГГ):");
                                if (string.IsNullOrEmpty(expiryDate)) return;
                                
                                string cardholderName = GetUserInput("Введите имя владельца карты:");
                                if (string.IsNullOrEmpty(cardholderName)) return;
                                
                                returnRequest.AdditionalComments += $"\nРеквизиты для возврата разницы: {cardNumber}, {expiryDate}, {cardholderName}";
                            }
                            else
                            {
                                return; // Пользователь отменил ввод данных карты
                            }
                        }
                        else
                        {
                            return; // Пользователь отменил ввод данных карты
                        }
                    }
                }
                
                // Если выбран возврат средств, запрашиваем данные карты
                if (returnRequest.Type == ReturnType.Возврат)
                {
                    decimal refundAmount = selectedOrderItem.Product.Price;
                    
                    // Показываем окно с информацией о возврате средств
                    MessageBoxResult result = MessageBox.Show(
                        $"Возврат средств на сумму {refundAmount:C}. \n\nПосле подтверждения администратором средства будут возвращены на вашу карту. \n\nВведите реквизиты карты?",
                        "Возврат средств",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        // Запрашиваем номер карты с маской
                        var cardInputWindow = new CardInputWindow($"Введите номер карты для возврата {refundAmount:C}");
                        bool? cardResult = cardInputWindow.ShowDialog();
                        
                        if (cardResult.HasValue && cardResult.Value && cardInputWindow.IsConfirmed)
                        {
                            string cardNumber = cardInputWindow.CardNumber;
                            
                            // Запрашиваем дополнительные данные
                            string expiryDate = GetUserInput("Введите срок действия карты (ММ/ГГ):");
                            if (string.IsNullOrEmpty(expiryDate)) return;
                            
                            string cardholderName = GetUserInput("Введите имя владельца карты:");
                            if (string.IsNullOrEmpty(cardholderName)) return;
                            
                            returnRequest.AdditionalComments += $"\nРеквизиты для возврата средств: {cardNumber}, {expiryDate}, {cardholderName}";
                        }
                        else
                        {
                            return; // Пользователь отменил ввод данных карты
                        }
                    }
                    else
                    {
                        return; // Пользователь отменил ввод данных карты
                    }
                }

                // Сохранение запроса в базу данных
                _context.ReturnRequests.Add(returnRequest);
                _context.SaveChanges();

                MessageBox.Show("Запрос на возврат успешно создан и будет рассмотрен администратором.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Очистка формы и обновление списка запросов
                ClearForm();
                LoadUserReturnRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании запроса на возврат: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик изменения фильтра статуса
        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUserReturnRequests();
        }

        // Обработчик изменения сортировки
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUserReturnRequests();
        }

        // Обработчик выбора запроса в таблице
        private void ReturnRequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRequest = ReturnRequestsDataGrid.SelectedItem as ReturnRequest;
            if (selectedRequest != null)
            {
                CustomerCommentsTextBlock.Text = selectedRequest.AdditionalComments;
                AdminCommentsTextBlock.Text = selectedRequest.AdminComments;
                ReviewDateTextBlock.Text = selectedRequest.ReviewDate.HasValue 
                    ? selectedRequest.ReviewDate.Value.ToString("dd.MM.yyyy HH:mm") 
                    : "Не рассмотрено";
                CompletionDateTextBlock.Text = selectedRequest.CompletionDate.HasValue 
                    ? selectedRequest.CompletionDate.Value.ToString("dd.MM.yyyy HH:mm") 
                    : "Не завершено";
                StatusTextBlock.Text = GetReturnStatusDisplay(selectedRequest.Status);
            }
            else
            {
                CustomerCommentsTextBlock.Text = string.Empty;
                AdminCommentsTextBlock.Text = string.Empty;
                ReviewDateTextBlock.Text = string.Empty;
                CompletionDateTextBlock.Text = string.Empty;
                StatusTextBlock.Text = string.Empty;
            }
        }

        // Получение текстового представления причины возврата
        private string GetReturnReasonDisplay(ReturnReason reason)
        {
            switch (reason)
            {
                case ReturnReason.Брак:
                    return "Брак/дефект товара";
                case ReturnReason.НеВерныйРазмер:
                    return "Не подошел размер";
                case ReturnReason.НеВерныйТовар:
                    return "Получен не тот товар";
                case ReturnReason.НеСоответствуетОписанию:
                    return "Не соответствует описанию";
                case ReturnReason.НашлиДешевле:
                    return "Нашли дешевле";
                case ReturnReason.Передумал:
                    return "Передумал";
                case ReturnReason.Другое:
                    return "Другое";
                default:
                    return reason.ToString();
            }
        }
        
        // Получение текстового представления статуса возврата на русском языке
        private string GetReturnStatusDisplay(ReturnStatus status)
        {
            switch (status)
            {
                case ReturnStatus.ЗаявкаОтправлена:
                    return "Заявка отправлена";
                case ReturnStatus.Одобрено:
                    return "Одобрено";
                case ReturnStatus.Отклонено:
                    return "Отклонено";
                case ReturnStatus.ТоварПолучен:
                    return "Товар получен";
                case ReturnStatus.ВозвратЗавершен:
                    return "Возврат выполнен";
                case ReturnStatus.ОбменЗавершен:
                    return "Обмен оформлен";
                case ReturnStatus.Отменено:
                    return "Отменено";
                default:
                    return status.ToString();
            }
        }
        
        /// <summary>
        /// Метод для запроса данных у пользователя через диалоговое окно
        /// </summary>
        /// <param name="prompt">Текст запроса</param>
        /// <returns>Введенное пользователем значение или пустая строка, если пользователь отменил ввод</returns>
        private string GetUserInput(string prompt)
        {
            // Используем наше специальное окно для ввода текста
            var textInputWindow = new TextInputWindow(prompt);
            bool? result = textInputWindow.ShowDialog();
            
            // Если пользователь нажал ОК, возвращаем введенное значение
            if (result.HasValue && result.Value && textInputWindow.IsConfirmed)
            {
                return textInputWindow.InputText;
            }
            
            // Если пользователь отменил ввод, возвращаем пустую строку
            return string.Empty;
        }
    }
}
