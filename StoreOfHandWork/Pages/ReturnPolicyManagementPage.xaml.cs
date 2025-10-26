using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReturnPolicyManagementPage.xaml
    /// </summary>
    public partial class ReturnPolicyManagementPage : Page
    {
        private readonly ApplicationDbContext _context;
        private ReturnPolicy _currentPolicy = new ReturnPolicy(); // Инициализация пустым объектом
        private bool _isNewPolicy = false;

        public ReturnPolicyManagementPage()
        {
            // Инициализация контекста базы данных - сначала создаем контекст, только потом инициализируем UI
            _context = new ApplicationDbContext();
            
            // Инициализация компонентов интерфейса
            InitializeComponent();
            
            // Загрузка активной политики возврата
            LoadActivePolicy();
            
            // Загрузка истории политик
            LoadPolicyHistory();
        }

        // Загрузка активной политики возврата
        private void LoadActivePolicy()
        {
            try
            {
                _currentPolicy = _context.ReturnPolicies
                    .Where(rp => rp.IsActive)
                    .OrderByDescending(rp => rp.LastUpdated)
                    .FirstOrDefault();

                if (_currentPolicy != null)
                {
                    // Заполнение полей формы данными активной политики
                    TitleTextBox.Text = _currentPolicy.Title;
                    ReturnPeriodTextBox.Text = _currentPolicy.ReturnPeriodDays.ToString();
                    GeneralConditionsTextBox.Text = _currentPolicy.GeneralConditions;
                    ExcludedCategoriesTextBox.Text = _currentPolicy.ExcludedCategories;
                    RefundPolicyTextBox.Text = _currentPolicy.RefundPolicy;
                    ExchangePolicyTextBox.Text = _currentPolicy.ExchangePolicy;
                    IsActiveCheckBox.IsChecked = _currentPolicy.IsActive;
                    
                    StatusTextBlock.Text = $"Активная политика: {_currentPolicy.Title} (ID: {_currentPolicy.Id})";
                }
                else
                {
                    // Если активной политики нет, создаем шаблон
                    CreateDefaultPolicy();
                    StatusTextBlock.Text = "Активная политика не найдена. Создан шаблон.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке активной политики возврата: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Создание шаблона политики по умолчанию
        private void CreateDefaultPolicy()
        {
            _currentPolicy = new ReturnPolicy
            {
                Title = "Стандартные правила возврата",
                ReturnPeriodDays = 14,
                GeneralConditions = "1. Возврат товара возможен в течение 14 дней с момента получения заказа.\n" +
                                  "2. Товар должен быть в оригинальной упаковке, не иметь следов использования.\n" +
                                  "3. Необходимо предоставить документ, подтверждающий покупку.",
                ExcludedCategories = "1. Изделия, изготовленные по индивидуальному заказу.\n" +
                                   "2. Товары с нарушенной заводской упаковкой.",
                RefundPolicy = "1. Возврат средств производится на банковскую карту, с которой производилась оплата.\n" +
                              "2. Срок возврата средств - до 10 рабочих дней.",
                ExchangePolicy = "1. Обмен товара возможен только на товары аналогичной категории.\n" +
                               "2. При обмене на товар с более высокой ценой покупатель доплачивает разницу.",
                IsActive = true,
                LastUpdated = DateTime.Now,
                UpdatedBy = "Система"
            };

            TitleTextBox.Text = _currentPolicy.Title;
            ReturnPeriodTextBox.Text = _currentPolicy.ReturnPeriodDays.ToString();
            GeneralConditionsTextBox.Text = _currentPolicy.GeneralConditions;
            ExcludedCategoriesTextBox.Text = _currentPolicy.ExcludedCategories;
            RefundPolicyTextBox.Text = _currentPolicy.RefundPolicy;
            ExchangePolicyTextBox.Text = _currentPolicy.ExchangePolicy;
            IsActiveCheckBox.IsChecked = _currentPolicy.IsActive;
            
            _isNewPolicy = true;
        }

        // Загрузка истории политик возврата
        private void LoadPolicyHistory()
        {
            try
            {
                var policyHistory = _context.ReturnPolicies
                    .OrderByDescending(rp => rp.LastUpdated)
                    .ToList();

                PolicyHistoryDataGrid.ItemsSource = policyHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории политик возврата: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки создания новой политики
        private void NewPolicyButton_Click(object sender, RoutedEventArgs e)
        {
            // Спрашиваем пользователя, хочет ли он создать новую политику
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите создать новую политику возврата?", 
                "Подтверждение", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                CreateDefaultPolicy();
            }
        }

        // Обработчик кнопки сохранения политики
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация введенных данных
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Название политики не может быть пустым.", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ReturnPeriodTextBox.Text) || !int.TryParse(ReturnPeriodTextBox.Text, out int returnPeriod) || returnPeriod <= 0)
                {
                    MessageBox.Show("Срок возврата должен быть положительным числом.", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(GeneralConditionsTextBox.Text))
                {
                    MessageBox.Show("Общие условия не могут быть пустыми.", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Если создаем новую политику или изменяем активность существующей
                if (_isNewPolicy || (_currentPolicy.IsActive != IsActiveCheckBox.IsChecked))
                {
                    // Если устанавливаем текущую политику как активную, деактивируем все остальные
                    if (IsActiveCheckBox.IsChecked == true)
                    {
                        var activePolicies = _context.ReturnPolicies.Where(rp => rp.IsActive);
                        foreach (var policy in activePolicies)
                        {
                            policy.IsActive = false;
                        }
                    }
                }

                // Если создаем новую политику
                if (_isNewPolicy)
                {
                    // Создаем новую запись в базе данных
                    var newPolicy = new ReturnPolicy
                    {
                        Title = TitleTextBox.Text,
                        ReturnPeriodDays = returnPeriod,
                        GeneralConditions = GeneralConditionsTextBox.Text,
                        ExcludedCategories = ExcludedCategoriesTextBox.Text,
                        RefundPolicy = RefundPolicyTextBox.Text,
                        ExchangePolicy = ExchangePolicyTextBox.Text,
                        IsActive = IsActiveCheckBox.IsChecked ?? false,
                        LastUpdated = DateTime.Now,
                        UpdatedBy = "Администратор" // Тут можно использовать имя текущего администратора
                    };

                    _context.ReturnPolicies.Add(newPolicy);
                    _currentPolicy = newPolicy;
                }
                else
                {
                    // Обновляем существующую политику
                    _currentPolicy.Title = TitleTextBox.Text;
                    _currentPolicy.ReturnPeriodDays = returnPeriod;
                    _currentPolicy.GeneralConditions = GeneralConditionsTextBox.Text;
                    _currentPolicy.ExcludedCategories = ExcludedCategoriesTextBox.Text;
                    _currentPolicy.RefundPolicy = RefundPolicyTextBox.Text;
                    _currentPolicy.ExchangePolicy = ExchangePolicyTextBox.Text;
                    _currentPolicy.IsActive = IsActiveCheckBox.IsChecked ?? false;
                    _currentPolicy.LastUpdated = DateTime.Now;
                    _currentPolicy.UpdatedBy = "Администратор"; // Тут можно использовать имя текущего администратора
                }

                // Сохраняем изменения в базе данных
                _context.SaveChanges();

                // Обновляем интерфейс
                _isNewPolicy = false;
                LoadPolicyHistory();
                
                StatusTextBlock.Text = $"Политика возврата успешно сохранена. ID: {_currentPolicy.Id}";
                
                MessageBox.Show("Политика возврата успешно сохранена.", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении политики возврата: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик выбора политики в истории
        private void PolicyHistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем кнопки управления историей
            bool isPolicySelected = PolicyHistoryDataGrid.SelectedItem != null;
            ViewHistoryButton.IsEnabled = isPolicySelected;
            ActivateHistoryButton.IsEnabled = isPolicySelected;
        }

        // Обработчик кнопки просмотра политики из истории
        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPolicy = PolicyHistoryDataGrid.SelectedItem as ReturnPolicy;
            if (selectedPolicy != null)
            {
                // Создаем новое окно для просмотра политики
                var policyViewWindow = new Window
                {
                    Title = $"Просмотр политики: {selectedPolicy.Title}",
                    Width = 700,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // Создаем содержимое окна
                var scrollViewer = new ScrollViewer();
                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                
                // Добавляем информацию о политике
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = selectedPolicy.Title, 
                    FontSize = 18, 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 0, 0, 10) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = $"ID: {selectedPolicy.Id}", 
                    Margin = new Thickness(0, 0, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = $"Последнее обновление: {selectedPolicy.LastUpdated:dd.MM.yyyy HH:mm}", 
                    Margin = new Thickness(0, 0, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = $"Обновил: {selectedPolicy.UpdatedBy}", 
                    Margin = new Thickness(0, 0, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = $"Активно: {(selectedPolicy.IsActive ? "Да" : "Нет")}", 
                    Margin = new Thickness(0, 0, 0, 15) 
                });
                
                // Срок возврата
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "Срок возврата:", 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 10, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = $"{selectedPolicy.ReturnPeriodDays} дней", 
                    Margin = new Thickness(0, 0, 0, 10) 
                });
                
                // Общие условия
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "Общие условия:", 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 10, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = selectedPolicy.GeneralConditions, 
                    TextWrapping = TextWrapping.Wrap, 
                    Margin = new Thickness(0, 0, 0, 10) 
                });
                
                // Исключения
                if (!string.IsNullOrEmpty(selectedPolicy.ExcludedCategories))
                {
                    stackPanel.Children.Add(new TextBlock 
                    { 
                        Text = "Исключения:", 
                        FontWeight = FontWeights.Bold, 
                        Margin = new Thickness(0, 10, 0, 5) 
                    });
                    
                    stackPanel.Children.Add(new TextBlock 
                    { 
                        Text = selectedPolicy.ExcludedCategories, 
                        TextWrapping = TextWrapping.Wrap, 
                        Margin = new Thickness(0, 0, 0, 10) 
                    });
                }
                
                // Условия возврата средств
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "Условия возврата средств:", 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 10, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = selectedPolicy.RefundPolicy, 
                    TextWrapping = TextWrapping.Wrap, 
                    Margin = new Thickness(0, 0, 0, 10) 
                });
                
                // Условия обмена товара
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "Условия обмена товара:", 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 10, 0, 5) 
                });
                
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = selectedPolicy.ExchangePolicy, 
                    TextWrapping = TextWrapping.Wrap, 
                    Margin = new Thickness(0, 0, 0, 10) 
                });
                
                // Кнопка закрытия
                var closeButton = new Button 
                { 
                    Content = "Закрыть", 
                    Width = 100, 
                    Margin = new Thickness(0, 20, 0, 0), 
                    HorizontalAlignment = HorizontalAlignment.Right 
                };
                closeButton.Click += (s, args) => policyViewWindow.Close();
                stackPanel.Children.Add(closeButton);
                
                scrollViewer.Content = stackPanel;
                policyViewWindow.Content = scrollViewer;
                
                // Отображаем окно
                policyViewWindow.ShowDialog();
            }
        }

        // Обработчик кнопки активации политики из истории
        private void ActivateHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPolicy = PolicyHistoryDataGrid.SelectedItem as ReturnPolicy;
            if (selectedPolicy != null)
            {
                try
                {
                    // Деактивируем все активные политики
                    var activePolicies = _context.ReturnPolicies.Where(rp => rp.IsActive);
                    foreach (var policy in activePolicies)
                    {
                        policy.IsActive = false;
                    }
                    
                    // Активируем выбранную политику
                    selectedPolicy.IsActive = true;
                    selectedPolicy.LastUpdated = DateTime.Now;
                    selectedPolicy.UpdatedBy = "Администратор"; // Тут можно использовать имя текущего администратора
                    
                    // Сохраняем изменения
                    _context.SaveChanges();
                    
                    // Обновляем текущую политику
                    _currentPolicy = selectedPolicy;
                    _isNewPolicy = false;
                    
                    // Заполняем форму данными активированной политики
                    TitleTextBox.Text = _currentPolicy.Title;
                    ReturnPeriodTextBox.Text = _currentPolicy.ReturnPeriodDays.ToString();
                    GeneralConditionsTextBox.Text = _currentPolicy.GeneralConditions;
                    ExcludedCategoriesTextBox.Text = _currentPolicy.ExcludedCategories;
                    RefundPolicyTextBox.Text = _currentPolicy.RefundPolicy;
                    ExchangePolicyTextBox.Text = _currentPolicy.ExchangePolicy;
                    IsActiveCheckBox.IsChecked = _currentPolicy.IsActive;
                    
                    // Обновляем интерфейс
                    LoadPolicyHistory();
                    
                    StatusTextBlock.Text = $"Политика возврата активирована. ID: {_currentPolicy.Id}";
                    
                    MessageBox.Show("Политика возврата успешно активирована.", 
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при активации политики возврата: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Валидация ввода чисел
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
