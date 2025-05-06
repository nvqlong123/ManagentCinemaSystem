using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class Cinema
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên rạp không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên rạp tối đa 100 ký tự.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(200, ErrorMessage = "Địa chỉ tối đa 200 ký tự.")]
        public string Address { get; set; }

        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
