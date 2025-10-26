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
    public partial class ProductReviewsPage : Page
    {
        private readonly ApplicationDbContext _context;
        private readonly User _currentUser;
        private readonly Product _product;
        private int _selectedRating = 5;

        public ProductReviewsPage(ApplicationDbContext context, User currentUser, Product product)
        {
            InitializeComponent();
            _context = context;
            _currentUser = currentUser;
            _product = product;

            // Устанавливаем информацию о товаре
            ProductNameText.Text = product.Name;
            UpdateProductRatingInfo();
            
            // По умолчанию выбираем рейтинг 5
            var radioButton = RatingPanel.Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.Tag.ToString() == "5");
            if (radioButton != null)
                radioButton.IsChecked = true;
            
            LoadReviews();
        }

        private void UpdateProductRatingInfo()
        {
            // Получаем все отзывы для этого товара напрямую из базы данных
            var reviews = _context.Reviews
                .Where(r => r.ProductId == _product.Id)
                .ToList();
            
            // Рассчитываем средний рейтинг и количество отзывов
            int reviewCount = reviews.Count;
            double averageRating = 0.0;
            
            if (reviewCount > 0)
            {
                averageRating = Math.Round(reviews.Average(r => r.Rating), 1);
            }
            
            // Обновляем отображение на странице
            ProductRatingText.Text = averageRating.ToString("0.0");
            ReviewCountText.Text = $"({reviewCount} {GetReviewWord(reviewCount)})";
        }

        private string GetReviewWord(int count)
        {
            // Правильное склонение слова "отзыв" в зависимости от количества
            if (count % 10 == 1 && count % 100 != 11)
                return "отзыв";
            else if ((count % 10 == 2 || count % 10 == 3 || count % 10 == 4) && 
                    !(count % 100 == 12 || count % 100 == 13 || count % 100 == 14))
                return "отзыва";
            else
                return "отзывов";
        }

        private void LoadReviews()
        {
            // Загружаем отзывы о товаре с включением информации о пользователях
            var reviews = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == _product.Id)
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            // Добавляем флаг, указывающий, является ли текущий пользователь автором отзыва
            var reviewViewModels = reviews.Select(r => new
            {
                r.Id,
                r.UserId,
                r.User,
                r.Rating,
                r.Comment,
                r.CreatedDate,
                r.UpdatedDate,
                r.IsVerified,
                IsUserReview = r.UserId == _currentUser.Id || _currentUser.IsAdmin
            }).ToList();

            ReviewsItemsControl.ItemsSource = reviewViewModels;
            
            // Показываем сообщение, если отзывов нет
            EmptyReviewsMessage.Visibility = reviews.Any() ? Visibility.Collapsed : Visibility.Visible;
            
            // Проверяем, оставил ли пользователь уже отзыв для этого товара
            var userHasReview = reviews.Any(r => r.UserId == _currentUser.Id);
            if (userHasReview)
            {
                ReviewTextBox.IsEnabled = false;
                foreach (var radioButton in RatingPanel.Children.OfType<RadioButton>())
                {
                    radioButton.IsEnabled = false;
                }
                ReviewTextBox.Text = "Вы уже оставили отзыв на этот товар.";
            }
            
            // Обновляем информацию о рейтинге
            UpdateProductRatingInfo();
        }

        private void RatingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag != null)
            {
                if (int.TryParse(radioButton.Tag.ToString(), out int rating))
                {
                    _selectedRating = rating;
                }
            }
        }

        private void SubmitReviewButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, заполнен ли текст отзыва
            if (string.IsNullOrWhiteSpace(ReviewTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, напишите ваш отзыв", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, оставил ли пользователь уже отзыв для этого товара
            var existingReview = _context.Reviews
                .FirstOrDefault(r => r.UserId == _currentUser.Id && r.ProductId == _product.Id);

            if (existingReview != null)
            {
                MessageBox.Show("Вы уже оставили отзыв на этот товар", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Создаем новый отзыв
            var review = new Review
            {
                UserId = _currentUser.Id,
                ProductId = _product.Id,
                Rating = _selectedRating,
                Comment = ReviewTextBox.Text.Trim(),
                CreatedDate = DateTime.UtcNow,
                IsVerified = false
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            // Очищаем форму и обновляем список отзывов
            ReviewTextBox.Text = string.Empty;
            LoadReviews();
            UpdateProductRatingInfo();
            
            MessageBox.Show("Спасибо за ваш отзыв!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int reviewId)
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
                if (review != null && (review.UserId == _currentUser.Id || _currentUser.IsAdmin))
                {
                    // Создаем диалоговое окно для редактирования отзыва
                    var window = new Window
                    {
                        Title = "Редактировать отзыв",
                        Width = 500,
                        Height = 300,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    var grid = new Grid { Margin = new Thickness(15) };
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Заголовок
                    var titleText = new TextBlock
                    {
                        Text = "Редактировать отзыв",
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(titleText, 0);
                    grid.Children.Add(titleText);

                    // Выбор рейтинга
                    var ratingPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
                    ratingPanel.Children.Add(new TextBlock { Text = "Ваша оценка: ", VerticalAlignment = VerticalAlignment.Center });

                    var ratingButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    var ratingRadioButtons = new List<RadioButton>();

                    for (int i = 1; i <= 5; i++)
                    {
                        var radioButton = new RadioButton
                        {
                            Content = i.ToString(),
                            GroupName = "EditRating",
                            Tag = i,
                            Margin = new Thickness(5, 0, 5, 0),
                            IsChecked = i == review.Rating
                        };
                        ratingButtonsPanel.Children.Add(radioButton);
                        ratingRadioButtons.Add(radioButton);
                    }

                    ratingPanel.Children.Add(ratingButtonsPanel);
                    Grid.SetRow(ratingPanel, 1);
                    grid.Children.Add(ratingPanel);

                    // Текстовое поле для отзыва
                    var textBox = new TextBox
                    {
                        Text = review.Comment,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    Grid.SetRow(textBox, 2);
                    grid.Children.Add(textBox);

                    // Кнопки
                    var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                    
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
                        if (string.IsNullOrWhiteSpace(textBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, напишите ваш отзыв", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Определяем выбранный рейтинг
                        int selectedRating = 5;
                        foreach (var rb in ratingRadioButtons)
                        {
                            if (rb.IsChecked == true && rb.Tag is int rating)
                            {
                                selectedRating = rating;
                                break;
                            }
                        }

                        // Обновляем отзыв
                        review.Rating = selectedRating;
                        review.Comment = textBox.Text.Trim();
                        review.UpdatedDate = DateTime.UtcNow;
                        
                        _context.SaveChanges();
                        window.Close();
                        
                        // Обновляем список отзывов
                        LoadReviews();
                        UpdateProductRatingInfo();
                        
                        MessageBox.Show("Отзыв успешно обновлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    };
                    
                    buttonsPanel.Children.Add(cancelButton);
                    buttonsPanel.Children.Add(saveButton);
                    Grid.SetRow(buttonsPanel, 3);
                    grid.Children.Add(buttonsPanel);

                    window.Content = grid;
                    window.ShowDialog();
                }
            }
        }

        private void DeleteReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int reviewId)
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
                if (review != null && (review.UserId == _currentUser.Id || _currentUser.IsAdmin))
                {
                    var result = MessageBox.Show("Вы уверены, что хотите удалить этот отзыв?", "Подтверждение", 
                                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _context.Reviews.Remove(review);
                        _context.SaveChanges();
                        
                        LoadReviews();
                        UpdateProductRatingInfo();
                        
                        MessageBox.Show("Отзыв успешно удален", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
