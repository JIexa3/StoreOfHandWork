using System.Windows;
using System.Windows.Controls;
using StoreOfHandWork.Models;
using StoreOfHandWork.Pages;
using StoreOfHandWork.Data;
using StoreOfHandWork.Services;
using Microsoft.Extensions.DependencyInjection;

namespace StoreOfHandWork
{
    public partial class AdminWindow : Window
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _context = App.Current.Services.GetService<ApplicationDbContext>();
            _emailService = App.Current.Services.GetService<IEmailService>();
            MainFrame.Navigate(new ProductsManagementPage());
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProductsManagementPage());
        }

        private void CategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CategoriesManagementPage());
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersManagementPage(_context, _emailService));
        }

        private void WarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.WarehousePage());
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersManagementPage());
        }

        private void TagsButton_Click(object sender, RoutedEventArgs e)
        {
            // Загружаем страницу управления тегами в основной фрейм
            MainFrame.Navigate(new Pages.TagsManagementPage());
        }
        
        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new StatisticsPage());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportsPage());
        }

        private void ReturnRequestsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReturnRequestsManagementPage(_context, _emailService));
        }

        private void ReturnPolicyButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReturnPolicyManagementPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
