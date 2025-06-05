using ManagentCinemaSystem.Models; // For User model
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Admin
{
    public class UserIndexViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public string SearchTerm { get; set; }
        public string RoleFilter { get; set; } // Role Name
        public string StatusFilter { get; set; } // e.g., "Active", "Inactive"

        public SelectList RoleList { get; set; }
        public SelectList StatusList { get; set; }
    }

    public class UserViewModel // For displaying user info in a list or details
    {
        public string Id { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Display(Name = "Họ tên")]
        public string Name { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Loại tài khoản")]
        public string UserType { get; set; }

        [Display(Name = "Email đã xác thực")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; } // Changed from IList<string> to string for single role
    }

    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ tên")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Xác thực Email")]
        public bool EmailConfirmed { get; set; }

        public SelectList AvailableStatuses { get; set; }
    }

    public class UserRoleViewModel // Renamed for clarity, handles single role assignment
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn một vai trò.")]
        [Display(Name = "Vai trò")]
        public string SelectedRoleName { get; set; } // Single role selection

        public SelectList? AvailableRoles { get; set; }
    }


    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ tên")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Vui lòng chọn loại tài khoản.")]
        [Display(Name = "Loại tài khoản")]
        public string UserType { get; set; }

        [Display(Name = "Vai trò ban đầu")]
        // If UserType implies a role (e.g., Admin type IS Admin role), this might be redundant
        // Or used if Staff/Customer can have one specific role from a list.
        // For simplicity with single role, this might be directly linked to UserType or set post-creation.
        // Let's make it explicit for now.
        [Required(ErrorMessage = "Vui lòng chọn vai trò ban đầu.")]
        public string InitialRole { get; set; }

        public SelectList? AvailableStatuses { get; set; }
        public SelectList? AvailableUserTypes { get; set; }
        public SelectList? AvailableRoles { get; set; } // For selecting a single role
    }
}