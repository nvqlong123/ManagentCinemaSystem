using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phim không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên phim tối đa 100 ký tự.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Đạo diễn không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên đạo diễn tối đa 100 ký tự.")]
        public string Director { get; set; }
        [Required(ErrorMessage = "Diễn viên không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên diễn viên tối đa 200 ký tự.")]
        public string Actor { get; set; }

        [Url(ErrorMessage = "Đường dẫn poster không hợp lệ.")]
        public string Poster { get; set; }

        [Required(ErrorMessage = "Ngày phát hành không được để trống.")]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }
        [Required(ErrorMessage = "Thời gian khởi chiếu không được để trống.")]
        [Range(1, 500, ErrorMessage = "Thời lượng phải từ 1 đến 500 phút.")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Thể loại không được để trống.")]
        [StringLength(50, ErrorMessage = "Thể loại tối đa 50 ký tự.")]
        public string Genre { get; set; }

        public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
    }
}
