using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreOfHandWork.Pages
{
    public partial class ProductsManagementPage : Page
    {
        private readonly ApplicationDbContext _context;
        private ICollectionView _productsView;
        private TextBox searchBox;

        public ProductsManagementPage()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            searchBox = (TextBox)FindName("SearchBox");
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                var products = _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToList();
                ProductsGrid.ItemsSource = products;
                _productsView = CollectionViewSource.GetDefaultView(products);
                _productsView.Filter = ProductFilter;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке товаров: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private bool ProductFilter(object item)
        {
            if (item is Product product)
            {
                var searchText = searchBox.Text?.ToLower() ?? "";
                return string.IsNullOrEmpty(searchText) ||
                       product.Name.ToLower().Contains(searchText) ||
                       product.Description?.ToLower().Contains(searchText) == true;
            }
            return false;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _productsView?.Refresh();
        }

        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var addProductWindow = new Windows.AddProductWindow();
            if (addProductWindow.ShowDialog() == true)
            {
                // Создаем новый продукт без тегов
                var newProduct = new Product
                {
                    Name = addProductWindow.NewProduct.Name,
                    Description = addProductWindow.NewProduct.Description,
                    Price = addProductWindow.NewProduct.Price,
                    CategoryId = addProductWindow.NewProduct.CategoryId,
                    StockQuantity = addProductWindow.NewProduct.StockQuantity,
                    ImagePath = addProductWindow.NewProduct.ImagePath,
                    IsActive = addProductWindow.NewProduct.IsActive,
                    CreatedDate = addProductWindow.NewProduct.CreatedDate
                };

                // Сначала сохраняем продукт в базу данных, чтобы получить ID
                _context.Products.Add(newProduct);
                _context.SaveChanges();
                
                // Теперь добавляем связи с тегами, если они есть
                if (addProductWindow.NewProduct.Tags != null && addProductWindow.NewProduct.Tags.Any())
                {
                    // Получаем все ID тегов, которые нужно добавить
                    var tagIds = addProductWindow.NewProduct.Tags.Select(t => t.Id).ToList();
                    
                    // Загружаем фактические экземпляры тегов из базы данных
                    var tagsFromDb = _context.Tags.Where(t => tagIds.Contains(t.Id)).ToList();
                    
                    // Загружаем продукт снова для избежания проблем с отслеживанием
                    var productFromDb = _context.Products.Find(newProduct.Id);
                    
                    // Добавляем теги к загруженному продукту
                    foreach (var tag in tagsFromDb)
                    {
                        productFromDb.Tags.Add(tag);
                    }
                    
                    // Сохраняем изменения
                    _context.SaveChanges();
                }
                
                LoadProducts();
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Product product)
            {
                var editWindow = new EditProductWindow(product);
                if (editWindow.ShowDialog() == true)
                {
                    LoadProducts(); // Обновляем список товаров после редактирования
                }
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Product product)
            {
                try
                {
                    // Проверяем, есть ли связанные заказы
                    var hasOrders = _context.OrderItems.Any(oi => oi.ProductId == product.Id);
                    var hasCartItems = _context.CartItems.Any(ci => ci.ProductId == product.Id);

                    string warningMessage = $"Вы уверены, что хотите удалить товар '{product.Name}'?";
                    if (hasOrders || hasCartItems)
                    {
                        warningMessage += "\n\nВнимание: Этот товар присутствует в заказах или корзинах пользователей. " +
                                        "При удалении товара эти записи также будут удалены.";
                    }

                    var result = MessageBox.Show(
                        warningMessage,
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        // Загружаем продукт со всеми связанными данными
                        var productToDelete = _context.Products
                            .Include(p => p.Category)
                            .FirstOrDefault(p => p.Id == product.Id);

                        if (productToDelete != null)
                        {
                            // Удаляем связанные записи в корзинах
                            var cartItems = _context.CartItems
                                .Where(ci => ci.ProductId == product.Id);
                            _context.CartItems.RemoveRange(cartItems);

                            // Удаляем связанные записи в заказах
                            var orderItems = _context.OrderItems
                                .Where(oi => oi.ProductId == product.Id);
                            _context.OrderItems.RemoveRange(orderItems);

                            // Удаляем сам продукт
                            _context.Products.Remove(productToDelete);
                            _context.SaveChanges();

                            MessageBox.Show(
                                "Товар успешно удален.",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );

                            LoadProducts();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при удалении товара: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    LoadProducts(); // Перезагружаем на случай частичных изменений
                }
            }
        }
    }
}
