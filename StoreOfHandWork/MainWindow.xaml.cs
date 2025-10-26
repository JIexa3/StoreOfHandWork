using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private ObservableCollection<Product> _products;
        private int _cartItemCount;
        private ApplicationDbContext _context;
        
        // Поля для поиска и сортировки
        private string _searchText = "";
        private int _searchTypeIndex = 0; // 0 - по названию, 1 - по тегам
        private int _sortTypeIndex = 0; // 0 - без сортировки, 1 - цена (возр), 2 - цена (убыв), 3 - кол-во (возр), 4 - кол-во (убыв)
        private int? _currentCategoryId = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public int CartItemCount
        {
            get => _cartItemCount;
            set
            {
                if (_cartItemCount != value)
                {
                    _cartItemCount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CartItemCount)));
                }
            }
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _context = new ApplicationDbContext();
            DataContext = this;
            
            // Загружаем данные после инициализации окна
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCategories();
                LoadAllProducts();
                UpdateCartItemCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateCartItemCount()
        {
            if (_currentUser != null && _context != null)
            {
                CartItemCount = _context.CartItems.Count(ci => ci.UserId == _currentUser.Id);
            }
        }

        public void UpdateCartCount(int count)
        {
            CartItemCountTextBlock.Text = count > 0 ? count.ToString() : "";
        }

        private void LoadCategories()
        {
            if (CategoriesListBox == null) return;

            CategoriesListBox.Items.Clear();
            var categories = _context.Categories.ToList();
            
            // Добавляем "Все товары" как первый элемент
            var allProductsItem = new ListBoxItem { Content = "Все товары" };
            CategoriesListBox.Items.Add(allProductsItem);
            
            // Добавляем остальные категории
            foreach (var category in categories)
            {
                CategoriesListBox.Items.Add(new ListBoxItem { Content = category.Name, Tag = category });
            }
            
            // Выбираем "Все товары" по умолчанию
            CategoriesListBox.SelectedItem = allProductsItem;
        }

        private void CategoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesListBox?.SelectedItem is ListBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Все товары")
                {
                    LoadAllProducts();
                }
                else if (selectedItem.Tag is Category category)
                {
                    LoadProductsByCategory(category.Id);
                }
            }
        }
        
        private void ApplyFiltersAndLoad()
        {
            if (ProductsListView == null) return;

            try
            {
                // Очищаем текущий контекст и создаем новый для получения свежих данных
                _context.Dispose();
                using (var newContext = new ApplicationDbContext())
                {
                    // Начинаем с базового запроса
                    IQueryable<Product> query = newContext.Products
                        .Include(p => p.Category);
                    
                    // Применяем фильтр по категории, если выбрана
                    if (_currentCategoryId.HasValue)
                    {
                        query = query.Where(p => p.CategoryId == _currentCategoryId.Value);
                    }
                    
                    // Применяем поиск по названию или тегам
                    if (!string.IsNullOrWhiteSpace(_searchText))
                    {
                        string searchLower = _searchText.ToLower();
                        
                        if (_searchTypeIndex == 0) // Поиск по названию
                        {
                            query = query.Where(p => p.Name.ToLower().Contains(searchLower) || 
                                                  (p.Description != null && p.Description.ToLower().Contains(searchLower)));
                        }
                        else // Поиск по тегам
                        {
                            // Загружаем теги для продуктов
                            query = query.Include(p => p.Tags);
                            
                            // Применяем фильтр по тегам
                            query = query.Where(p => p.Tags.Any(t => t.Name.ToLower().Contains(searchLower)));
                        }
                    }
                    else if (_searchTypeIndex == 1) // Если выбран поиск по тегам, загружаем теги даже без текста поиска
                    {
                        query = query.Include(p => p.Tags);
                    }
                    
                    // Применяем сортировку
                    switch (_sortTypeIndex)
                    {
                        case 1: // По цене (возрастание)
                            query = query.OrderBy(p => p.Price);
                            break;
                        case 2: // По цене (убывание)
                            query = query.OrderByDescending(p => p.Price);
                            break;
                        case 3: // По количеству (возрастание)
                            query = query.OrderBy(p => p.StockQuantity);
                            break;
                        case 4: // По количеству (убывание)
                            query = query.OrderByDescending(p => p.StockQuantity);
                            break;
                        default: // Без сортировки или сортировка по умолчанию (по имени)
                            query = query.OrderBy(p => p.Name);
                            break;
                    }
                    
                    // Выполняем запрос и получаем результаты
                    var products = query.ToList();
                    
                    // Обновляем UI
                    ProductsListView.ItemsSource = products;
                }
                _context = new ApplicationDbContext();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAllProducts()
        {
            _currentCategoryId = null;
            ApplyFiltersAndLoad();
        }

        private void LoadProductsByCategory(int categoryId)
        {
            _currentCategoryId = categoryId;
            ApplyFiltersAndLoad();
        }



        public void RefreshProducts()
        {
            if (_currentCategoryId.HasValue)
            {
                LoadProductsByCategory(_currentCategoryId.Value);
            }
            else
            {
                LoadAllProducts();
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product)
            {
                if (product.StockQuantity <= 0)
                {
                    MessageBox.Show("Этого товара нет в наличии", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    // Проверяем, есть ли уже такой товар в корзине
                    var existingCartItem = _context.CartItems
                        .FirstOrDefault(ci => ci.UserId == _currentUser.Id && ci.ProductId == product.Id);

                    // Обновляем информацию о товаре
                    var currentProduct = _context.Products.Find(product.Id);
                    if (currentProduct == null)
                    {
                        MessageBox.Show("Товар не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (existingCartItem != null)
                    {
                        // Проверяем, не превысит ли новое количество доступное на складе
                        if (existingCartItem.Quantity + 1 > currentProduct.StockQuantity)
                        {
                            MessageBox.Show($"Невозможно добавить товар. В корзине уже {existingCartItem.Quantity} шт., на складе осталось {currentProduct.StockQuantity} шт.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        
                        // Если товар уже в корзине, увеличиваем количество
                        existingCartItem.Quantity++;
                    }
                    else
                    {
                        // Если товара нет в корзине, добавляем новую запись
                        var cartItem = new CartItem
                        {
                            UserId = _currentUser.Id,
                            ProductId = product.Id,
                            Quantity = 1,
                            DateAdded = DateTime.Now
                        };
                        _context.CartItems.Add(cartItem);
                    }

                    _context.SaveChanges();
                    UpdateCartItemCount();
                    MessageBox.Show("Товар добавлен в корзину", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении товара в корзину: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cartWindow = new CartWindow(_currentUser, this);
                cartWindow.ShowDialog();
                RefreshProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии корзины: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profileWindow = new ProfileWindow(_currentUser);
                profileWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewProductDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product)
            {
                try
                {
                    var productDetailsWindow = new ProductDetailsWindow(product, _currentUser, _context);
                    productDetailsWindow.ShowDialog();
                    RefreshProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии деталей товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void WishList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем фрейм для отображения страницы
                var frame = new Frame();
                // Создаем Window для отображения фрейма
                var wishlistWindow = new Window
                {
                    Title = "Список желаний",
                    Content = frame,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                
                // Загружаем страницу WishListPage в фрейм
                frame.Content = new Pages.WishListPage(_context, _currentUser);
                
                wishlistWindow.ShowDialog();
                RefreshProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии списка желаний: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Tags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, является ли пользователь администратором
                if (!_currentUser.IsAdmin)
                {
                    MessageBox.Show("Для управления тегами требуются права администратора", "Доступ запрещен", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Открываем окно управления тегами
                var tagsWindow = new TagsManagementWindow(_context);
                tagsWindow.ShowDialog();
                
                // После закрытия окна обновляем список товаров
                RefreshProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии управления тегами: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики событий для поиска и сортировки
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            ApplyFiltersAndLoad();
        }
        
        private void SearchTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTypeComboBox != null)
            {
                _searchTypeIndex = SearchTypeComboBox.SelectedIndex;
                ApplyFiltersAndLoad();
            }
        }
        
        private void SortByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortByComboBox != null)
            {
                _sortTypeIndex = SortByComboBox.SelectedIndex;
                ApplyFiltersAndLoad();
            }
        }
        
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            _searchText = string.Empty;
            SearchTypeComboBox.SelectedIndex = 0;
            _searchTypeIndex = 0;
            ApplyFiltersAndLoad();
        }
        
        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            ApplyFiltersAndLoad();
        }
    }
}