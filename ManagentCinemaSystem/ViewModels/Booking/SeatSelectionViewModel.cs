using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Booking
{
    // ViewModel cho trang chọn ghế
    public class SeatSelectionViewModel
    {
        public int ShowId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime StartTime { get; set; }
        public string RoomName { get; set; }
        public string CinemaName { get; set; }

        // Danh sách các ghế trong phòng chiếu cho suất chiếu này
        public List<ShowSeatViewModel> Seats { get; set; } = new List<ShowSeatViewModel>();

        // Thông tin bổ sung để vẽ sơ đồ ghế (nếu cần)
        public char MaxRow { get; set; }
        public int MaxCol { get; set; }

        // Dùng để nhận danh sách ID ghế đã chọn từ form
        [Required(ErrorMessage = "Vui lòng chọn ít nhất một ghế.")]
        public List<int> SelectedShowSeatIds { get; set; } = new List<int>();
    }
}