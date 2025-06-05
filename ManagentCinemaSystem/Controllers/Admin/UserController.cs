// File: Controllers/UserController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels.Admin; // Đảm bảo namespace này đúng
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được controller này
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        private async Task<string> GetUserTypeAsync(User user)
        {
            if (user is ManagentCinemaSystem.Models.Admin) return "Admin";
            if (user is ManagentCinemaSystem.Models.Staff) return "Staff";
            if (user is ManagentCinemaSystem.Models.Customer) return "Customer";

            var entry = _context.Entry(user);
            if (entry.State == EntityState.Detached)
            {
                var dbUser = await _context.Users.FindAsync(user.Id);
                if (dbUser is ManagentCinemaSystem.Models.Admin) return "Admin";
                if (dbUser is ManagentCinemaSystem.Models.Staff) return "Staff";
                if (dbUser is ManagentCinemaSystem.Models.Customer) return "Customer";
                return "Unknown";
            }
            var userTypeProperty = entry.Property("UserType");
            if (userTypeProperty.CurrentValue == null || entry.State == EntityState.Modified)
            {
                await entry.ReloadAsync();
            }
            return userTypeProperty.CurrentValue?.ToString() ?? "Unknown";
        }

        // GET: /User hoặc /User/Index
        public async Task<IActionResult> Index(string searchTerm, string roleFilter, string statusFilter)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.UserName.ToLower().Contains(searchTermLower) ||
                    u.Email.ToLower().Contains(searchTermLower) ||
                    u.Name.ToLower().Contains(searchTermLower));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                usersQuery = usersQuery.Where(u => u.Status == statusFilter);
            }

            var allUsers = await usersQuery.OrderBy(u => u.UserName).ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string userRole = roles.FirstOrDefault();

                if (!string.IsNullOrEmpty(roleFilter) && userRole != roleFilter)
                {
                    continue;
                }

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Status = user.Status,
                    EmailConfirmed = user.EmailConfirmed,
                    Role = userRole,
                    UserType = await GetUserTypeAsync(user)
                });
            }

            var definedUserStatuses = new List<string> { "Active", "Inactive", "Banned" };
            var allDbRoles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();


            var model = new UserIndexViewModel
            {
                Users = userViewModels,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,
                RoleList = new SelectList(allDbRoles, roleFilter),
                StatusList = new SelectList(definedUserStatuses, statusFilter)
            };
            return View(model);
        }

        // GET: /User/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Status = user.Status,
                EmailConfirmed = user.EmailConfirmed,
                Role = roles.FirstOrDefault(),
                UserType = await GetUserTypeAsync(user)
            };
            return View(model);
        }

        // GET: /User/Create
        public async Task<IActionResult> Create()
        {
            var definedUserStatuses = new List<string> { "Active", "Inactive" };
            var userTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Customer", Text = "Khách hàng" },
                new SelectListItem { Value = "Staff", Text = "Nhân viên" },
                new SelectListItem { Value = "Admin", Text = "Quản trị viên" }
            };
            var roles = await _roleManager.Roles.OrderBy(r => r.Name)
                                          .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                                          .ToListAsync();

            var model = new UserCreateViewModel
            {
                AvailableStatuses = new SelectList(definedUserStatuses),
                AvailableUserTypes = new SelectList(userTypes, "Value", "Text"),
                AvailableRoles = new SelectList(roles, "Value", "Text")
            };
            return View(model);
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                User newUser;
                // Ensure InitialRole is set based on UserType if not explicitly provided
                // or validate that it's appropriate for the UserType.
                switch (model.UserType)
                {
                    case "Admin":
                        newUser = new ManagentCinemaSystem.Models.Admin { UserName = model.UserName, Email = model.Email, Name = model.Name, Phone = model.Phone, Status = model.Status, EmailConfirmed = true };
                        model.InitialRole = "Admin"; // Admins must have Admin role
                        break;
                    case "Staff":
                        newUser = new ManagentCinemaSystem.Models.Staff { UserName = model.UserName, Email = model.Email, Name = model.Name, Phone = model.Phone, Status = model.Status, EmailConfirmed = true };
                        if (string.IsNullOrEmpty(model.InitialRole) || model.InitialRole == "Admin" || model.InitialRole == "Customer")
                        { // Staff cannot be Admin or Customer by default from this form
                            ModelState.AddModelError("InitialRole", "Vui lòng chọn vai trò phù hợp cho Nhân viên (ví dụ: 'Staff').");
                            await RepopulateCreateViewModelAsync(model);
                            return View(model);
                        }
                        break;
                    case "Customer":
                    default:
                        newUser = new ManagentCinemaSystem.Models.Customer { UserName = model.UserName, Email = model.Email, Name = model.Name, Phone = model.Phone, Status = model.Status, EmailConfirmed = false };
                        model.InitialRole = "Customer"; // Customers default to Customer role
                        break;
                }

                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.InitialRole))
                    {
                        if (!await _roleManager.RoleExistsAsync(model.InitialRole))
                        {
                            ModelState.AddModelError("InitialRole", $"Vai trò '{model.InitialRole}' không tồn tại.");
                            await RepopulateCreateViewModelAsync(model);
                            return View(model);
                        }
                        await _userManager.AddToRoleAsync(newUser, model.InitialRole);
                    }

                    TempData["Success"] = $"Tạo người dùng '{newUser.UserName}' thành công.";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            await RepopulateCreateViewModelAsync(model);
            return View(model);
        }

        private async Task RepopulateCreateViewModelAsync(UserCreateViewModel model)
        {
            var definedUserStatuses = new List<string> { "Active", "Inactive" };
            var userTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Customer", Text = "Khách hàng" },
                new SelectListItem { Value = "Staff", Text = "Nhân viên" },
                new SelectListItem { Value = "Admin", Text = "Quản trị viên" }
            };
            var roles = await _roleManager.Roles.OrderBy(r => r.Name)
                                          .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                                          .ToListAsync();
            model.AvailableStatuses = new SelectList(definedUserStatuses, model.Status);
            model.AvailableUserTypes = new SelectList(userTypes, "Value", "Text", model.UserType);
            model.AvailableRoles = new SelectList(roles, "Value", "Text", model.InitialRole);
        }

        // GET: /User/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var definedUserStatuses = new List<string> { "Active", "Inactive", "Banned" };

            var model = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Status = user.Status,
                EmailConfirmed = user.EmailConfirmed,
                AvailableStatuses = new SelectList(definedUserStatuses, user.Status)
            };
            return View(model);
        }

        // POST: /User/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var definedUserStatuses = new List<string> { "Active", "Inactive", "Banned" };
                model.AvailableStatuses = new SelectList(definedUserStatuses, model.Status);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound($"User with ID '{model.Id}' not found.");

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId && model.Status != "Active")
            {
                ModelState.AddModelError("Status", "Bạn không thể vô hiệu hóa tài khoản của chính mình.");
                var definedUserStatuses = new List<string> { "Active", "Inactive", "Banned" }; // Re-populate for view
                model.AvailableStatuses = new SelectList(definedUserStatuses, model.Status);
                return View(model);
            }
            // Prevent editing username of Admin type users by other admins
            if (user is ManagentCinemaSystem.Models.Admin && user.Id != currentUserId && user.UserName != model.UserName)
            {
                ModelState.AddModelError("UserName", "Không thể thay đổi tên đăng nhập của tài khoản Quản trị viên hệ thống khác.");
                var definedUserStatuses = new List<string> { "Active", "Inactive", "Banned" };
                model.AvailableStatuses = new SelectList(definedUserStatuses, model.Status);
                return View(model);
            }


            user.UserName = model.UserName;
            user.Name = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.Phone;
            user.Status = model.Status;
            user.EmailConfirmed = model.EmailConfirmed;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Cập nhật thông tin người dùng '{user.UserName}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            var allStatuses = new List<string> { "Active", "Inactive", "Banned" }; // Re-populate for view
            model.AvailableStatuses = new SelectList(allStatuses, model.Status);
            return View(model);
        }

        // GET: /User/ManageRole/{id}
        public async Task<IActionResult> ManageRole(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRoleName = userRoles.FirstOrDefault();

            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name)
                                        .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                                        .ToListAsync();

            var viewModel = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                SelectedRoleName = currentRoleName,
                AvailableRoles = new SelectList(allRoles, "Value", "Text", currentRoleName)
            };
            return View(viewModel);
        }

        // POST: /User/ManageRole/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRole(UserRoleViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound($"User with ID '{model.UserId}' not found.");

            if (!ModelState.IsValid) // Includes check for [Required] SelectedRoleName
            {
                await RepopulateManageRoleViewModelAsync(model, model.SelectedRoleName);
                return View(model);
            }

            var currentUserId = _userManager.GetUserId(User);
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();
            bool isUserTypeAdmin = user is ManagentCinemaSystem.Models.Admin;

            if (user.Id == currentUserId && model.SelectedRoleName != "Admin" && currentRole == "Admin")
            {
                TempData["Error"] = "Bạn không thể tự xóa vai trò Admin của chính mình.";
                await RepopulateManageRoleViewModelAsync(model, currentRole); // Revert to original role in dropdown
                return View(model);
            }
            if (isUserTypeAdmin && model.SelectedRoleName != "Admin")
            {
                TempData["Error"] = "Không thể thay đổi vai trò của tài khoản Quản trị viên hệ thống sang vai trò khác 'Admin'.";
                await RepopulateManageRoleViewModelAsync(model, currentRole); // Revert to original role
                return View(model);
            }

            // Ensure the selected role actually exists
            if (!string.IsNullOrEmpty(model.SelectedRoleName) && !await _roleManager.RoleExistsAsync(model.SelectedRoleName))
            {
                ModelState.AddModelError("SelectedRoleName", "Vai trò được chọn không hợp lệ.");
                await RepopulateManageRoleViewModelAsync(model, model.SelectedRoleName);
                return View(model);
            }


            var removalResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removalResult.Succeeded)
            {
                AddErrors(removalResult);
                await RepopulateManageRoleViewModelAsync(model, model.SelectedRoleName);
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.SelectedRoleName))
            {
                var additionResult = await _userManager.AddToRoleAsync(user, model.SelectedRoleName);
                if (!additionResult.Succeeded)
                {
                    AddErrors(additionResult);
                    // Attempt to re-add old role? Or just show error and user has no roles.
                    await RepopulateManageRoleViewModelAsync(model, model.SelectedRoleName);
                    return View(model);
                }
            }

            TempData["Success"] = $"Cập nhật vai trò cho người dùng '{user.UserName}' thành công.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        private async Task RepopulateManageRoleViewModelAsync(UserRoleViewModel model, string currentSelectedRole)
        {
            var allRolesListItems = await _roleManager.Roles.OrderBy(r => r.Name)
                                        .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                                        .ToListAsync();
            model.AvailableRoles = new SelectList(allRolesListItems, "Value", "Text", currentSelectedRole);
        }

        // POST: /User/ToggleStatusAction/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusAction(string id, string targetStatus = "Toggle")
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Bạn không thể thay đổi trạng thái của chính mình theo cách này.";
                return RedirectToAction(nameof(Index));
            }
            if (user is ManagentCinemaSystem.Models.Admin && user.Id != currentUserId)
            {
                TempData["Error"] = "Không thể thay đổi trạng thái của tài khoản Quản trị viên hệ thống khác.";
                return RedirectToAction(nameof(Index));
            }

            string oldStatus = user.Status;
            string newStatus = user.Status;

            if (targetStatus == "Toggle")
            {
                newStatus = user.Status == "Active" ? "Inactive" : "Active";
            }
            else if (new[] { "Active", "Inactive", "Banned" }.Contains(targetStatus))
            {
                newStatus = targetStatus;
            }
            else
            {
                TempData["Error"] = "Trạng thái mục tiêu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            user.Status = newStatus;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã thay đổi trạng thái của người dùng '{user.UserName}' từ '{oldStatus}' thành '{newStatus}'.";
            }
            else
            {
                AddErrors(result);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }
            if (user is ManagentCinemaSystem.Models.Admin)
            {
                TempData["Error"] = "Không thể xóa tài khoản Quản trị viên hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UserViewModel // Using UserViewModel for simplicity, could be a smaller specific ViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name
            };
            return View(model);
        }

        // POST: /User/Delete/{id}
        [HttpPost("User/Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("User ID cannot be null or empty.");
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng để xóa.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }
            if (user is ManagentCinemaSystem.Models.Admin)
            {
                TempData["Error"] = "Không thể xóa tài khoản Quản trị viên hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa người dùng '{user.UserName}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = $"Lỗi khi xóa người dùng '{user.UserName}'.";
            AddErrors(result);
            return RedirectToAction(nameof(Index));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}