using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using StoreOfHandWork.Models;
using StoreOfHandWork.Data;

namespace StoreOfHandWork
{
    public partial class EditProductWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly Product _product;
        private string _selectedImagePath;
        private bool _isNewImage;

        public EditProductWindow(Product product)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _product = _context.Products.Find(product.Id); // Получаем свежую копию из базы
            LoadCategories();
            LoadProductData();
        }

        private void LoadCategories()
        {
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            CategoryComboBox.ItemsSource = categories;
            CategoryComboBox.DisplayMemberPath = "Name";
            CategoryComboBox.SelectedValuePath = "Id";
        }

        private void LoadProductData()
        {
            NameTextBox.Text = _product.Name;
            DescriptionTextBox.Text = _product.Description;
            PriceTextBox.Text = _product.Price.ToString();
            StockTextBox.Text = _product.StockQuantity.ToString();
            CategoryComboBox.SelectedValue = _product.CategoryId;

            if (!string.IsNullOrEmpty(_product.ImagePath))
            {
                _selectedImagePath = _product.ImagePath;
                ImagePathTextBox.Text = Path.GetFileName(_product.ImagePath);
                LoadProductImage(_product.ImagePath);
            }
        }

        private void LoadProductImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(imagePath);
                    image.EndInit();
                    ProductImage.Source = image;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить изображение: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                _isNewImage = true;
                ImagePathTextBox.Text = Path.GetFileName(_selectedImagePath);
                LoadProductImage(_selectedImagePath);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                _product.Name = NameTextBox.Text.Trim();
                _product.Description = DescriptionTextBox.Text.Trim();
                _product.Price = decimal.Parse(PriceTextBox.Text);
                _product.StockQuantity = int.Parse(StockTextBox.Text);
                _product.CategoryId = (int)CategoryComboBox.SelectedValue;

                if (_isNewImage && !string.IsNullOrEmpty(_selectedImagePath))
                {
                    // Создаем папку Images, если её нет
                    var imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    Directory.CreateDirectory(imagesFolder);

                    // Генерируем уникальное имя файла
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(_selectedImagePath)}";
                    var destinationPath = Path.Combine(imagesFolder, fileName);

                    // Копируем файл
                    File.Copy(_selectedImagePath, destinationPath, true);

                    // Удаляем старый файл, если он существует
                    if (!string.IsNullOrEmpty(_product.ImagePath) && File.Exists(_product.ImagePath))
                    {
                        try
                        {
                            File.Delete(_product.ImagePath);
                        }
                        catch
                        {
                            // Игнорируем ошибки при удалении старого файла
                        }
                    }

                    // Обновляем путь к изображению в продукте
                    _product.ImagePath = destinationPath;
                }

                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении товара: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название товара", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(StockTextBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Введите корректное количество товара", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите категорию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
