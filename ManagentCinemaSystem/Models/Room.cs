using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phòng chiếu không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên phòng chiếu tối đa 100 ký tự.")]
        public string Name { get; set; }
        public int CinemaId { get; set; }
        public virtual Cinema Cinema { get; set; }

        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
    }

}
