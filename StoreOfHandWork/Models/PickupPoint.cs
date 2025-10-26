using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreOfHandWork.Models
{
    public class PickupPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [StringLength(100)]
        public string WorkingHours { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
} 