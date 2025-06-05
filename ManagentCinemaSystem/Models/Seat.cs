using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Seat
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Số hàng không được để trống.")]
        [Range('A', 'Z', ErrorMessage = "Số hàng phải từ A đến Z.")]
        public char Row { get; set; }
        [Required(ErrorMessage = "Số cột không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số cột phải từ 1 đến 100.")]
        public int Col { get; set; }

        public int SeatTypeId { get; set; }
        public virtual SeatType SeatType { get; set; }

        public int RoomId { get; set; }
        public virtual Room Room { get; set; }
    }
}
