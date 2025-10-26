using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreOfHandWork.Models
{
    public enum ReturnStatus
    {
        ЗаявкаОтправлена,     // Заявка отправлена
        Одобрено,                // Одобрено
        Отклонено,               // Отклонено
        ТоварПолучен,          // Товар получен
        ВозвратЗавершен,      // Возврат средств выполнен
        ОбменЗавершен,        // Обмен оформлен
        Отменено                 // Отменено
    }

    public enum ReturnReason
    {
        Брак,                 // Брак/дефект товара
        НеВерныйРазмер,       // Не подошел размер
        НеВерныйТовар,       // Получен не тот товар
        НеСоответствуетОписанию, // Не соответствует описанию
        НашлиДешевле,         // Нашли дешевле
        Передумал,            // Передумал
        Другое                // Другое
    }

    public enum ReturnType
    {
        Возврат,              // Возврат денежных средств
        Обмен              // Обмен на другой товар
    }

    public class ReturnRequest
    {
        public ReturnRequest()
        {
            RequestDate = DateTime.Now;
            Status = ReturnStatus.ЗаявкаОтправлена;
            AdditionalComments = string.Empty;
            AdminComments = string.Empty;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("OrderItem")]
        public int OrderItemId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public ReturnStatus Status { get; set; }

        [Required]
        public ReturnReason Reason { get; set; }

        [Required]
        public ReturnType Type { get; set; }

        [StringLength(1000)]
        public string AdditionalComments { get; set; } = string.Empty;

        // Для обмена товара
        [ForeignKey("ExchangeProduct")]
        public int? ExchangeProductId { get; set; }

        public DateTime? ReviewDate { get; set; }
        
        [StringLength(1000)]
        public string AdminComments { get; set; } = string.Empty;

        public DateTime? CompletionDate { get; set; }

        public virtual OrderItem OrderItem { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Product? ExchangeProduct { get; set; }
    }
}
