using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using StoreOfHandWork.Pages;

namespace StoreOfHandWork
{
    public partial class ProductDetailsWindow : Window
    {
        private readonly Product _product;
        private readonly User _currentUser;
        private readonly ApplicationDbContext _context;

        public ProductDetailsWindow(Product product, User currentUser, ApplicationDbContext context)
        {
            InitializeComponent();
            _product = product;
            _currentUser = currentUser;
            _context = context;

            LoadProductDetails();
            LoadProductReviews();
            CheckWishListStatus();
        }

        private void LoadProductDetails()
        {
            try
            {
                // Загружаем основную информацию о товаре
                ProductNameTextBlock.Text = _product.Name;
                ProductPriceTextBlock.Text = $"{_product.Price:N2} ₽";
                ProductCategoryTextBlock.Text = $"Категория: {_product.Category?.Name ?? "Не указана"}";
                ProductStockTextBlock.Text = $"В наличии: {_product.StockQuantity} шт.";
                ProductDescriptionTextBlock.Text = _product.Description;

                // Загружаем рейтинг товара
                var reviews = _context.Reviews.Where(r => r.ProductId == _product.Id).ToList();
                double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                int reviewCount = reviews.Count;

                ProductRatingTextBlock.Text = avgRating.ToString("0.0");
                ReviewCountTextBlock.Text = $"({reviewCount} {GetReviewWord(reviewCount)})";

                // Загружаем теги товара из коллекции Tags продукта
                // Загружаем продукт с тегами
                var productWithTags = _context.Products
                    .Include(p => p.Tags)
                    .FirstOrDefault(p => p.Id == _product.Id);

                TagsPanel.Children.Clear();
                TagsPanel.Children.Add(new TextBlock { Text = "Теги: ", FontSize = 14 });

                if (productWithTags?.Tags != null && productWithTags.Tags.Any())
                {
                    foreach (var tag in productWithTags.Tags)
                    {
                        var tagBorder = new Border
                        {
                            Background = new SolidColorBrush(Colors.LightGray),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(5, 2, 5, 2),
                            Margin = new Thickness(2)
                        };

                        tagBorder.Child = new TextBlock
                        {
                            Text = tag.Name,
                            FontSize = 12
                        };

                        TagsPanel.Children.Add(tagBorder);
                    }
                }
                else
                {
                    var noTagsText = new TextBlock
                    {
                        Text = "Нет тегов",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Colors.Gray)
                    };
                    TagsPanel.Children.Add(noTagsText);
                }

                // Загружаем изображение товара
                if (!string.IsNullOrEmpty(_product.ImagePath))
                {
                    try
                    {
                        ProductImage.Source = new BitmapImage(new Uri(_product.ImagePath, UriKind.RelativeOrAbsolute));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о товаре: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void LoadProductReviews()
        {
            try
            {
                // Загружаем последние 3 отзыва для товара
                var reviews = _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == _product.Id)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(3)
                    .ToList();

                ReviewsPanel.Children.Clear();

                if (reviews.Any())
                {
                    NoReviewsTextBlock.Visibility = Visibility.Collapsed;

                    foreach (var review in reviews)
                    {
                        // Создаем панель отзыва
                        var reviewPanel = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.LightGray),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(5),
                            Margin = new Thickness(0, 0, 0, 10),
                            Padding = new Thickness(10)
                        };

                        var grid = new Grid();
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        // Верхняя панель с именем пользователя и датой
                        var topPanel = new Grid();
                        topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var userNameText = new TextBlock
                        {
                            Text = review.User?.Name ?? "Пользователь",
                            FontWeight = FontWeights.SemiBold
                        };
                        Grid.SetColumn(userNameText, 0);
                        topPanel.Children.Add(userNameText);

                        var dateText = new TextBlock
                        {
                            Text = review.CreatedDate.ToString("dd.MM.yyyy"),
                            Foreground = new SolidColorBrush(Colors.Gray),
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        Grid.SetColumn(dateText, 1);
                        topPanel.Children.Add(dateText);

                        Grid.SetRow(topPanel, 0);
                        grid.Children.Add(topPanel);

                        // Рейтинг
                        var ratingPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 5, 0, 5)
                        };

                        ratingPanel.Children.Add(new TextBlock
                        {
                            Text = "Оценка: ",
                            VerticalAlignment = VerticalAlignment.Center
                        });

                        ratingPanel.Children.Add(new TextBlock
                        {
                            Text = review.Rating.ToString(),
                            FontWeight = FontWeights.SemiBold
                        });

                        ratingPanel.Children.Add(new TextBlock
                        {
                            Text = " из 5",
                            Margin = new Thickness(2, 0, 0, 0)
                        });

                        Grid.SetRow(ratingPanel, 1);
                        grid.Children.Add(ratingPanel);

                        // Текст отзыва
                        var commentText = new TextBlock
                        {
                            Text = review.Comment,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 5, 0, 0)
                        };
                        Grid.SetRow(commentText, 2);
                        grid.Children.Add(commentText);

                        reviewPanel.Child = grid;
                        ReviewsPanel.Children.Add(reviewPanel);
                    }
                }
                else
                {
                    NoReviewsTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отзывов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckWishListStatus()
        {
            try
            {
                // Проверяем, есть ли товар уже в списке желаний пользователя
                var inWishList = _context.WishListItems
                    .Any(w => w.UserId == _currentUser.Id && w.ProductId == _product.Id);

                if (inWishList)
                {
                    AddToWishListButton.Content = "Удалить из списка желаний";
                    AddToWishListButton.Background = new SolidColorBrush(Colors.LightCoral);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке списка желаний: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, есть ли товар в наличии
                if (_product.StockQuantity <= 0)
                {
                    MessageBox.Show("К сожалению, этого товара нет в наличии", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверяем, есть ли уже этот товар в корзине
                var existingCartItem = _context.CartItems
                    .FirstOrDefault(c => c.UserId == _currentUser.Id && c.ProductId == _product.Id);

                if (existingCartItem != null)
                {
                    // Если товар уже в корзине, увеличиваем количество
                    existingCartItem.Quantity++;
                }
                else
                {
                    // Если товара нет в корзине, добавляем новый элемент
                    var newCartItem = new CartItem
                    {
                        UserId = _currentUser.Id,
                        ProductId = _product.Id,
                        Quantity = 1,
                        DateAdded = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newCartItem);
                }

                _context.SaveChanges();
                MessageBox.Show("Товар добавлен в корзину", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в корзину: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToWishListButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, есть ли товар уже в списке желаний пользователя
                var wishListItem = _context.WishListItems
                    .FirstOrDefault(w => w.UserId == _currentUser.Id && w.ProductId == _product.Id);

                if (wishListItem != null)
                {
                    // Если товар уже в списке желаний, удаляем его
                    _context.WishListItems.Remove(wishListItem);
                    _context.SaveChanges();

                    AddToWishListButton.Content = "В список желаний";
                    AddToWishListButton.Background = null; // Возвращаем стандартный цвет кнопки

                    MessageBox.Show("Товар удален из списка желаний", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Если товара нет в списке желаний, добавляем его
                    var newWishListItem = new WishListItem
                    {
                        UserId = _currentUser.Id,
                        ProductId = _product.Id,
                        DateAdded = DateTime.UtcNow
                    };

                    _context.WishListItems.Add(newWishListItem);
                    _context.SaveChanges();

                    AddToWishListButton.Content = "Удалить из списка желаний";
                    AddToWishListButton.Background = new SolidColorBrush(Colors.LightCoral);

                    MessageBox.Show("Товар добавлен в список желаний", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при работе со списком желаний: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewAllReviewsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Открываем страницу со всеми отзывами и возможностью оставить отзыв
                var reviewsPage = new ProductReviewsPage(_context, _currentUser, _product);
                var reviewsWindow = new Window
                {
                    Title = $"Отзывы о товаре: {_product.Name}",
                    Content = reviewsPage,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                reviewsWindow.ShowDialog();

                // После закрытия окна отзывов обновляем информацию на странице товара
                LoadProductDetails();
                LoadProductReviews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии страницы отзывов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
