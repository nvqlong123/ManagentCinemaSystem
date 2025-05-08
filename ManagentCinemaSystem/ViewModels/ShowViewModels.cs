using ManagentCinemaSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class ShowViewModel // For display in lists
    {
        public int Id { get; set; }
        public string MovieTitle { get; set; }
        public string RoomName { get; set; }
        public string CinemaName { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime StartTime { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime EndTime { get; set; }
        public int MovieDuration { get; set; }
    }

    public class ShowCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phim.")]
        [Display(Name = "Phim")]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn rạp.")]
        [Display(Name = "Rạp")]
        public int CinemaId { get; set; } // Added for cascading dropdown

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu.")]
        [Display(Name = "Phòng chiếu")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày chiếu.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày chiếu")]
        public DateTime ShowDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
        [DataType(DataType.Time)]
        [Display(Name = "Giờ bắt đầu")]
        public DateTime StartTimeOnly { get; set; } = DateTime.Today.Date.AddHours(9); // Default 9 AM

        public IEnumerable<SelectListItem> AvailableMovies { get; set; }
        public IEnumerable<SelectListItem> AvailableCinemas { get; set; }
        public IEnumerable<SelectListItem> AvailableRooms { get; set; } // To be populated by AJAX or on cinema selection
    }

    public class ShowEditViewModel : ShowCreateViewModel
    {
        public int Id { get; set; }
        public int OriginalMovieDuration { get; set; } // To display and compare
    }

    public class ShowIndexViewModel
    {
        public List<ShowViewModel> Shows { get; set; }
        // Filters
        public string MovieFilter { get; set; }
        public int? CinemaFilter { get; set; }
        public DateTime? DateFilter { get; set; }

        public IEnumerable<SelectListItem> MovieList { get; set; }
        public IEnumerable<SelectListItem> CinemaList { get; set; }
    }
}