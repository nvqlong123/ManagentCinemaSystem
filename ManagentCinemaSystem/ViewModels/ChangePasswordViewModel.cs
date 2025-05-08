using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Compare("NewPassword"), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
