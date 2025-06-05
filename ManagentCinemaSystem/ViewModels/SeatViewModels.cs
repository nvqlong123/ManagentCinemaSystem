// File: ViewModels/SeatViewModels.cs
using ManagentCinemaSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class SeatViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Số hàng không được để trống.")]
        [RegularExpression(@"^[A-Z]$", ErrorMessage = "Hàng phải là một chữ cái viết hoa (A-Z).")]
        [Display(Name = "Hàng")]
        public char Row { get; set; }

        [Required(ErrorMessage = "Số cột không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số cột phải từ 1 đến 100.")]
        [Display(Name = "Cột")]
        public int Col { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại ghế.")]
        [Display(Name = "Loại ghế")]
        public int SeatTypeId { get; set; }

        public int RoomId { get; set; } // Hidden field, set by controller
        public string RoomName { get; set; } // For display
        public string CinemaName { get; set; } // For display

        public IEnumerable<SelectListItem>? AvailableSeatTypes { get; set; }
    }

    public class ManageSeatsViewModel
    {
        public int RoomId { get; set; }
        public string? RoomName { get; set; }
        public int? CinemaId { get; set; } // <<<===== THÊM THUỘC TÍNH NÀY
        public string? CinemaName { get; set; }
        public List<SeatViewModel> ?Seats { get; set; } = new List<SeatViewModel>();
        public IEnumerable<SelectListItem>? AvailableSeatTypes { get; set; }

        // For batch creation
        [Display(Name = "Hàng bắt đầu (A-Z)")]
        [RegularExpression(@"^[A-Z]$", ErrorMessage = "Hàng phải là một chữ cái viết hoa (A-Z).")]
        [Required(ErrorMessage = "Vui lòng nhập hàng bắt đầu.")] // Thêm required
        public char? BatchStartRow { get; set; }

        [Display(Name = "Hàng kết thúc (A-Z)")]
        [RegularExpression(@"^[A-Z]$", ErrorMessage = "Hàng phải là một chữ cái viết hoa (A-Z).")]
        [Required(ErrorMessage = "Vui lòng nhập hàng kết thúc.")] // Thêm required
        public char? BatchEndRow { get; set; }

        [Display(Name = "Cột bắt đầu (1-100)")]
        [Range(1, 100)]
        [Required(ErrorMessage = "Vui lòng nhập cột bắt đầu.")] // Thêm required
        public int? BatchStartCol { get; set; }

        [Display(Name = "Cột kết thúc (1-100)")]
        [Range(1, 100)]
        [Required(ErrorMessage = "Vui lòng nhập cột kết thúc.")] // Thêm required
        public int? BatchEndCol { get; set; }

        [Display(Name = "Loại ghế mặc định")]
        [Required(ErrorMessage = "Vui lòng chọn loại ghế mặc định.")] // Thêm required
        public int? BatchDefaultSeatTypeId { get; set; }
    }
}