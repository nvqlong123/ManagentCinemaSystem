using System; // Đảm bảo using System
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cho DatabaseGenerated

namespace ManagentCinemaSystem.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        // Mã đặt vé duy nhất, dễ đọc hơn ID, dùng cho nội dung QR
        [Required]
        [StringLength(50)]
        public string BookingCode { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(50)]
        public string Status { get; set; } // Ví dụ: PendingPayment, Confirmed, Cancelled, Expired

        public int TotalCost { get; set; }

        [Required(ErrorMessage = "Thời gian đặt vé không được để trống.")]
        public DateTime Purchased { get; set; } // Thời điểm khách hàng bắt đầu quy trình (tạo QR)

        public DateTime? PaymentDeadline { get; set; } // Thời hạn thanh toán

        [Required(ErrorMessage = "Loại giao dịch không được để trống.")]
        [StringLength(20)]
        public string TransactionType { get; set; } // Ví dụ: QRCode, Cash (nếu có)

        public string? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public string? StaffIdConfirmed { get; set; } // ID của nhân viên đã xác nhận
        [ForeignKey("StaffIdConfirmed")]
        public virtual Staff? StaffConfirmed { get; set; } // Optional: Tham chiếu đến nhân viên

        public virtual ICollection<ShowSeat> ShowSeats { get; set; } = new List<ShowSeat>();
    }
}