// File: Controllers/CinemaController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels; // Đảm bảo namespace này đúng cho CinemaViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers // Bỏ .Admin nếu bạn muốn, hoặc giữ để tổ chức file
{
    // KHÔNG CÓ [Area("Admin")]
    [Authorize(Roles = "Admin")] // Vẫn giữ Authorize vì đây là controller cho Admin
    public class CinemaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CinemaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cinema hoặc /Cinema/Index
        public async Task<IActionResult> Index()
        {
            var cinemas = await _context.Cinemas
                               .Include(c => c.Rooms) // << KIỂM TRA LẠI DÒNG NÀY
                               .OrderBy(c => c.Name)
                               .ToListAsync();
            return View(cinemas);
        }

        // GET: /Cinema/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var cinema = await _context.Cinemas
                                       .Include(c => c.Rooms) // Để hiển thị danh sách phòng trong chi tiết rạp
                                       .FirstOrDefaultAsync(c => c.Id == id);
            if (cinema == null)
            {
                return NotFound($"Không tìm thấy rạp với ID {id}.");
            }
            return View(cinema); // Views/Cinema/Details.cshtml
        }

        // GET: /Cinema/Create
        public IActionResult Create()
        {
            return View(new CinemaViewModel()); // Views/Cinema/Create.cshtml
        }

        // POST: /Cinema/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CinemaViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Cinemas.AnyAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Address.ToLower() == model.Address.ToLower()))
                {
                    ModelState.AddModelError(string.Empty, "Rạp chiếu phim với tên và địa chỉ này đã tồn tại.");
                    return View(model);
                }

                var cinema = new Cinema { Name = model.Name, Address = model.Address };
                _context.Cinemas.Add(cinema);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm rạp '{cinema.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: /Cinema/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
            {
                return NotFound($"Không tìm thấy rạp với ID {id}.");
            }
            var model = new CinemaViewModel { Id = cinema.Id, Name = cinema.Name, Address = cinema.Address };
            return View(model); // Views/Cinema/Edit.cshtml
        }

        // POST: /Cinema/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CinemaViewModel model)
        {
            if (ModelState.IsValid)
            {
                var cinema = await _context.Cinemas.FindAsync(model.Id);
                if (cinema == null)
                {
                    return NotFound($"Không tìm thấy rạp với ID {model.Id} để cập nhật.");
                }

                if (await _context.Cinemas.AnyAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Address.ToLower() == model.Address.ToLower() && c.Id != model.Id))
                {
                    ModelState.AddModelError(string.Empty, "Rạp chiếu phim khác với tên và địa chỉ này đã tồn tại.");
                    return View(model);
                }

                cinema.Name = model.Name;
                cinema.Address = model.Address;

                try
                {
                    _context.Update(cinema);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật rạp '{cinema.Name}' thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Cinemas.Any(e => e.Id == model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể lưu thay đổi. Rạp có thể đã bị xóa hoặc cập nhật bởi người khác.");
                        // Bạn có thể load lại dữ liệu mới nhất và hiển thị cho người dùng
                        // var currentDbValues = await _context.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == model.Id);
                        // model.Name = currentDbValues?.Name; // etc.
                        return View(model);
                    }
                }
            }
            return View(model);
        }

        // GET: /Cinema/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var cinema = await _context.Cinemas.Include(c => c.Rooms).FirstOrDefaultAsync(c => c.Id == id);
            if (cinema == null)
            {
                return NotFound($"Không tìm thấy rạp với ID {id}.");
            }
            // Truyền thông tin về việc rạp có phòng hay không cho View để hiển thị cảnh báo
            ViewBag.CanDelete = !cinema.Rooms.Any();
            return View(cinema); // Views/Cinema/Delete.cshtml
        }

        // POST: /Cinema/Delete/5 (ActionName("Delete") giúp form post đến đây)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cinema = await _context.Cinemas.Include(c => c.Rooms).FirstOrDefaultAsync(m => m.Id == id);
            if (cinema == null)
            {
                TempData["Error"] = "Không tìm thấy rạp chiếu phim để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (cinema.Rooms.Any())
            {
                TempData["Error"] = $"Không thể xóa rạp '{cinema.Name}' vì vẫn còn phòng chiếu liên kết. Vui lòng xóa hoặc di chuyển các phòng chiếu trước.";
                // Quay lại view Delete với thông tin đầy đủ để hiển thị lỗi
                ViewBag.CanDelete = false; // Để view biết không thể xóa
                return View("Delete", cinema); // Truyền lại model cinema cho view "Delete"
            }

            try
            {
                _context.Cinemas.Remove(cinema);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa rạp '{cinema.Name}' thành công.";
            }
            catch (DbUpdateException ex) // Trường hợp hiếm nếu check ở trên không bắt được
            {
                TempData["Error"] = $"Lỗi khi xóa rạp: {ex.InnerException?.Message ?? ex.Message}.";
                ViewBag.CanDelete = !cinema.Rooms.Any(); // Re-evaluate
                return View("Delete", cinema);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}