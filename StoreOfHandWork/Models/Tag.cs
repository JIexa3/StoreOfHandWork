using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreOfHandWork.Models
{
    public class Tag
    {
        public Tag()
        {
            Products = new HashSet<Product>();
            Name = string.Empty;
        }

        public int Id { get; set; }
        
        [Required(ErrorMessage = "Название тега обязательно для заполнения")]
        [MaxLength(50, ErrorMessage = "Название тега не должно превышать 50 символов")]
        public string Name { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Product> Products { get; set; }
    }
}
