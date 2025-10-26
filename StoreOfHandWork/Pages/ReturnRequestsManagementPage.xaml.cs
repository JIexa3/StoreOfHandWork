using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using StoreOfHandWork.Services;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReturnRequestsManagementPage.xaml
    /// </summary>
    public partial class ReturnRequestsManagementPage : Page
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private ReturnRequest _selectedRequest;

        // Метод InitializeComponent автоматически генерируется из XAML
        
        public ReturnRequestsManagementPage(ApplicationDbContext context, IEmailService emailService)
        {
            // Инициализация контекста базы данных - сначала создаем контекст, только потом инициализируем UI
            _context = context;
            _emailService = emailService;
            _selectedRequest = new ReturnRequest(); // Инициализация пустым объектом
            
            // Инициализация компонентов интерфейса
            InitializeComponent();
            
            // Инициализация фильтров
            InitializeFilters();
            
            // Загрузка запросов на возврат
            LoadReturnRequests();
        }

        // Инициализация фильтров и сортировки
        private void InitializeFilters()
        {
            StatusFilterComboBox.SelectedIndex = 0;
            TypeFilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            
            // Заполнение ComboBox для изменения статуса
            ChangeStatusComboBox.ItemsSource = Enum.GetValues(typeof(ReturnStatus))
                .Cast<ReturnStatus>()
                .Select(s => new { Value = s, Display = GetReturnStatusDisplay(s) })
                .ToList();
            ChangeStatusComboBox.DisplayMemberPath = "Display";
            ChangeStatusComboBox.SelectedValuePath = "Value";
        }

        // Загрузка запросов на возврат
        private void LoadReturnRequests()
        {
            try
            {
                // Получение всех запросов на возврат с включением связанных данных
                var returnRequests = _context.ReturnRequests
                    .Include(rr => rr.User)
                    .Include(rr => rr.OrderItem)
                    .ThenInclude(oi => oi.Product)
                    .Include(rr => rr.OrderItem.Order)
                    .Include(rr => rr.ExchangeProduct)
                    .AsNoTracking() // Добавляем AsNoTracking() для предотвращения кэширования
                    .ToList();

                // Применение фильтров и сортировки
                ApplyFiltersAndSort(returnRequests);
                
                StatusTextBlockBar.Text = $"Всего запросов: {returnRequests.Count}";

                // Если есть выбранный запрос, обновляем его данные
                if (_selectedRequest != null)
                {
                    var updatedRequest = returnRequests.FirstOrDefault(r => r.Id == _selectedRequest.Id);
                    if (updatedRequest != null)
                    {
                        _selectedRequest = updatedRequest;
                        UpdateDetailsPanel();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке запросов на возврат: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDetailsPanel()
        {
            if (_selectedRequest != null)
            {
                // Обновляем все поля в панели деталей
                RequestIdTextBlock.Text = _selectedRequest.Id.ToString();
                RequestDateTextBlock.Text = _selectedRequest.RequestDate.ToString("dd.MM.yyyy HH:mm");
                CustomerTextBlock.Text = _selectedRequest.User?.Email ?? "Н/Д";
                OrderTextBlock.Text = $"№{_selectedRequest.OrderItem?.Order?.OrderNumber} от {_selectedRequest.OrderItem?.Order?.OrderDate.ToString("dd.MM.yyyy") ?? "Н/Д"}";
                ProductTextBlock.Text = _selectedRequest.OrderItem?.Product?.Name ?? "Н/Д";
                StatusTextBlock.Text = GetReturnStatusDisplay(_selectedRequest.Status);
                
                ReturnTypeTextBlock.Text = _selectedRequest.Type == ReturnType.Возврат ? "Возврат" : "Обмен";
                ReasonTextBlock.Text = GetReturnReasonDisplay(_selectedRequest.Reason);
                CommentsTextBlock.Text = _selectedRequest.AdditionalComments;
                
                // Обновляем комбобокс со статусами
                var availableStatuses = GetAvailableStatuses(_selectedRequest.Status);
                ChangeStatusComboBox.ItemsSource = availableStatuses.Select(s => new { Value = s, Display = GetReturnStatusDisplay(s) });
                ChangeStatusComboBox.DisplayMemberPath = "Display";
                ChangeStatusComboBox.SelectedValuePath = "Value";
                ChangeStatusComboBox.SelectedValue = _selectedRequest.Status;
            }
        }

        // Применение фильтров и сортировки
        private void ApplyFiltersAndSort(List<ReturnRequest> requests)
        {
            // Применение фильтра по статусу
            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                ReturnStatus selectedStatus = (ReturnStatus)(StatusFilterComboBox.SelectedIndex - 1);
                requests = requests.Where(r => r.Status == selectedStatus).ToList();
            }

            // Применение фильтра по типу возврата
            if (TypeFilterComboBox.SelectedIndex > 0)
            {
                ReturnType selectedType = (ReturnType)(TypeFilterComboBox.SelectedIndex - 1);
                requests = requests.Where(r => r.Type == selectedType).ToList();
            }

            // Применение сортировки
            switch (SortComboBox.SelectedIndex)
            {
                case 0: // Новые сначала
                    requests = requests.OrderByDescending(r => r.RequestDate).ToList();
                    break;
                case 1: // Старые сначала
                    requests = requests.OrderBy(r => r.RequestDate).ToList();
                    break;
                case 2: // По статусу
                    requests = requests.OrderBy(r => r.Status).ToList();
                    break;
                case 3: // По клиенту
                    requests = requests.OrderBy(r => r.User.Email).ToList();
                    break;
            }

            // Обновление источника данных таблицы
            ReturnRequestsDataGrid.ItemsSource = requests;
        }

        // Обновление данных при изменении фильтров
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, что страница инициализирована и элементы управления доступны
            if (this.ActualHeight > 0 && sender != null)
            {
                LoadReturnRequests();
            }
        }

        // Обработчик выбора запроса в таблице
        private void ReturnRequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRequest = ReturnRequestsDataGrid.SelectedItem as ReturnRequest;
            
            if (_selectedRequest != null)
            {
                // Отображение панели с деталями
                DetailsPanel.Visibility = Visibility.Visible;
                
                // Заполнение данных о запросе
                RequestIdTextBlock.Text = _selectedRequest.Id.ToString();
                RequestDateTextBlock.Text = _selectedRequest.RequestDate.ToString("dd.MM.yyyy HH:mm");
                CustomerTextBlock.Text = _selectedRequest.User?.Email ?? "Н/Д";
                OrderTextBlock.Text = $"№{_selectedRequest.OrderItem?.Order?.OrderNumber} от {_selectedRequest.OrderItem?.Order?.OrderDate.ToString("dd.MM.yyyy") ?? "Н/Д"}";
                ProductTextBlock.Text = _selectedRequest.OrderItem?.Product?.Name ?? "Н/Д";
                StatusTextBlock.Text = GetReturnStatusDisplay(_selectedRequest.Status);
                
                // Характеристики выбранного запроса
                ReturnTypeTextBlock.Text = _selectedRequest.Type == ReturnType.Возврат ? "Возврат" : "Обмен";
                ReasonTextBlock.Text = GetReturnReasonDisplay(_selectedRequest.Reason);
                CommentsTextBlock.Text = _selectedRequest.AdditionalComments;
                
                // Данные о товаре для обмена
                if (_selectedRequest.Type == ReturnType.Обмен && _selectedRequest.ExchangeProduct != null)
                {
                    ExchangeProductLabel.Visibility = Visibility.Visible;
                    ExchangeProductTextBlock.Visibility = Visibility.Visible;
                    ExchangeProductTextBlock.Text = 
                        $"{_selectedRequest.ExchangeProduct.Name} - {_selectedRequest.ExchangeProduct.Price:C}";
                }
                else
                {
                    ExchangeProductLabel.Visibility = Visibility.Collapsed;
                    ExchangeProductTextBlock.Visibility = Visibility.Collapsed;
                }
                
                // Комментарий администратора
                AdminCommentsTextBox.Text = _selectedRequest.AdminComments;
                
                // Заполняем комбобокс доступными статусами
                var availableStatuses = GetAvailableStatuses(_selectedRequest.Status);
                ChangeStatusComboBox.ItemsSource = availableStatuses.Select(s => new { Value = s, Display = GetReturnStatusDisplay(s) });
                ChangeStatusComboBox.DisplayMemberPath = "Display";
                ChangeStatusComboBox.SelectedValuePath = "Value";
                ChangeStatusComboBox.SelectedValue = _selectedRequest.Status;
            }
            else
            {
                // Скрытие панели с деталями
                DetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private List<ReturnStatus> GetAvailableStatuses(ReturnStatus currentStatus)
        {
            var statuses = new List<ReturnStatus> { currentStatus };

            switch (currentStatus)
            {
                case ReturnStatus.ЗаявкаОтправлена:
                    statuses.AddRange(new[] { ReturnStatus.Одобрено, ReturnStatus.Отклонено, ReturnStatus.Отменено });
                    break;

                case ReturnStatus.Одобрено:
                    statuses.AddRange(new[] { ReturnStatus.ТоварПолучен, ReturnStatus.Отменено });
                    break;

                case ReturnStatus.ТоварПолучен:
                    statuses.AddRange(new[] { ReturnStatus.ВозвратЗавершен, ReturnStatus.ОбменЗавершен });
                    break;
            }

            return statuses;
        }

        // Сохранение изменений в запросе на возврат
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Не выбран запрос на возврат.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ReturnStatus newStatus = (ReturnStatus)ChangeStatusComboBox.SelectedValue;
                
                if (!IsValidStatusTransition(_selectedRequest.Status, newStatus))
                {
                    MessageBox.Show("Недопустимое изменение статуса запроса.", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Получаем актуальную версию запроса из базы данных
                        var returnRequest = await _context.ReturnRequests
                            .Include(r => r.User)
                            .Include(r => r.OrderItem)
                            .ThenInclude(oi => oi.Product)
                            .FirstOrDefaultAsync(r => r.Id == _selectedRequest.Id);

                        if (returnRequest == null)
                        {
                            MessageBox.Show("Запрос на возврат не найден в базе данных.", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var oldStatus = returnRequest.Status;
                        returnRequest.Status = newStatus;
                        returnRequest.AdminComments = AdminCommentsTextBox.Text;
                        
                        if (!returnRequest.ReviewDate.HasValue && 
                            (newStatus == ReturnStatus.Одобрено || newStatus == ReturnStatus.Отклонено))
                        {
                            returnRequest.ReviewDate = DateTime.Now;
                        }
                        
                        if (newStatus == ReturnStatus.ВозвратЗавершен || 
                            newStatus == ReturnStatus.ОбменЗавершен || 
                            newStatus == ReturnStatus.Отклонено || 
                            newStatus == ReturnStatus.Отменено)
                        {
                            returnRequest.CompletionDate = DateTime.Now;
                        }

                        await _context.SaveChangesAsync();

                        // Отправляем уведомление по email
                        try
                        {
                            await _emailService.SendReturnStatusUpdateEmailAsync(returnRequest);
                        }
                        catch (Exception emailEx)
                        {
                            // Логируем ошибку отправки email, но не отменяем транзакцию
                            MessageBox.Show($"Статус обновлен, но возникла ошибка при отправке уведомления: {emailEx.Message}",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        await transaction.CommitAsync();
                        
                        MessageBox.Show("Изменения успешно сохранены.", 
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                        // Обновляем список запросов
                        LoadReturnRequests();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadReturnRequests();
            }
        }
        
        // Проверка валидности изменения статуса
        private bool IsValidStatusTransition(ReturnStatus currentStatus, ReturnStatus newStatus)
        {
            // Определяем допустимые переходы для каждого статуса
            switch (currentStatus)
            {
                case ReturnStatus.ЗаявкаОтправлена:
                    return newStatus == ReturnStatus.Одобрено || 
                           newStatus == ReturnStatus.Отклонено ||
                           newStatus == ReturnStatus.Отменено;

                case ReturnStatus.Одобрено:
                    return newStatus == ReturnStatus.ТоварПолучен || 
                           newStatus == ReturnStatus.Отменено;

                case ReturnStatus.ТоварПолучен:
                    return newStatus == ReturnStatus.ВозвратЗавершен || 
                           newStatus == ReturnStatus.ОбменЗавершен;

                case ReturnStatus.Отклонено:
                case ReturnStatus.ВозвратЗавершен:
                case ReturnStatus.ОбменЗавершен:
                case ReturnStatus.Отменено:
                    return false; // Конечные статусы, переход из них невозможен

                default:
                    return false;
            }
        }
        
        // Обработка логики завершения возврата/обмена
        private void ProcessReturnCompletion(ReturnRequest request)
        {
            // Получение товара и заказа
            var orderItem = _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .FirstOrDefault(oi => oi.Id == request.OrderItemId);
                
            if (orderItem == null) return;
            
            // Обработка в зависимости от типа возврата
            if (request.Type == ReturnType.Возврат)
            {
                // Возврат товара на склад
                var product = orderItem.Product;
                product.StockQuantity += orderItem.Quantity;
                
                // Тут можно добавить логику возврата средств клиенту,
                // но это требует интеграции с платежной системой
            }
            else if (request.Type == ReturnType.Обмен && request.ExchangeProductId.HasValue)
            {
                // Возврат старого товара на склад
                var oldProduct = orderItem.Product;
                oldProduct.StockQuantity += orderItem.Quantity;
                
                // Выдача нового товара клиенту
                var newProduct = _context.Products.Find(request.ExchangeProductId.Value);
                if (newProduct != null && newProduct.StockQuantity >= orderItem.Quantity)
                {
                    newProduct.StockQuantity -= orderItem.Quantity;
                }
            }
        }
        
        // Обработчик кнопки обновления
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReturnRequests();
        }
        
        // Открытие страницы управления правилами возврата
        private void ReturnPolicyButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем окно для отображения страницы политик возврата
            Window policyWindow = new Window
            {
                Title = "Управление правилами возврата",
                Content = new ReturnPolicyManagementPage(),
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            
            // Отображаем окно как модальное диалоговое окно
            policyWindow.ShowDialog();
            
            // После закрытия окна правил возврата обновляем данные
            LoadReturnRequests();
        }
        
        // Получение текстового представления статуса возврата
        private string GetReturnStatusDisplay(ReturnStatus status)
        {
            return status switch
            {
                ReturnStatus.ЗаявкаОтправлена => "Заявка отправлена",
                ReturnStatus.Одобрено => "Одобрено",
                ReturnStatus.Отклонено => "Отклонено",
                ReturnStatus.ТоварПолучен => "Товар получен",
                ReturnStatus.ВозвратЗавершен => "Возврат завершен",
                ReturnStatus.ОбменЗавершен => "Обмен завершен",
                ReturnStatus.Отменено => "Отменено",
                _ => status.ToString()
            };
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
    }
}
