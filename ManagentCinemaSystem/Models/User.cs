using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ManagentCinemaSystem.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; }
    }
    public class Customer : User
    {
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public class Staff : User
    {
    }
    public class Admin : User
    {
    }
}
