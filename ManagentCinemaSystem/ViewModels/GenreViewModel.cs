using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class GenreViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên thể loại tối đa 50 ký tự.")]
        [Display(Name = "Tên thể loại")]
        public string Name { get; set; }
    }
}