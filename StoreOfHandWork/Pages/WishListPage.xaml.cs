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
    public partial class WishListPage : Page
    {
        private readonly ApplicationDbContext _context;
        private readonly User _currentUser;

        public WishListPage(ApplicationDbContext context, User currentUser)
        {
            InitializeComponent();
            _context = context;
            _currentUser = currentUser;
            
            LoadWishListItems();
        }



        private void LoadWishListItems()
        {
            // Загружаем элементы списка желаний пользователя вместе с товарами и категориями
            var wishListItems = _context.WishListItems
                .Include(w => w.Product)
                .ThenInclude(p => p.Category)
                .Where(w => w.UserId == (_currentUser != null ? _currentUser.Id : 0))
                .OrderByDescending(w => w.DateAdded)
                .ToList();

            // Устанавливаем данные для ItemsControl
            WishListItemsControl.ItemsSource = wishListItems;
            
            // Показываем сообщение, если список желаний пуст
            EmptyWishListMessage.Visibility = wishListItems.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RemoveFromWishListButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int wishListItemId)
            {
                var wishListItem = _context.WishListItems.FirstOrDefault(w => w.Id == wishListItemId);
                if (wishListItem != null)
                {
                    _context.WishListItems.Remove(wishListItem);
                    _context.SaveChanges();
                    LoadWishListItems();
                    MessageBox.Show("Товар удален из списка желаний", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int productId)
            {
                // Проверяем, есть ли уже этот товар в корзине
                var existingCartItem = _context.CartItems
                    .FirstOrDefault(c => c.UserId == _currentUser.Id && c.ProductId == productId);

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
                        ProductId = productId,
                        Quantity = 1,
                        DateAdded = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newCartItem);
                }

                _context.SaveChanges();
                MessageBox.Show("Товар добавлен в корзину", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
