using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; }

        public int TotalCost { get; set; }
        [Required(ErrorMessage = "Thời gian đặt vé không được để trống.")]
        public DateTime Purchased { get; set; }

        [Required(ErrorMessage = "Loại giao dịch không được để trống.")]
        public string TransactionType { get; set; } // Cash or QR
        public string? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<ShowSeat> ShowSeats { get; set; } = new List<ShowSeat>();
    }

}
