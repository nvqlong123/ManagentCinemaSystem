using System;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Booking
{
    public class PaymentQRViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string MovieTitle { get; set; }
        public DateTime ShowStartTime { get; set; }
        public string RoomName { get; set; }
        public string CinemaName { get; set; }

        public int TotalCost { get; set; }
        public string QrCodeBase64Image { get; set; } // Ảnh QR dạng base64
        public string BankName { get; set; } // Tên ngân hàng của bạn
        public string BankAccountNumber { get; set; } // Số tài khoản của bạn
        public string BankAccountName { get; set; } // Tên chủ tài khoản của bạn
        public string PaymentContent { get; set; } // Nội dung chuyển khoản (chứa BookingCode)
        public DateTime PaymentDeadline { get; set; }
    }
}