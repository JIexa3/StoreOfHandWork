using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreOfHandWork.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        
        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsCollected { get; set; }

        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}
