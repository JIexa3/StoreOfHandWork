using System;
using System.ComponentModel.DataAnnotations;

namespace StoreOfHandWork.Models
{
    public class ReturnPolicy
    {
        public ReturnPolicy()
        {
            Title = string.Empty;
            GeneralConditions = string.Empty;
            ExcludedCategories = string.Empty;
            RefundPolicy = string.Empty;
            ExchangePolicy = string.Empty;
            UpdatedBy = string.Empty;
            LastUpdated = DateTime.Now;
        }
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int ReturnPeriodDays { get; set; }

        [Required]
        [StringLength(2000)]
        public string GeneralConditions { get; set; } = string.Empty;

        [StringLength(2000)]
        public string ExcludedCategories { get; set; } = string.Empty;

        [StringLength(2000)]
        public string RefundPolicy { get; set; } = string.Empty;

        [StringLength(2000)]
        public string ExchangePolicy { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        [StringLength(100)]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
