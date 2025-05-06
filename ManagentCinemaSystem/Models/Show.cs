using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Show
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc không được để trống.")]
        public DateTime EndTime { get; set; }

        public int MovieId { get; set; }
        public virtual Movie Movie { get; set; }

        public int RoomId { get; set; }
        public virtual Room Room { get; set; }
        public virtual ICollection<ShowSeat> ShowSeats { get; set; } = new List<ShowSeat>();
    }
}
