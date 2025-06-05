using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Booking
{
    // ViewModel để biểu diễn một ghế cụ thể trong một suất chiếu trên giao diện chọn ghế
    public class ShowSeatViewModel
    {
        public int ShowSeatId { get; set; }
        public char SeatRow { get; set; }
        public int SeatCol { get; set; }
        public string SeatTypeName { get; set; }
        public int SeatTypeCost { get; set; }
        public bool IsBooked { get; set; }
        public bool IsSelected { get; set; } // Để JS sử dụng đánh dấu ghế đang chọn
    }
}