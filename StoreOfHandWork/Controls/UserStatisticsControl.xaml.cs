using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StoreOfHandWork.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using LiveChartsCore.SkiaSharpView.VisualElements;
using StoreOfHandWork.Data;

namespace StoreOfHandWork.Controls
{
    public partial class UserStatisticsControl : UserControl
    {
        private readonly int _userId;
        private readonly ApplicationDbContext _context;

        public UserStatisticsControl(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _context = new ApplicationDbContext();
            
            ProductsRadio.IsChecked = true;
            Loaded += (s, e) => LoadStatistics();
        }

        public void RefreshData()
        {
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                if (_context == null)
                {
                    MessageBox.Show("Ошибка: контекст базы данных не инициализирован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var orders = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                    .Where(o => o.UserId == _userId && o.Status == OrderStatus.Доставлен)
                    .ToList();

                if (orders == null || !orders.Any())
                {
                    TotalSpentText.Text = "0.00 ₽";
                    StatisticsChart.Series = Array.Empty<ISeries>();
                    return;
                }

                var totalSpent = orders.SelectMany(o => o.OrderItems)
                    .Sum(oi => oi.Quantity * oi.Product.Price);

                TotalSpentText.Text = $"{totalSpent:C}";

                if (ProductsRadio?.IsChecked == true)
                {
                    LoadProductStatistics(orders);
                }
                else
                {
                    LoadCategoryStatistics(orders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductStatistics(List<Order> orders)
        {
            if (orders == null || !orders.Any() || StatisticsChart == null) return;

            var productStats = orders.SelectMany(o => o.OrderItems)
                .Where(oi => oi.Product != null)
                .GroupBy(oi => oi.Product.Name ?? "Без названия")
                .Select(g => new
                {
                    Name = g.Key,
                    Total = (double)(g.Sum(oi => oi.Quantity * oi.Product.Price))
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            if (!productStats.Any())
            {
                StatisticsChart.Series = Array.Empty<ISeries>();
                return;
            }

            var colors = new[]
            {
                new SolidColorPaint(SKColors.DodgerBlue),
                new SolidColorPaint(SKColors.LightSeaGreen),
                new SolidColorPaint(SKColors.Orange),
                new SolidColorPaint(SKColors.Crimson),
                new SolidColorPaint(SKColors.Purple),
                new SolidColorPaint(SKColors.Gold),
                new SolidColorPaint(SKColors.LightBlue),
                new SolidColorPaint(SKColors.LightGreen),
                new SolidColorPaint(SKColors.Pink),
                new SolidColorPaint(SKColors.Gray)
            };

            var series = productStats.Select((item, index) => new PieSeries<double>
            {
                Values = new[] { item.Total },
                Name = item.Name,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = (value) => $"{item.Name}\n{value:C}",
                Fill = colors[index % colors.Length]
            }).ToArray();

            StatisticsChart.Series = series;
            StatisticsChart.Title = new LabelVisual
            {
                Text = "Расходы по товарам",
                TextSize = 18,
                Paint = new SolidColorPaint(SKColors.Black)
            };
        }

        private void LoadCategoryStatistics(List<Order> orders)
        {
            try
            {
                if (orders == null || !orders.Any() || StatisticsChart == null) return;

                var orderItems = orders.SelectMany(o => o.OrderItems).ToList();
                var debug = orderItems.Select(oi => new 
                { 
                    ProductName = oi.Product?.Name ?? "null",
                    CategoryName = oi.Product?.Category?.Name ?? "null",
                    Price = oi.Product?.Price ?? 0,
                    Quantity = oi.Quantity
                }).ToList();

                var categoryStats = orderItems
                    .Where(oi => oi.Product != null)
                    .GroupBy(oi => oi.Product.Category?.Name ?? "Без категории")
                    .Select(g => new
                    {
                        Name = g.Key,
                        Total = (double)(g.Sum(oi => oi.Quantity * oi.Product.Price))
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                if (!categoryStats.Any())
                {
                    MessageBox.Show("Нет данных для отображения статистики по категориям", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatisticsChart.Series = Array.Empty<ISeries>();
                    return;
                }

                var colors = new[]
                {
                    new SolidColorPaint(SKColors.DodgerBlue),
                    new SolidColorPaint(SKColors.LightSeaGreen),
                    new SolidColorPaint(SKColors.Orange),
                    new SolidColorPaint(SKColors.Crimson),
                    new SolidColorPaint(SKColors.Purple)
                };

                var series = categoryStats.Select((item, index) => new PieSeries<double>
                {
                    Values = new[] { item.Total },
                    Name = item.Name,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = (value) => $"{item.Name}\n{value:C}",
                    Fill = colors[index % colors.Length]
                }).ToArray();

                StatisticsChart.Series = series;
                StatisticsChart.Title = new LabelVisual
                {
                    Text = "Расходы по категориям",
                    TextSize = 18,
                    Paint = new SolidColorPaint(SKColors.Black)
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики по категориям: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadStatistics();
            }
        }
    }
}
