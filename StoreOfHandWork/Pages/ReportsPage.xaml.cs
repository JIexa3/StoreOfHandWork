using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using Xceed.Words.NET;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Страница для формирования отчетов в формате Word
    /// </summary>
    public partial class ReportsPage : Page
    {
        private ApplicationDbContext? _context;
        private DatePicker? _startDatePicker;
        private DatePicker? _endDatePicker;
        private CheckBox? _includeCategoriesCheckBox;
        private CheckBox? _includeStockCheckBox;
        private CheckBox? _includeCustomerInfoCheckBox;
        private CheckBox? _includeOrderDetailsCheckBox;
        private TextBlock? _statusTextBlock;
        
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Now;

        public ReportsPage()
        {
            try
            {
                // Инициализируем контекст базы данных
                _context = new ApplicationDbContext();
                
                // Используем рефлексию для вызова InitializeComponent
                var method = GetType().GetMethod("InitializeComponent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
                
                // Программно создаем основной интерфейс, если загрузка из XAML не удалась
                CreateUIElements();
                
                DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации страницы отчетов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CreateUIElements()
        {
            // Создаем основной Grid для страницы
            Grid mainGrid = new Grid { Background = System.Windows.Media.Brushes.White };
            this.Content = mainGrid;
            
            // Создаем ScrollViewer для прокрутки
            ScrollViewer scrollViewer = new ScrollViewer();
            mainGrid.Children.Add(scrollViewer);
            
            // Создаем основной StackPanel для содержимого
            StackPanel mainPanel = new StackPanel { Margin = new Thickness(20) };
            scrollViewer.Content = mainPanel;
            
            // Добавляем заголовок
            TextBlock headerText = new TextBlock
            {
                Text = "Генерация отчетов",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(headerText);
            
            // Создаем компоненты для формы отчета по товарам
            GroupBox productsReportGroup = new GroupBox { Header = "Отчет по товарам", Margin = new Thickness(0, 0, 0, 20), Padding = new Thickness(10) };
            mainPanel.Children.Add(productsReportGroup);
            
            StackPanel productsPanel = new StackPanel();
            productsReportGroup.Content = productsPanel;
            
            TextBlock productsText = new TextBlock
            {
                Text = "Формирование отчета по всем товарам в базе данных",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            productsPanel.Children.Add(productsText);
            
            // Добавляем чекбоксы
            _includeCategoriesCheckBox = new CheckBox { Content = "Включить информацию о категориях", IsChecked = true, Margin = new Thickness(0, 0, 0, 5) };
            productsPanel.Children.Add(_includeCategoriesCheckBox);
            
            _includeStockCheckBox = new CheckBox { Content = "Включить информацию о наличии", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
            productsPanel.Children.Add(_includeStockCheckBox);
            
            // Создаем кнопку для генерации отчета по товарам
            Button generateProductsReportButton = new Button
            {
                Content = "Сформировать отчет по товарам",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498db")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Width = 264,
                Height = 25
            };
            generateProductsReportButton.Click += GenerateProductsReportButton_Click;
            productsPanel.Children.Add(generateProductsReportButton);
            
            // Создаем компоненты для формы отчета по заказам
            GroupBox ordersReportGroup = new GroupBox { Header = "Отчет по заказам", Margin = new Thickness(0, 0, 0, 20), Padding = new Thickness(10) };
            mainPanel.Children.Add(ordersReportGroup);
            
            StackPanel ordersPanel = new StackPanel();
            ordersReportGroup.Content = ordersPanel;
            
            TextBlock ordersText = new TextBlock
            {
                Text = "Формирование отчета по заказам за выбранный период",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            ordersPanel.Children.Add(ordersText);
            
            // Создаем грид для выбора даты
            Grid dateGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            ordersPanel.Children.Add(dateGrid);
            
            // Настраиваем колонки
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Добавляем метки и выбор дат
            TextBlock fromLabel = new TextBlock { Text = "Период с:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            Grid.SetColumn(fromLabel, 0);
            dateGrid.Children.Add(fromLabel);
            
            _startDatePicker = new DatePicker { SelectedDate = DateTime.Now.AddMonths(-1) };
            Grid.SetColumn(_startDatePicker, 1);
            dateGrid.Children.Add(_startDatePicker);
            
            TextBlock toLabel = new TextBlock { Text = "По:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            Grid.SetColumn(toLabel, 2);
            dateGrid.Children.Add(toLabel);
            
            _endDatePicker = new DatePicker { SelectedDate = DateTime.Now };
            Grid.SetColumn(_endDatePicker, 3);
            dateGrid.Children.Add(_endDatePicker);
            
            // Добавляем чекбоксы для опций
            DockPanel optionsPanel = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 10, 0, 10) };
            ordersPanel.Children.Add(optionsPanel);
            
            _includeCustomerInfoCheckBox = new CheckBox { Content = "Включить информацию о клиентах", IsChecked = true, Margin = new Thickness(0, 0, 20, 0) };
            DockPanel.SetDock(_includeCustomerInfoCheckBox, Dock.Left);
            optionsPanel.Children.Add(_includeCustomerInfoCheckBox);
            
            _includeOrderDetailsCheckBox = new CheckBox { Content = "Включить детали заказов", IsChecked = true };
            DockPanel.SetDock(_includeOrderDetailsCheckBox, Dock.Left);
            optionsPanel.Children.Add(_includeOrderDetailsCheckBox);
            
            // Создаем кнопку для генерации отчета по заказам
            Button generateOrdersReportButton = new Button
            {
                Content = "Сформировать отчет по заказам",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498db")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Width = 264,
                Height = 25
            };
            generateOrdersReportButton.Click += GenerateOrdersReportButton_Click;
            ordersPanel.Children.Add(generateOrdersReportButton);
            
            // Создаем компоненты для формы отчета по статистике продаж
            GroupBox salesStatisticsReportGroup = new GroupBox { Header = "Отчет по статистике продаж", Margin = new Thickness(0, 0, 0, 20), Padding = new Thickness(10) };
            mainPanel.Children.Add(salesStatisticsReportGroup);
            
            StackPanel salesStatisticsPanel = new StackPanel();
            salesStatisticsReportGroup.Content = salesStatisticsPanel;
            
            TextBlock salesStatisticsText = new TextBlock
            {
                Text = "Формирование отчета по статистике продаж за выбранный период",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            salesStatisticsPanel.Children.Add(salesStatisticsText);
            
            // Создаем кнопку для генерации отчета по статистике продаж
            Button generateSalesStatisticsReportButton = new Button
            {
                Content = "Сформировать отчет по статистике продаж",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498db")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Width = 264,
                Height = 25
            };
            generateSalesStatisticsReportButton.Click += GenerateSalesStatisticsReportButton_Click;
            salesStatisticsPanel.Children.Add(generateSalesStatisticsReportButton);
            
            // Добавляем текстовый блок для отображения статуса
            _statusTextBlock = new TextBlock
            {
                Foreground = System.Windows.Media.Brushes.Green,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };
            mainPanel.Children.Add(_statusTextBlock);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Инициализируем контекст базы данных
                _context = new ApplicationDbContext();
                
                // Находим элементы управления на странице
                _startDatePicker = FindName("StartDatePicker") as DatePicker;
                _endDatePicker = FindName("EndDatePicker") as DatePicker;
                _includeCategoriesCheckBox = FindName("IncludeCategoriesCheckBox") as CheckBox;
                _includeStockCheckBox = FindName("IncludeStockCheckBox") as CheckBox;
                _includeCustomerInfoCheckBox = FindName("IncludeCustomerInfoCheckBox") as CheckBox;
                _includeOrderDetailsCheckBox = FindName("IncludeOrderDetailsCheckBox") as CheckBox;
                _statusTextBlock = FindName("StatusTextBlock") as TextBlock;
                
                // Устанавливаем начальные значения
                if (_startDatePicker != null)
                    _startDatePicker.SelectedDate = StartDate;
                    
                if (_endDatePicker != null)
                    _endDatePicker.SelectedDate = EndDate;
                    
                // Настраиваем обработчики событий для кнопок
                if (FindName("GenerateProductsReportButton") is Button productsReportButton)
                    productsReportButton.Click += GenerateProductsReportButton_Click;
                    
                if (FindName("GenerateOrdersReportButton") is Button ordersReportButton)
                    ordersReportButton.Click += GenerateOrdersReportButton_Click;
                    
                if (FindName("GenerateSalesStatisticsReportButton") is Button salesReportButton)
                    salesReportButton.Click += GenerateSalesStatisticsReportButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации страницы отчетов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateProductsReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context == null) 
            {
                MessageBox.Show("Ошибка доступа к базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    DefaultExt = "docx",
                    FileName = $"Отчет_по_товарам_{DateTime.Now:yyyy-MM-dd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateProductsReport(saveFileDialog.FileName);
                    ShowStatusMessage($"Отчет успешно сохранен: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateOrdersReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context == null)
            {
                MessageBox.Show("Ошибка доступа к базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    DefaultExt = "docx",
                    FileName = $"Отчет_по_заказам_{DateTime.Now:yyyy-MM-dd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var startDate = _startDatePicker?.SelectedDate ?? DateTime.Now.AddMonths(-1);
                    var endDate = _endDatePicker?.SelectedDate ?? DateTime.Now;
                    
                    GenerateOrdersReport(saveFileDialog.FileName, startDate, endDate);
                    ShowStatusMessage($"Отчет успешно сохранен: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateSalesStatisticsReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context == null)
            {
                MessageBox.Show("Ошибка доступа к базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    DefaultExt = "docx",
                    FileName = $"Отчет_по_статистике_продаж_{DateTime.Now:yyyy-MM-dd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var startDate = _startDatePicker?.SelectedDate ?? DateTime.Now.AddMonths(-1);
                    var endDate = _endDatePicker?.SelectedDate ?? DateTime.Now;
                    
                    GenerateSalesStatisticsReport(saveFileDialog.FileName, startDate, endDate);
                    ShowStatusMessage($"Отчет успешно сохранен: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateProductsReport(string fileName)
        {
            // Создаем новый документ Word
            using (var document = DocX.Create(fileName))
            {
                // Добавляем заголовок
                var title = document.InsertParagraph();
                title.Append("Отчет по товарам магазина рукоделия")
                     .FontSize(16)
                     .Bold()
                     .Alignment = Xceed.Document.NET.Alignment.center;

                // Добавляем информацию о дате отчета
                var dateInfo = document.InsertParagraph();
                dateInfo.Append($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(12)
                        .Alignment = Xceed.Document.NET.Alignment.right;

                document.InsertParagraph().Append("\n");

                // Получаем данные о товарах
                var products = _context.Products
                                     .Include(p => p.Category)
                                     .OrderBy(p => p.CategoryId)
                                     .ThenBy(p => p.Name)
                                     .ToList();

                // Добавляем информацию о количестве товаров
                var productCountInfo = document.InsertParagraph();
                productCountInfo.Append($"Общее количество товаров: {products.Count}")
                               .FontSize(12);

                document.InsertParagraph().Append("\n");

                // Создаем таблицу для товаров
                var includeCategoriesInfo = _includeCategoriesCheckBox?.IsChecked == true;
                var includeStockInfo = _includeStockCheckBox?.IsChecked == true;

                // Определяем количество столбцов в зависимости от выбранных опций
                int columnsCount = 2; // ID и Наименование обязательны
                if (includeCategoriesInfo) columnsCount++;
                if (includeStockInfo) columnsCount++;
                columnsCount++; // Для цены

                var productsTable = document.AddTable(products.Count + 1, columnsCount);
                productsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                // Заголовки таблицы
                int columnIndex = 0;
                productsTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("ID").Bold();
                productsTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Наименование").Bold();
                if (includeCategoriesInfo)
                    productsTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Категория").Bold();
                if (includeStockInfo)
                    productsTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Наличие").Bold();
                productsTable.Rows[0].Cells[columnIndex].Paragraphs[0].Append("Цена").Bold();

                // Заполняем таблицу данными
                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    columnIndex = 0;
                    productsTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(product.Id.ToString());
                    productsTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(product.Name);
                    if (includeCategoriesInfo)
                        productsTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(product.Category?.Name ?? "Нет категории");
                    if (includeStockInfo)
                        productsTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(product.StockQuantity.ToString());
                    productsTable.Rows[i + 1].Cells[columnIndex].Paragraphs[0].Append($"{product.Price:C}");
                }

                // Добавляем таблицу в документ
                document.InsertTable(productsTable);

                // Добавляем подпись в конце документа
                document.InsertParagraph().Append("\n");
                var signature = document.InsertParagraph();
                signature.Append("Отчет сформирован автоматически системой управления магазина рукоделия")
                         .FontSize(10)
                         .Italic()
                         .Alignment = Xceed.Document.NET.Alignment.right;

                // Сохраняем документ
                document.Save();
            }
        }

        private void GenerateOrdersReport(string fileName, DateTime startDate, DateTime endDate)
        {
            // Создаем новый документ Word
            using (var document = DocX.Create(fileName))
            {
                // Добавляем заголовок
                var title = document.InsertParagraph();
                title.Append("Отчет по заказам магазина рукоделия")
                     .FontSize(16)
                     .Bold()
                     .Alignment = Xceed.Document.NET.Alignment.center;

                // Добавляем информацию о периоде и дате отчета
                var periodInfo = document.InsertParagraph();
                periodInfo.Append($"Период: с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                          .FontSize(12)
                          .Alignment = Xceed.Document.NET.Alignment.left;

                var dateInfo = document.InsertParagraph();
                dateInfo.Append($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(12)
                        .Alignment = Xceed.Document.NET.Alignment.right;

                document.InsertParagraph().Append("\n");

                // Получаем данные о заказах за выбранный период
                var orders = _context.Orders
                                 .Include(o => o.User)
                                 .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                 .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate.AddDays(1).AddSeconds(-1))
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

                // Добавляем информацию о количестве заказов
                var orderCountInfo = document.InsertParagraph();
                orderCountInfo.Append($"Общее количество заказов за период: {orders.Count}")
                              .FontSize(12);

                var totalAmount = orders.Sum(o => o.TotalAmount);
                var totalAmountInfo = document.InsertParagraph();
                totalAmountInfo.Append($"Общая сумма заказов за период: {totalAmount:C}")
                               .FontSize(12)
                               .Bold();

                document.InsertParagraph().Append("\n");

                // Создаем таблицу для заказов
                var includeCustomerInfo = _includeCustomerInfoCheckBox?.IsChecked == true;
                var includeOrderDetails = _includeOrderDetailsCheckBox?.IsChecked == true;

                // Определяем количество столбцов в зависимости от выбранных опций
                int columnsCount = 4; // ID, Дата, Статус, Сумма обязательны
                if (includeCustomerInfo) columnsCount += 2; // Имя клиента и Email

                var ordersTable = document.AddTable(orders.Count + 1, columnsCount);
                ordersTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                // Заголовки таблицы
                int columnIndex = 0;
                ordersTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("ID").Bold();
                ordersTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Дата заказа").Bold();
                if (includeCustomerInfo)
                {
                    ordersTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Клиент").Bold();
                    ordersTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Email").Bold();
                }
                ordersTable.Rows[0].Cells[columnIndex++].Paragraphs[0].Append("Статус").Bold();
                ordersTable.Rows[0].Cells[columnIndex].Paragraphs[0].Append("Сумма").Bold();

                // Заполняем таблицу данными
                for (int i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    columnIndex = 0;
                    ordersTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(order.Id.ToString());
                    ordersTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
                    if (includeCustomerInfo)
                    {
                        ordersTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(order.User?.Name ?? "Неизвестно");
                        ordersTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(order.User?.Email ?? "Неизвестно");
                    }
                    ordersTable.Rows[i + 1].Cells[columnIndex++].Paragraphs[0].Append(GetOrderStatusText(order.Status));
                    ordersTable.Rows[i + 1].Cells[columnIndex].Paragraphs[0].Append($"{order.TotalAmount:C}");
                }

                // Добавляем таблицу в документ
                document.InsertTable(ordersTable);

                // Если нужно включить детали заказов
                if (includeOrderDetails && orders.Any())
                {
                    document.InsertParagraph().Append("\n");
                    var detailsHeader = document.InsertParagraph();
                    detailsHeader.Append("Детали заказов")
                                .FontSize(14)
                                .Bold();

                    foreach (var order in orders)
                    {
                        document.InsertParagraph().Append("\n");
                        var orderHeader = document.InsertParagraph();
                        orderHeader.Append($"Заказ #{order.Id} от {order.OrderDate:dd.MM.yyyy HH:mm}")
                                  .FontSize(12)
                                  .Bold();

                        if (order.OrderItems.Any())
                        {
                            // Создаем таблицу для товаров в заказе
                            var orderItemsTable = document.AddTable(order.OrderItems.Count + 1, 4);
                            orderItemsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                            // Заголовки таблицы
                            orderItemsTable.Rows[0].Cells[0].Paragraphs[0].Append("Товар").Bold();
                            orderItemsTable.Rows[0].Cells[1].Paragraphs[0].Append("Цена").Bold();
                            orderItemsTable.Rows[0].Cells[2].Paragraphs[0].Append("Количество").Bold();
                            orderItemsTable.Rows[0].Cells[3].Paragraphs[0].Append("Сумма").Bold();

                            // Заполняем таблицу данными
                            for (int i = 0; i < order.OrderItems.Count; i++)
                            {
                                var item = order.OrderItems.ElementAt(i);
                                orderItemsTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(item.Product?.Name ?? "Неизвестный товар");
                                orderItemsTable.Rows[i + 1].Cells[1].Paragraphs[0].Append($"{item.Price:C}");
                                orderItemsTable.Rows[i + 1].Cells[2].Paragraphs[0].Append(item.Quantity.ToString());
                                orderItemsTable.Rows[i + 1].Cells[3].Paragraphs[0].Append($"{(item.Price * item.Quantity):C}");
                            }

                            // Добавляем таблицу в документ
                            document.InsertTable(orderItemsTable);
                        }
                        else
                        {
                            var noItemsInfo = document.InsertParagraph();
                            noItemsInfo.Append("Нет товаров в заказе")
                                      .Italic();
                        }
                    }
                }

                // Добавляем подпись в конце документа
                document.InsertParagraph().Append("\n");
                var signature = document.InsertParagraph();
                signature.Append("Отчет сформирован автоматически системой управления магазина рукоделия")
                         .FontSize(10)
                         .Italic()
                         .Alignment = Xceed.Document.NET.Alignment.right;

                // Сохраняем документ
                document.Save();
            }
        }

        private void GenerateSalesStatisticsReport(string fileName, DateTime startDate, DateTime endDate)
        {
            // Создаем новый документ Word
            using (var document = DocX.Create(fileName))
            {
                // Добавляем заголовок
                var title = document.InsertParagraph();
                title.Append("Отчет по статистике продаж магазина рукоделия")
                     .FontSize(16)
                     .Bold()
                     .Alignment = Xceed.Document.NET.Alignment.center;

                // Добавляем информацию о периоде и дате отчета
                var periodInfo = document.InsertParagraph();
                periodInfo.Append($"Период: с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                          .FontSize(12)
                          .Alignment = Xceed.Document.NET.Alignment.left;

                var dateInfo = document.InsertParagraph();
                dateInfo.Append($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(12)
                        .Alignment = Xceed.Document.NET.Alignment.right;

                document.InsertParagraph().Append("\n");

                // Получаем данные о заказах за выбранный период
                var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                        .ThenInclude(p => p.Category)
                                 .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate.AddDays(1).AddSeconds(-1))
                                 .ToList();

                // Общая статистика
                var totalOrdersCount = orders.Count;
                var totalSalesAmount = orders.Sum(o => o.TotalAmount);
                var completedOrdersCount = orders.Count(o => o.Status == OrderStatus.Доставлен);
                var averageOrderAmount = totalOrdersCount > 0 ? totalSalesAmount / totalOrdersCount : 0;

                var generalStatsHeader = document.InsertParagraph();
                generalStatsHeader.Append("Общая статистика")
                                 .FontSize(14)
                                 .Bold();

                var statsTable = document.AddTable(5, 2);
                statsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                statsTable.Rows[0].Cells[0].Paragraphs[0].Append("Показатель").Bold();
                statsTable.Rows[0].Cells[1].Paragraphs[0].Append("Значение").Bold();

                statsTable.Rows[1].Cells[0].Paragraphs[0].Append("Общее количество заказов");
                statsTable.Rows[1].Cells[1].Paragraphs[0].Append(totalOrdersCount.ToString());

                statsTable.Rows[2].Cells[0].Paragraphs[0].Append("Общая сумма продаж");
                statsTable.Rows[2].Cells[1].Paragraphs[0].Append($"{totalSalesAmount:C}");

                statsTable.Rows[3].Cells[0].Paragraphs[0].Append("Количество выполненных заказов");
                statsTable.Rows[3].Cells[1].Paragraphs[0].Append(completedOrdersCount.ToString());

                statsTable.Rows[4].Cells[0].Paragraphs[0].Append("Средняя сумма заказа");
                statsTable.Rows[4].Cells[1].Paragraphs[0].Append($"{averageOrderAmount:C}");

                document.InsertTable(statsTable);
                document.InsertParagraph().Append("\n");

                // Статистика по категориям
                var categorySales = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(oi => oi.Price * oi.Quantity),
                        ItemsCount = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                if (categorySales.Any())
                {
                    var categoryStatsHeader = document.InsertParagraph();
                    categoryStatsHeader.Append("Продажи по категориям")
                                      .FontSize(14)
                                      .Bold();

                    var categoryTable = document.AddTable(categorySales.Count + 1, 3);
                    categoryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                    categoryTable.Rows[0].Cells[0].Paragraphs[0].Append("Категория").Bold();
                    categoryTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество проданных товаров").Bold();
                    categoryTable.Rows[0].Cells[2].Paragraphs[0].Append("Сумма продаж").Bold();

                    for (int i = 0; i < categorySales.Count; i++)
                    {
                        var sale = categorySales[i];
                        categoryTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(sale.Category?.Name ?? "Без категории");
                        categoryTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(sale.ItemsCount.ToString());
                        categoryTable.Rows[i + 1].Cells[2].Paragraphs[0].Append($"{sale.TotalAmount:C}");
                    }

                    document.InsertTable(categoryTable);
                    document.InsertParagraph().Append("\n");
                }

                // Топ-10 продаваемых товаров
                var topProducts = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product)
                    .Select(g => new
                    {
                        Product = g.Key,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalAmount = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToList();

                if (topProducts.Any())
                {
                    var topProductsHeader = document.InsertParagraph();
                    topProductsHeader.Append("Топ-10 продаваемых товаров")
                                    .FontSize(14)
                                    .Bold();

                    var topProductsTable = document.AddTable(topProducts.Count + 1, 4);
                    topProductsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;

                    topProductsTable.Rows[0].Cells[0].Paragraphs[0].Append("Товар").Bold();
                    topProductsTable.Rows[0].Cells[1].Paragraphs[0].Append("Категория").Bold();
                    topProductsTable.Rows[0].Cells[2].Paragraphs[0].Append("Количество продаж").Bold();
                    topProductsTable.Rows[0].Cells[3].Paragraphs[0].Append("Сумма продаж").Bold();

                    for (int i = 0; i < topProducts.Count; i++)
                    {
                        var product = topProducts[i];
                        topProductsTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(product.Product?.Name ?? "Неизвестный товар");
                        topProductsTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(product.Product?.Category?.Name ?? "Без категории");
                        topProductsTable.Rows[i + 1].Cells[2].Paragraphs[0].Append(product.TotalQuantity.ToString());
                        topProductsTable.Rows[i + 1].Cells[3].Paragraphs[0].Append($"{product.TotalAmount:C}");
                    }

                    document.InsertTable(topProductsTable);
                }

                // Добавляем подпись в конце документа
                document.InsertParagraph().Append("\n");
                var signature = document.InsertParagraph();
                signature.Append("Отчет сформирован автоматически системой управления магазина рукоделия")
                         .FontSize(10)
                         .Italic()
                         .Alignment = Xceed.Document.NET.Alignment.right;

                // Сохраняем документ
                document.Save();
            }
        }

        private string GetOrderStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Новый => "Новый",
                OrderStatus.ВОбработке => "В обработке",
                OrderStatus.Отправлен => "Отправлен",
                OrderStatus.Доставлен => "Доставлен",
                OrderStatus.Отменен => "Отменен",
                _ => "Неизвестно"
            };
        }

        private void ShowStatusMessage(string message)
        {
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = message;
                _statusTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
