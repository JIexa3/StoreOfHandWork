using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StoreOfHandWork.Pages
{
    public partial class TagsManagementPage : Page
    {
        private readonly ApplicationDbContext _context;
        private Tag _currentTag;
        private bool _isEditMode = false;

        public TagsManagementPage()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            LoadTags();
        }

        private void LoadTags()
        {
            try
            {
                // Загружаем теги вместе с коллекцией связанных товаров для отображения количества
                var tags = _context.Tags.Include(t => t.Products).ToList();
                TagsDataGrid.ItemsSource = tags;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке тегов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TagNameTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите название тега", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isEditMode && _currentTag != null)
                {
                    // Редактирование существующего тега
                    _currentTag.Name = TagNameTextBox.Text.Trim();
                    _context.SaveChanges();
                    MessageBox.Show("Тег успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Добавление нового тега
                    var newTag = new Tag { Name = TagNameTextBox.Text.Trim() };
                    _context.Tags.Add(newTag);
                    _context.SaveChanges();
                    MessageBox.Show("Новый тег успешно добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Сбрасываем форму и обновляем список
                ResetForm();
                LoadTags();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении тега: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tag tag)
            {
                // Переключаемся в режим редактирования
                _currentTag = tag;
                _isEditMode = true;
                
                // Заполняем форму данными выбранного тега
                FormTitleTextBlock.Text = "Редактировать тег";
                TagNameTextBox.Text = tag.Name;
                SaveButton.Content = "Обновить";
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tag tag)
            {
                // Проверяем, используется ли тег в товарах
                if (tag.Products.Any())
                {
                    MessageBox.Show($"Невозможно удалить тег, так как он используется в {tag.Products.Count} товарах.\nСначала удалите тег из товаров.", 
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Запрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить тег '{tag.Name}'?", 
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.Tags.Remove(tag);
                        _context.SaveChanges();
                        MessageBox.Show("Тег успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadTags();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении тега: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            // Сбрасываем форму в исходное состояние
            _currentTag = null;
            _isEditMode = false;
            FormTitleTextBlock.Text = "Добавить новый тег";
            TagNameTextBox.Text = "";
            SaveButton.Content = "Сохранить";
            CancelButton.Visibility = Visibility.Visible;
        }
    }
}
