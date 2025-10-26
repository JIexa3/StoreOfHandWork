using System.Configuration;
using System.Data;
using System.Windows;
using StoreOfHandWork.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using StoreOfHandWork.Services;
using StoreOfHandWork.Pages;

namespace StoreOfHandWork
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        public IConfiguration Configuration { get; private set; }
        public ServiceProvider ServiceProvider => serviceProvider;

        public static new App Current => (App)Application.Current;
        public IServiceProvider Services => ServiceProvider;

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<ApplicationDbContext>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddScoped<ICartService, CartService>();
            
            // Регистрация окон и страниц
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<RegisterWindow>();
            services.AddTransient<EmailVerificationWindow>();
            services.AddTransient<ReturnRequestsManagementPage>();
            services.AddTransient<OrdersManagementPage>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализация базы данных
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                DbInitializer.Initialize(context);
            }

            var loginWindow = serviceProvider.GetService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
