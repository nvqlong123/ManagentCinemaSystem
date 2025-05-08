using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
