using ManagentCinemaSystem.Models; // Namespace cho Booking
using Microsoft.AspNetCore.Mvc.Rendering; // Cho SelectList
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Staff // Hoặc namespace ViewModel chung
{
    public class BookingListViewModel
    {
        public PaginatedList<ManagentCinemaSystem.Models.Booking> Bookings { get; set; } // Danh sách booking đã phân trang

        // --- Các tham số Filter ---
        [Display(Name = "Tìm kiếm (Mã vé, Tên/Email KH)")]
        public string SearchTerm { get; set; }

        [Display(Name = "Trạng thái")]
        public string StatusFilter { get; set; } // PendingPayment, Confirmed, CancelledByCustomer, etc.

        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? DateFromFilter { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? DateToFilter { get; set; }

        [Display(Name = "Rạp")]
        public int? CinemaFilter { get; set; } // Lọc theo CinemaId

        [Display(Name = "Phim")]
        public int? MovieFilter { get; set; } // Lọc theo MovieId (phức tạp hơn vì Booking không trực tiếp link Movie)


        // --- SelectLists cho Dropdown Filters ---
        public SelectList StatusOptions { get; set; }
        public SelectList CinemaOptions { get; set; }
        // public SelectList MovieOptions { get; set; } // Sẽ cần load danh sách phim

        // Thuộc tính để lưu trữ các giá trị filter hiện tại cho phân trang
        public string CurrentSearchTerm { get; set; }
        public string CurrentStatusFilter { get; set; }
        public DateTime? CurrentDateFromFilter { get; set; }
        public DateTime? CurrentDateToFilter { get; set; }
        public int? CurrentCinemaFilter { get; set; }
        public int? CurrentMovieFilter { get; set; }
    }
}