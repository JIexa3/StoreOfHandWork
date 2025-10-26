using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork
{
    public partial class TagsManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private Tag? _currentTag;
        private bool _isEditMode;

        public TagsManagementWindow(ApplicationDbContext context)
        {
            InitializeComponent();
            _context = context;
            LoadTags();
        }

        private void LoadTags()
        {
            // Загружаем все теги с подсчетом количества продуктов для каждого тега
            var tags = _context.Tags
                .Include(t => t.Products)
                .Select(t => new 
                {
                    t.Id,
                    t.Name,
                    ProductCount = _context.Set<ProductTag>().Count(pt => pt.TagId == t.Id)
                })
                .OrderBy(t => t.Name)
                .ToList();

            TagsDataGrid.ItemsSource = tags;
        }

        private void TagsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Отключаем режим редактирования при изменении выбора
            ResetForm();
        }

        private void EditTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int tagId)
            {
                var tag = _context.Tags.Find(tagId);
                if (tag != null)
                {
                    _currentTag = tag;
                    _isEditMode = true;
                    
                    FormTitleTextBlock.Text = "Редактировать тег";
                    TagNameTextBox.Text = tag.Name;
                    CancelButton.Visibility = Visibility.Visible;
                    SaveButton.Content = "Обновить";
                }
            }
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int tagId)
            {
                var tag = _context.Tags.Find(tagId);
                if (tag != null)
                {
                    // Проверяем, используется ли тег
                    var usageCount = _context.Set<ProductTag>().Count(pt => pt.TagId == tagId);
                    
                    if (usageCount > 0)
                    {
                        var result = MessageBox.Show(
                            $"Тег '{tag.Name}' используется {usageCount} товарами. Вы уверены, что хотите удалить его?",
                            "Подтверждение удаления",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                            
                        if (result != MessageBoxResult.Yes)
                            return;
                    }
                    else
                    {
                        var result = MessageBox.Show(
                            $"Вы уверены, что хотите удалить тег '{tag.Name}'?",
                            "Подтверждение удаления",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                            
                        if (result != MessageBoxResult.Yes)
                            return;
                    }
                    
                    try
                    {
                        // Удаляем связи тег-продукт
                        var productTags = _context.Set<ProductTag>().Where(pt => pt.TagId == tagId).ToList();
                        _context.Set<ProductTag>().RemoveRange(productTags);
                        
                        // Удаляем сам тег
                        _context.Tags.Remove(tag);
                        _context.SaveChanges();
                        
                        MessageBox.Show(
                            $"Тег '{tag.Name}' успешно удален.",
                            "Успешно",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                            
                        LoadTags();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при удалении тега: {ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var tagName = TagNameTextBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(tagName))
            {
                MessageBox.Show(
                    "Название тега не может быть пустым.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // Проверяем, существует ли уже тег с таким именем
            var existingTag = _context.Tags.FirstOrDefault(t => 
                t.Name.ToLower() == tagName.ToLower() && 
                (!_isEditMode || t.Id != _currentTag.Id));
                
            if (existingTag != null)
            {
                MessageBox.Show(
                    $"Тег с названием '{tagName}' уже существует.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                if (_isEditMode && _currentTag != null)
                {
                    // Режим редактирования
                    _currentTag.Name = tagName;
                    _context.SaveChanges();
                    
                    MessageBox.Show(
                        $"Тег успешно обновлен.",
                        "Успешно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Режим добавления
                    var newTag = new Tag { Name = tagName };
                    _context.Tags.Add(newTag);
                    _context.SaveChanges();
                    
                    MessageBox.Show(
                        $"Новый тег успешно добавлен.",
                        "Успешно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                // Очищаем форму и обновляем список
                ResetForm();
                LoadTags();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }
        
        private void ResetForm()
        {
            _currentTag = null;
            _isEditMode = false;
            
            FormTitleTextBlock.Text = "Добавить новый тег";
            TagNameTextBox.Text = string.Empty;
            CancelButton.Visibility = Visibility.Collapsed;
            SaveButton.Content = "Сохранить";
        }
    }
}
