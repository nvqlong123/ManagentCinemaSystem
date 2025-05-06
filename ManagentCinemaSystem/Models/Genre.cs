using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Genre
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên thể loại tối đa 50 ký tự.")]
        public string Name { get; set; }

        // Liên kết nhiều-nhiều với Movie thông qua MovieGenre
        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }
}
