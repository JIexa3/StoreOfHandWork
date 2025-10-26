using StoreOfHandWork.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StoreOfHandWork.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Инициализация пунктов выдачи
            if (!context.PickupPoints.Any())
            {
                var pickupPoints = new List<PickupPoint>
                {
                    new PickupPoint
                    {
                        Name = "Пункт выдачи на Гончарова",
                        Address = "ул. Гончарова, 27, Ульяновск",
                        Latitude = 54.31724,
                        Longitude = 48.39876,
                        WorkingHours = "Пн-Вс: 10:00-20:00",
                        Phone = "+7 (8422) 42-42-42"
                    },
                    new PickupPoint
                    {
                        Name = "Пункт выдачи на Минаева",
                        Address = "ул. Минаева, 11, Ульяновск",
                        Latitude = 54.31089,
                        Longitude = 48.39462,
                        WorkingHours = "Пн-Вс: 09:00-21:00",
                        Phone = "+7 (8422) 43-43-43"
                    },
                    new PickupPoint
                    {
                        Name = "Пункт выдачи на Нариманова",
                        Address = "пр-т Нариманова, 75, Ульяновск",
                        Latitude = 54.33256,
                        Longitude = 48.38569,
                        WorkingHours = "Пн-Вс: 10:00-19:00",
                        Phone = "+7 (8422) 44-44-44"
                    },
                    new PickupPoint
                    {
                        Name = "Пункт выдачи на Карла Маркса",
                        Address = "ул. Карла Маркса, 13, Ульяновск",
                        Latitude = 54.31679,
                        Longitude = 48.40123,
                        WorkingHours = "Пн-Вс: 09:00-20:00",
                        Phone = "+7 (8422) 45-45-45"
                    },
                    new PickupPoint
                    {
                        Name = "Пункт выдачи на Рябикова",
                        Address = "ул. Рябикова, 70, Ульяновск",
                        Latitude = 54.28988,
                        Longitude = 48.32146,
                        WorkingHours = "Пн-Вс: 10:00-21:00",
                        Phone = "+7 (8422) 46-46-46"
                    }
                };

                context.PickupPoints.AddRange(pickupPoints);
                context.SaveChanges();
            }

            // Проверяем, есть ли уже пользователи
            if (!context.Users.Any())
            {
            // Создаем администратора
            var admin = new User
            {
                Name = "Admin",
                Email = "admin@store.com",
                Phone = "1234567890",
                Address = "Admin Address",
                PasswordHash = "admin123",
                Role = "Admin",
                Status = "Активен",
                CreatedDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                IsEmailVerified = true,
                EmailNotificationsEnabled = true,
                SmsNotificationsEnabled = true
            };

            context.Users.Add(admin);
            context.SaveChanges();
            }

            // Проверяем, есть ли уже категории
            if (!context.Categories.Any())
            {
            // Создаем категории
            var categories = new Category[]
            {
                new Category { Name = "Вязание", Description = "Товары для вязания" },
                new Category { Name = "Вышивка", Description = "Товары для вышивки" },
                new Category { Name = "Шитье", Description = "Товары для шитья" },
                new Category { Name = "Бисероплетение", Description = "Товары для бисероплетения" }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();

            // Создаем продукты
            var products = new Product[]
            {
                new Product
                {
                    Name = "Спицы для вязания",
                    Description = "Набор спиц разного размера",
                    Price = 500,
                    StockQuantity = 100,
                    CategoryId = categories[0].Id,
                    ImagePath = "/Images/knitting-needles.jpg",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Product
                {
                    Name = "Пряжа",
                    Description = "Шерстяная пряжа высокого качества",
                    Price = 300,
                    StockQuantity = 200,
                    CategoryId = categories[0].Id,
                    ImagePath = "/Images/yarn.jpg",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
            }
        }
    }
}
