using System;
using System.Linq;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Models;
using System.Collections.Generic;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StoreOfHandWork.Data;

namespace StoreOfHandWork.Pages
{
    public partial class StatisticsPage : Page
    {
        private readonly ApplicationDbContext _context;

        public StatisticsPage()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                // Загружаем все заказы с их элементами
                var orders = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                    .AsNoTracking()
                    .ToList();

                var today = DateTime.Today;
                var weekAgo = today.AddDays(-7);
                var monthAgo = today.AddMonths(-1);

                // Подсчет продаж
                decimal todaySales = orders.Where(o => o.OrderDate.Date == today).Sum(o => o.TotalAmount);
                decimal weekSales = orders.Where(o => o.OrderDate.Date >= weekAgo).Sum(o => o.TotalAmount);
                decimal monthSales = orders.Where(o => o.OrderDate.Date >= monthAgo).Sum(o => o.TotalAmount);

                // Отображение сумм продаж
                TodaySalesText.Text = $"{todaySales:N2} ₽";
                WeekSalesText.Text = $"{weekSales:N2} ₽";
                MonthSalesText.Text = $"{monthSales:N2} ₽";

                // Статистика заказов
                var todayOrders = orders.Count(o => o.OrderDate.Date == today);
                var processingOrders = orders.Count(o => o.Status == OrderStatus.ВОбработке);
                var completedOrders = orders.Count(o => o.Status == OrderStatus.Доставлен && o.OrderDate.Date >= monthAgo);

                TodayOrdersText.Text = todayOrders.ToString();
                ProcessingOrdersText.Text = processingOrders.ToString();
                CompletedOrdersText.Text = completedOrders.ToString();

                // Топ продаж
                var allOrderItems = orders.SelectMany(o => o.OrderItems).ToList();

                var productSales = allOrderItems
                    .GroupBy(oi => new { 
                        oi.Product.Id, 
                        oi.Product.Name, 
                        Category = oi.Product.Category?.Name ?? "Без категории" 
                    })
                    .Select(g => new
                    {
                        Name = g.Key.Name,
                        Category = g.Key.Category,
                        SoldCount = g.Sum(oi => oi.Quantity),
                        TotalAmount = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderByDescending(x => x.SoldCount)
                    .Take(10)
                    .ToList();

                TopSalesGrid.ItemsSource = productSales;

                // Создаем данные для кругового графика
                var colors = new[]
                {
                    new SKColor(45, 85, 255),    // Синий
                    new SKColor(238, 96, 85),    // Красный
                    new SKColor(65, 196, 99),    // Зеленый
                    new SKColor(246, 196, 85),   // Желтый
                    new SKColor(153, 85, 255),   // Фиолетовый
                    new SKColor(85, 196, 196),   // Бирюзовый
                    new SKColor(255, 85, 184),   // Розовый
                    new SKColor(85, 238, 184),   // Мятный
                    new SKColor(255, 153, 85),   // Оранжевый
                    new SKColor(85, 153, 238)    // Голубой
                };

                var pieSeriesValues = productSales.Select((item, index) => new PieSeries<decimal>
                {
                    Values = new decimal[] { item.SoldCount },
                    Name = $"{item.Name} ({item.SoldCount} шт.)",
                    Fill = new SolidColorPaint(colors[index % colors.Length])
                }).ToArray();

                PopularProductsChart.Series = pieSeriesValues;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}
