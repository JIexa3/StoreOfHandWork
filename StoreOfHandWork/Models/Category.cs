using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreOfHandWork.Models
{
    public class Category
    {
        public Category()
        {
            Products = new List<Product>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Название категории обязательно для заполнения")]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
