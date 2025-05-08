using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class CinemaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên rạp không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên rạp tối đa 100 ký tự.")]
        [Display(Name = "Tên rạp")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(200, ErrorMessage = "Địa chỉ tối đa 200 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }
    }
}