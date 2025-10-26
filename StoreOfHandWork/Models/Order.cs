using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreOfHandWork.Models
{
    public enum OrderStatus
    {
        Новый,
        ВОбработке,
        Отправлен,
        Доставлен,
        Отменен
    }

    public class Order
    {
        public Order()
        {
            OrderDate = DateTime.Now;
            Status = OrderStatus.Новый;
            OrderItems = new List<OrderItem>();
            TrackingNumber = GenerateTrackingNumber();
            OrderNumber = GenerateOrderNumber();
        }

        public string GenerateTrackingNumber()
        {
            return $"TN-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 4)}";
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string TrackingNumber { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public string ShippingAddress { get; set; }

        public string PickupAddress { get; set; }

        public int? PickupPointId { get; set; }

        [ForeignKey("PickupPointId")]
        public virtual PickupPoint PickupPoint { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        [NotMapped]
        public string CollectionStatus { get; set; }
    }
}
