using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Booking
{
    // ViewModel cho trang xác nhận đặt vé
    public class BookingConfirmationViewModel
    {
        public int ShowId { get; set; }
        public string MovieTitle { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime StartTime { get; set; }
        public string RoomName { get; set; }
        public string CinemaName { get; set; }

        // Danh sách ID ghế đã chọn (truyền ẩn đi)
        public List<int> SelectedShowSeatIds { get; set; } = new List<int>();

        // Danh sách chi tiết ghế đã chọn (để hiển thị)
        public List<ShowSeatViewModel> SelectedSeatDetails { get; set; } = new List<ShowSeatViewModel>();

        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public int TotalCost { get; set; }

        // Có thể thêm thông tin người dùng ở đây nếu cần
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
    }
}