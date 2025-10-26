using System;
using System.ComponentModel.DataAnnotations;

namespace StoreOfHandWork.Models
{
    public class WishListItem
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public virtual User? User { get; set; }
        
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
        
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
