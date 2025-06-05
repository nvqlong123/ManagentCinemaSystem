using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class SeatType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên loại ghế không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên loại ghế tối đa 50 ký tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá ghế không được để trống.")]
        [Range(0, 1000000, ErrorMessage = "Giá ghế phải từ 0 đến 1,000,000 VNĐ.")]
        public int Cost { get; set; }

        public ICollection<Seat> Seats { get; set; }
    }

}
