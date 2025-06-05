// File: ViewModels/ShowDetailsViewModel.cs
using ManagentCinemaSystem.Models;
using System.Collections.Generic;

namespace ManagentCinemaSystem.ViewModels
{
    public class ShowDetailsViewModel
    {
        public Show Show { get; set; } // Đối tượng Show chính
        public Dictionary<int, string> SeatTypeNames { get; set; } // Danh sách tên loại ghế
        // Thêm các thuộc tính khác nếu cần
    }
}