using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Data;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Services
{
    public interface ICartService
    {
        Task<bool> IsProductAvailableForCart(int productId, int requestedQuantity, int? excludeCartItemId = null);
        Task<int> GetAvailableQuantity(int productId);
        Task<bool> AddToCart(int userId, int productId, int quantity);
        Task<bool> UpdateCartItemQuantity(int cartItemId, int newQuantity);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsProductAvailableForCart(int productId, int requestedQuantity, int? excludeCartItemId = null)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return false;

            // Получаем общее количество товара в корзинах других пользователей
            var quantityInCarts = await _context.CartItems
                .Where(ci => ci.ProductId == productId && (!excludeCartItemId.HasValue || ci.Id != excludeCartItemId.Value))
                .SumAsync(ci => ci.Quantity);

            // Проверяем, достаточно ли товара на складе с учетом количества в корзинах
            return product.StockQuantity >= (quantityInCarts + requestedQuantity);
        }

        public async Task<int> GetAvailableQuantity(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return 0;

            var quantityInCarts = await _context.CartItems
                .Where(ci => ci.ProductId == productId)
                .SumAsync(ci => ci.Quantity);

            return Math.Max(0, product.StockQuantity - quantityInCarts);
        }

        public async Task<bool> AddToCart(int userId, int productId, int quantity)
        {
            // Проверяем доступность товара
            if (!await IsProductAvailableForCart(productId, quantity))
                return false;

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (existingItem != null)
            {
                // Если товар уже есть в корзине, проверяем доступность с учетом исключения текущего элемента
                if (!await IsProductAvailableForCart(productId, quantity + existingItem.Quantity, existingItem.Id))
                    return false;

                existingItem.Quantity += quantity;
                _context.Entry(existingItem).State = EntityState.Modified;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCartItemQuantity(int cartItemId, int newQuantity)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null)
                return false;

            // Проверяем доступность товара с учетом исключения текущего элемента корзины
            if (!await IsProductAvailableForCart(cartItem.ProductId, newQuantity, cartItem.Id))
                return false;

            cartItem.Quantity = newQuantity;
            _context.Entry(cartItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 