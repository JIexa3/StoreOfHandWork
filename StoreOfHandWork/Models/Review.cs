using System;
using System.ComponentModel.DataAnnotations;

namespace StoreOfHandWork.Models
{
    public class Review
    {
        public Review()
        {
            Comment = string.Empty;
        }

        public int Id { get; set; }
        
        public int UserId { get; set; }
        public virtual User? User { get; set; }
        
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Рейтинг должен быть от 1 до 5")]
        public int Rating { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Отзыв не должен превышать 1000 символов")]
        public string Comment { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        
        public bool IsVerified { get; set; }
    }
}
