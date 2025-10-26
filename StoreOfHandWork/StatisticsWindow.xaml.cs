using System.Linq;
using System.Windows;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;

namespace StoreOfHandWork
{
    public partial class StatisticsWindow : Window
    {
        private static readonly CultureInfo RuCulture = new CultureInfo("ru-RU");

        public StatisticsWindow()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            using (var context = new ApplicationDbContext())
            {
                // Загрузка основных показателей
                var totalOrders = context.Orders.Count();
                var totalSales = context.Orders.Sum(o => o.TotalAmount);
                var activeUsers = context.Users.Count();

                TotalOrdersText.Text = totalOrders.ToString();
                TotalSalesText.Text = totalSales.ToString("C", RuCulture);
                ActiveUsersText.Text = activeUsers.ToString();

                // Загрузка популярных товаров
                var topProducts = context.Products
                    .Include(p => p.Category)
                    .Include(p => p.OrderItems)
                    .Select(p => new
                    {
                        p.Name,
                        Category = p.Category,
                        SalesCount = p.OrderItems.Sum(oi => oi.Quantity),
                        TotalSales = p.OrderItems.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .Where(p => p.SalesCount > 0)
                    .OrderByDescending(x => x.SalesCount)
                    .Take(5)
                    .ToList();

                TopProductsGrid.ItemsSource = topProducts;

                // Загрузка статистики по категориям
                var categoryStats = context.Categories
                    .Include(c => c.Products)
                    .ThenInclude(p => p.OrderItems)
                    .Select(c => new
                    {
                        c.Name,
                        ProductCount = c.Products.Count,
                        SalesCount = c.Products.SelectMany(p => p.OrderItems).Sum(oi => oi.Quantity),
                        TotalSales = c.Products.SelectMany(p => p.OrderItems).Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToList();

                CategoryStatsGrid.ItemsSource = categoryStats;
            }
        }
    }
}
