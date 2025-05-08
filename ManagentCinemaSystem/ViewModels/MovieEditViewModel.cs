using ManagentCinemaSystem.Models;
using System; // Required for DateTime
using System.Collections.Generic; // Required for List
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class MovieEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phim không được để trống")]
        [StringLength(100, ErrorMessage = "Tên phim tối đa 100 ký tự")]
        [Display(Name = "Tên phim")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Tên đạo diễn không được để trống")]
        [StringLength(100, ErrorMessage = "Tên đạo diễn tối đa 100 ký tự")]
        [Display(Name = "Đạo diễn")]
        public string Director { get; set; }

        [Required(ErrorMessage = "Diễn viên không được để trống")]
        [StringLength(200, ErrorMessage = "Tên diễn viên tối đa 200 ký tự")]
        [Display(Name = "Diễn viên chính")]
        public string Actors { get; set; }

        [Required(ErrorMessage = "Thời lượng không được để trống")]
        [Range(1, 500, ErrorMessage = "Thời lượng phải từ 1 đến 500 phút")]
        [Display(Name = "Thời lượng (phút)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Ngày phát hành không được để trống")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày phát hành")]
        // FutureDate validation might not be strictly necessary for edit if movie is already released.
        // If it must remain editable to a future date only: [FutureDate]
        public DateTime ReleaseDate { get; set; }

        [Url(ErrorMessage = "URL poster không hợp lệ")]
        [StringLength(2048, ErrorMessage = "URL poster tối đa 2048 ký tự")]
        [Display(Name = "Poster (URL)")]
        public string PosterUrl { get; set; }

        [Display(Name = "Đang hoạt động (chiếu/sắp chiếu)")]
        public bool IsActive { get; set; }

        [Display(Name = "Thể loại")]
        public List<int> SelectedGenreIds { get; set; } = new List<int>();

        public List<Genre> AvailableGenres { get; set; } = new List<Genre>();
    }
}