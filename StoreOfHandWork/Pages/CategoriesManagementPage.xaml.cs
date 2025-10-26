using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Pages
{
    public partial class CategoriesManagementPage : Page
    {
        private readonly ApplicationDbContext _context;

        public CategoriesManagementPage()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            
            Loaded += CategoriesManagementPage_Loaded;
        }

        private void CategoriesManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _context.Categories
                    .Include(c => c.Products)
                    .OrderBy(c => c.Name)
                    .ToList();

                CategoriesDataGrid.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.DataContext is Category category)
                {
                    if (string.IsNullOrWhiteSpace(category.Name))
                    {
                        MessageBox.Show("Название категории не может быть пустым",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Проверяем существование категории с таким же именем
                    var existingCategory = _context.Categories
                        .FirstOrDefault(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);

                    if (existingCategory != null)
                    {
                        MessageBox.Show("Категория с таким названием уже существует",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Получаем актуальную версию категории из базы
                    var categoryToUpdate = _context.Categories.Find(category.Id);
                    if (categoryToUpdate != null)
                    {
                        // Обновляем данные
                        categoryToUpdate.Name = category.Name.Trim();
                        categoryToUpdate.Description = category.Description?.Trim() ?? "";

                        // Сохраняем изменения
                        _context.SaveChanges();

                        MessageBox.Show("Категория успешно обновлена",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезагружаем список категорий
                        LoadCategories();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении категории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Category category)
            {
                try
                {
                    var categoryToDelete = _context.Categories
                        .Include(c => c.Products)
                        .FirstOrDefault(c => c.Id == category.Id);

                    if (categoryToDelete != null)
                    {
                        if (categoryToDelete.Products.Any())
                        {
                            MessageBox.Show("Невозможно удалить категорию, содержащую товары",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var result = MessageBox.Show(
                            $"Вы действительно хотите удалить категорию '{categoryToDelete.Name}'?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            _context.Categories.Remove(categoryToDelete);
                            _context.SaveChanges();
                            LoadCategories();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении категории: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newCategory = new Category
                {
                    Name = "Новая категория",
                    Description = "Описание категории",
                    IsActive = true
                };

                _context.Categories.Add(newCategory);
                _context.SaveChanges();
                LoadCategories();

                // Находим новую строку и начинаем её редактирование
                var rowIndex = CategoriesDataGrid.Items.Count - 1;
                if (rowIndex >= 0)
                {
                    CategoriesDataGrid.ScrollIntoView(CategoriesDataGrid.Items[rowIndex]);
                    CategoriesDataGrid.SelectedIndex = rowIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoriesDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var category = e.Row.Item as Category;
                if (category != null)
                {
                    SaveCategory_Click(sender, new RoutedEventArgs());
                }
            }
        }
    }
}
