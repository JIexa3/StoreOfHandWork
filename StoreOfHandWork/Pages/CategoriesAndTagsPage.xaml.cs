using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Pages
{
    public partial class CategoriesAndTagsPage : Page
    {
        private readonly ApplicationDbContext _context;
        private readonly User _currentUser;

        public CategoriesAndTagsPage(ApplicationDbContext context, User currentUser)
        {
            InitializeComponent();
            _context = context;
            _currentUser = currentUser;

            // Проверяем, является ли пользователь администратором
            if (!_currentUser.IsAdmin)
            {
                MessageBox.Show("У вас нет прав для доступа к этой странице", "Ошибка доступа", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadCategories();
            LoadTags();
        }

        #region Категории

        private void LoadCategories()
        {
            var categories = _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    ProductCount = c.Products.Count,
                    c.IsActive
                })
                .ToList();

            CategoriesListView.ItemsSource = categories;
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка ввода
            if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
            {
                MessageBox.Show("Введите название категории", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, существует ли уже категория с таким названием
            var categoryName = CategoryNameTextBox.Text.Trim();
            if (_context.Categories.Any(c => c.Name.ToLower() == categoryName.ToLower()))
            {
                MessageBox.Show("Категория с таким названием уже существует", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем новую категорию
            var category = new Category
            {
                Name = categoryName,
                Description = CategoryDescriptionTextBox.Text.Trim(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Categories.Add(category);
            _context.SaveChanges();

            // Очищаем поля ввода и обновляем список
            CategoryNameTextBox.Text = string.Empty;
            CategoryDescriptionTextBox.Text = string.Empty;
            LoadCategories();

            MessageBox.Show("Категория успешно добавлена", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int categoryId)
            {
                var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    // Создаем диалоговое окно для редактирования категории
                    var window = new Window
                    {
                        Title = "Редактировать категорию",
                        Width = 400,
                        Height = 250,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    var grid = new Grid { Margin = new Thickness(15) };
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Заголовок
                    var titleText = new TextBlock
                    {
                        Text = "Редактировать категорию",
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(titleText, 0);
                    grid.Children.Add(titleText);

                    // Название категории
                    var nameLabel = new TextBlock
                    {
                        Text = "Название категории:",
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    Grid.SetRow(nameLabel, 1);
                    grid.Children.Add(nameLabel);

                    var nameTextBox = new TextBox
                    {
                        Text = category.Name,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(nameTextBox, 2);
                    grid.Children.Add(nameTextBox);

                    // Описание категории
                    var descriptionLabel = new TextBlock
                    {
                        Text = "Описание категории:",
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    Grid.SetRow(descriptionLabel, 3);
                    grid.Children.Add(descriptionLabel);

                    var descriptionTextBox = new TextBox
                    {
                        Text = category.Description,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(descriptionTextBox, 4);
                    grid.Children.Add(descriptionTextBox);

                    // Активна ли категория
                    var activeCheckBox = new CheckBox
                    {
                        Content = "Категория активна",
                        IsChecked = category.IsActive,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(activeCheckBox, 5);
                    grid.Children.Add(activeCheckBox);

                    // Кнопки
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    var cancelButton = new Button
                    {
                        Content = "Отмена",
                        Width = 100,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    cancelButton.Click += (s, args) => window.Close();

                    var saveButton = new Button
                    {
                        Content = "Сохранить",
                        Width = 100,
                        Height = 30
                    };
                    saveButton.Click += (s, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Введите название категории", "Ошибка", 
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var trimmedName = nameTextBox.Text.Trim();
                        if (_context.Categories.Any(c => c.Name.ToLower() == trimmedName.ToLower() && c.Id != category.Id))
                        {
                            MessageBox.Show("Категория с таким названием уже существует", "Ошибка", 
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Обновляем категорию
                        category.Name = trimmedName;
                        category.Description = descriptionTextBox.Text.Trim();
                        category.IsActive = activeCheckBox.IsChecked ?? true;

                        _context.SaveChanges();
                        window.Close();

                        // Обновляем список категорий
                        LoadCategories();

                        MessageBox.Show("Категория успешно обновлена", "Успешно", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    };

                    buttonsPanel.Children.Add(cancelButton);
                    buttonsPanel.Children.Add(saveButton);
                    Grid.SetRow(buttonsPanel, 6);
                    grid.Children.Add(buttonsPanel);

                    window.Content = grid;
                    window.ShowDialog();
                }
            }
        }

        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int categoryId)
            {
                var category = _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefault(c => c.Id == categoryId);

                if (category != null)
                {
                    // Проверяем, есть ли товары в этой категории
                    if (category.Products.Any())
                    {
                        MessageBox.Show($"Невозможно удалить категорию '{category.Name}', так как в ней есть товары. " +
                                      $"Сначала переместите товары в другую категорию.", 
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Вы уверены, что хотите удалить категорию '{category.Name}'?", 
                                              "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _context.Categories.Remove(category);
                        _context.SaveChanges();
                        LoadCategories();
                        MessageBox.Show("Категория успешно удалена", "Успешно", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        #endregion

        #region Теги

        private void LoadTags()
        {
            var tags = _context.Tags
                .Include(t => t.Products)
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    ProductCount = t.Products.Count
                })
                .ToList();

            TagsListView.ItemsSource = tags;
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка ввода
            if (string.IsNullOrWhiteSpace(TagNameTextBox.Text))
            {
                MessageBox.Show("Введите название тега", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, существует ли уже тег с таким названием
            var tagName = TagNameTextBox.Text.Trim();
            if (_context.Tags.Any(t => t.Name.ToLower() == tagName.ToLower()))
            {
                MessageBox.Show("Тег с таким названием уже существует", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем новый тег
            var tag = new Tag
            {
                Name = tagName,
                CreatedDate = DateTime.UtcNow
            };

            _context.Tags.Add(tag);
            _context.SaveChanges();

            // Очищаем поле ввода и обновляем список
            TagNameTextBox.Text = string.Empty;
            LoadTags();

            MessageBox.Show("Тег успешно добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int tagId)
            {
                var tag = _context.Tags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null)
                {
                    // Создаем диалоговое окно для редактирования тега
                    var window = new Window
                    {
                        Title = "Редактировать тег",
                        Width = 350,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    var grid = new Grid { Margin = new Thickness(15) };
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Заголовок
                    var titleText = new TextBlock
                    {
                        Text = "Редактировать тег",
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(titleText, 0);
                    grid.Children.Add(titleText);

                    // Название тега
                    var nameTextBox = new TextBox
                    {
                        Text = tag.Name,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(nameTextBox, 1);
                    grid.Children.Add(nameTextBox);

                    // Кнопки
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    var cancelButton = new Button
                    {
                        Content = "Отмена",
                        Width = 100,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    cancelButton.Click += (s, args) => window.Close();

                    var saveButton = new Button
                    {
                        Content = "Сохранить",
                        Width = 100,
                        Height = 30
                    };
                    saveButton.Click += (s, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Введите название тега", "Ошибка", 
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var trimmedName = nameTextBox.Text.Trim();
                        if (_context.Tags.Any(t => t.Name.ToLower() == trimmedName.ToLower() && t.Id != tag.Id))
                        {
                            MessageBox.Show("Тег с таким названием уже существует", "Ошибка", 
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Обновляем тег
                        tag.Name = trimmedName;
                        _context.SaveChanges();
                        window.Close();

                        // Обновляем список тегов
                        LoadTags();

                        MessageBox.Show("Тег успешно обновлен", "Успешно", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    };

                    buttonsPanel.Children.Add(cancelButton);
                    buttonsPanel.Children.Add(saveButton);
                    Grid.SetRow(buttonsPanel, 2);
                    grid.Children.Add(buttonsPanel);

                    window.Content = grid;
                    window.ShowDialog();
                }
            }
        }

        private void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int tagId)
            {
                var tag = _context.Tags
                    .Include(t => t.Products)
                    .FirstOrDefault(t => t.Id == tagId);

                if (tag != null)
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите удалить тег '{tag.Name}'? " +
                                              $"Он будет удален из всех товаров, к которым он привязан.", 
                                              "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Удаляем связи тега с товарами
                        var productTags = _context.Set<ProductTag>().Where(pt => pt.TagId == tagId);
                        _context.RemoveRange(productTags);

                        // Удаляем сам тег
                        _context.Tags.Remove(tag);
                        _context.SaveChanges();
                        
                        LoadTags();
                        MessageBox.Show("Тег успешно удален", "Успешно", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        #endregion
        
        #region Расширенное управление тегами
        
        private void OpenTagsManagementWindow_Click(object sender, RoutedEventArgs e)
        {
            var tagsWindow = new TagsManagementWindow(_context);
            tagsWindow.ShowDialog();
            
            // После закрытия окна обновляем список тегов
            LoadTags();
        }
        
        #endregion
    }
}
