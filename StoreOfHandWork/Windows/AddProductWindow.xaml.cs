using Microsoft.Win32;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreOfHandWork.Windows
{
    public partial class AddProductWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private static readonly Regex _numberRegex = new Regex("[^0-9,.]");
        public Product NewProduct { get; private set; }
        private ObservableCollection<Tag> _selectedTags = new ObservableCollection<Tag>();

        public AddProductWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            LoadCategories();
            SelectedTagsListBox.ItemsSource = _selectedTags;
        }

        private void LoadCategories()
        {
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            CategoryComboBox.ItemsSource = categories;
            if (categories.Any())
            {
                CategoryComboBox.SelectedIndex = 0;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _numberRegex.IsMatch(e.Text);
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*",
                Title = "Выберите изображение товара"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название товара", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text?.Replace(',', '.'), out decimal price))
            {
                MessageBox.Show("Пожалуйста, введите корректную цену", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(StockQuantityTextBox.Text, out int stockQuantity))
            {
                MessageBox.Show("Пожалуйста, введите корректное количество", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCategory = CategoryComboBox.SelectedItem as Category;
            if (selectedCategory == null)
            {
                MessageBox.Show("Пожалуйста, выберите категорию", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewProduct = new Product
            {
                Name = NameTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text?.Trim(),
                Price = price,
                CategoryId = selectedCategory.Id,
                StockQuantity = stockQuantity,
                ImagePath = string.IsNullOrEmpty(ImagePathTextBox.Text) ? 
                    "/Images/default-product.jpg" : ImagePathTextBox.Text,
                IsActive = IsActiveCheckBox.IsChecked ?? true,
                CreatedDate = DateTime.Now
            };
            
            // Добавляем выбранные теги к товару
            foreach (var tag in _selectedTags)
            {
                NewProduct.Tags.Add(tag);
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            // Создаем окно выбора тегов
            var tagSelectionWindow = new TagSelectionWindow(_context, _selectedTags.ToList());
            if (tagSelectionWindow.ShowDialog() == true && tagSelectionWindow.SelectedTags != null)
            {
                // Очищаем текущие выбранные теги
                _selectedTags.Clear();
                
                // Добавляем выбранные теги в коллекцию
                foreach (var tag in tagSelectionWindow.SelectedTags)
                {
                    _selectedTags.Add(tag);
                }
            }
        }
        
        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tag tag)
            {
                _selectedTags.Remove(tag);
            }
        }
    }
}
