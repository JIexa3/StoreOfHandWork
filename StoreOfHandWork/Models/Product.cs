using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace StoreOfHandWork.Models
{
    public class Product
    {
        public Product()
        {
            OrderItems = new List<OrderItem>();
            Reviews = new List<Review>();
            WishListItems = new List<WishListItem>();
            Tags = new HashSet<Tag>();
        }

        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [MaxLength(2000)]
        public string Description { get; set; }
        
        [Required]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }
        
        public int StockQuantity { get; set; }
        
        public string ImagePath { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        
        public virtual ICollection<Review> Reviews { get; set; }
        
        public virtual ICollection<WishListItem> WishListItems { get; set; }
        
        public virtual ICollection<Tag> Tags { get; set; }
        
        [NotMapped]
        public double AverageRating => Reviews.Any() ? Math.Round(Reviews.Average(r => r.Rating), 1) : 0;
        
        [NotMapped]
        public int ReviewCount => Reviews.Count;
    }
}
