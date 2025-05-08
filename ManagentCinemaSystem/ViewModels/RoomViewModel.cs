using ManagentCinemaSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class RoomViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phòng chiếu không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên phòng chiếu tối đa 100 ký tự.")]
        [Display(Name = "Tên phòng")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn rạp.")]
        [Display(Name = "Thuộc rạp")]
        public int CinemaId { get; set; }

        public string CinemaName { get; set; } // For display purposes

        public IEnumerable<SelectListItem> CinemasList { get; set; } // For dropdown in Create/Edit views
    }

    public class RoomIndexViewModel
    {
        public Cinema Cinema { get; set; }
        public List<Room> Rooms { get; set; }
    }
}