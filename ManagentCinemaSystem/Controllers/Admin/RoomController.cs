// File: Controllers/RoomController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels; // Đảm bảo namespace này đúng cho RoomViewModel, RoomIndexViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers // Bỏ .Admin nếu bạn muốn
{
    [Authorize(Roles = "Admin")]
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Room/Index/{cinemaId}  HOẶC /Room?cinemaId=... (Tùy vào cách gọi link)
        // Chúng ta sẽ định nghĩa route rõ ràng hơn cho Index để có URL đẹp
        [HttpGet("Room/Index/{cinemaId:int}")] // Route này sẽ khớp /Room/Index/123
        [HttpGet("Room/{cinemaId:int}")]      // Route này sẽ khớp /Room/123 (coi như Index là mặc định)
                                              // Hoặc bạn có thể để trống và dùng /Room?cinemaId=123
        public async Task<IActionResult> Index(int cinemaId)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null)
            {
                TempData["Error"] = $"Không tìm thấy rạp với ID {cinemaId} để hiển thị phòng.";
                return RedirectToAction("Index", "Cinema"); // Chuyển về danh sách rạp
            }

            var rooms = await _context.Rooms
                                      .Where(r => r.CinemaId == cinemaId)
                                      .OrderBy(r => r.Name)
                                      .ToListAsync();

            var viewModel = new RoomIndexViewModel
            {
                Cinema = cinema,
                Rooms = rooms
            };

            // Sẽ tìm view trong Views/Room/Index.cshtml
            return View(viewModel);
        }

        // GET: /Room/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms
                                     .Include(r => r.Cinema)
                                     .Include(r => r.Seats)
                                     .ThenInclude(s => s.SeatType) // Để xem loại ghế
                                     .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return NotFound($"Không tìm thấy phòng với ID {id}.");
            }
            return View(room); // Views/Room/Details.cshtml
        }

        // GET: /Room/Create?cinemaId=1 (cinemaId được truyền qua query string)
        public async Task<IActionResult> Create(int cinemaId) // cinemaId là bắt buộc để tạo phòng
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null)
            {
                TempData["Error"] = "Rạp chiếu không hợp lệ để tạo phòng. Vui lòng chọn một rạp.";
                return RedirectToAction("Index", "Cinema"); // Chuyển về danh sách rạp
            }

            var model = new RoomViewModel
            {
                CinemaId = cinemaId,
                CinemaName = cinema.Name,
                // Không cần CinemasList nếu form Create chỉ cho phép tạo phòng cho cinemaId đã cho.
                // Nếu muốn cho phép chọn lại rạp trên form Create, thì mới cần CinemasList.
                // Để đơn giản, form Create sẽ chỉ cho rạp đã chọn.
            };
            return View(model); // Views/Room/Create.cshtml
        }

        // POST: /Room/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomViewModel model) // Model sẽ chứa CinemaId
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra cinemaId có hợp lệ không
                var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == model.CinemaId);
                if (!cinemaExists)
                {
                    ModelState.AddModelError("CinemaId", "Rạp chiếu đã chọn không hợp lệ.");
                    // Cần load lại CinemaName nếu ModelState không hợp lệ và trả về View
                    var cinema = await _context.Cinemas.FindAsync(model.CinemaId);
                    model.CinemaName = cinema?.Name; // Lấy lại tên rạp nếu có
                    return View(model);
                }

                if (await _context.Rooms.AnyAsync(r => r.Name.ToLower() == model.Name.ToLower() && r.CinemaId == model.CinemaId))
                {
                    ModelState.AddModelError("Name", "Tên phòng này đã tồn tại trong rạp đã chọn.");
                    var cinema = await _context.Cinemas.FindAsync(model.CinemaId);
                    model.CinemaName = cinema?.Name;
                    return View(model);
                }

                var room = new Room { Name = model.Name, CinemaId = model.CinemaId };
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm phòng '{room.Name}' vào rạp '{model.CinemaName ?? room.Cinema?.Name}' thành công.";
                return RedirectToAction(nameof(Index), new { cinemaId = model.CinemaId });
            }

            // Nếu ModelState không hợp lệ, load lại CinemaName để hiển thị trên view
            var currentCinema = await _context.Cinemas.FindAsync(model.CinemaId);
            model.CinemaName = currentCinema?.Name;
            return View(model);
        }

        // GET: /Room/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return NotFound($"Không tìm thấy phòng với ID {id}.");
            }

            var model = new RoomViewModel
            {
                Id = room.Id,
                Name = room.Name,
                CinemaId = room.CinemaId,
                CinemaName = room.Cinema?.Name, // Lấy tên rạp để hiển thị
                // Nếu muốn cho phép đổi rạp cho phòng, thì cần CinemasList ở đây
                // CinemasList = new SelectList(await _context.Cinemas.OrderBy(c=>c.Name).ToListAsync(), "Id", "Name", room.CinemaId)
            };
            return View(model); // Views/Room/Edit.cshtml
        }

        // POST: /Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomViewModel model)
        {
            if (ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(model.Id);
                if (room == null)
                {
                    return NotFound($"Không tìm thấy phòng với ID {model.Id} để cập nhật.");
                }

                // Kiểm tra cinemaId có hợp lệ không nếu cho phép thay đổi
                var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == model.CinemaId);
                if (!cinemaExists)
                {
                    ModelState.AddModelError("CinemaId", "Rạp chiếu đã chọn không hợp lệ.");
                    // Cần load lại CinemaName và CinemasList nếu có
                    var originalCinema = await _context.Cinemas.FindAsync(room.CinemaId); // Hoặc model.CinemaId nếu đã thay đổi
                    model.CinemaName = originalCinema?.Name;
                    // model.CinemasList = new SelectList(await _context.Cinemas.OrderBy(c=>c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
                    return View(model);
                }

                if (await _context.Rooms.AnyAsync(r => r.Name.ToLower() == model.Name.ToLower() && r.CinemaId == model.CinemaId && r.Id != model.Id))
                {
                    ModelState.AddModelError("Name", "Tên phòng này đã tồn tại trong rạp đã chọn.");
                    // Cần load lại CinemaName và CinemasList nếu có
                    var currentCinema = await _context.Cinemas.FindAsync(model.CinemaId);
                    model.CinemaName = currentCinema?.Name;
                    // model.CinemasList = new SelectList(await _context.Cinemas.OrderBy(c=>c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
                    return View(model);
                }

                room.Name = model.Name;
                // Chỉ cho phép thay đổi CinemaId nếu logic nghiệp vụ cho phép và bạn có dropdown cho nó
                // room.CinemaId = model.CinemaId; 

                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật phòng '{room.Name}' thành công.";
                    return RedirectToAction(nameof(Index), new { cinemaId = room.CinemaId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Rooms.Any(e => e.Id == model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể lưu thay đổi. Phòng có thể đã bị xóa hoặc cập nhật bởi người khác.");
                        // Load lại CinemaName...
                        var currentCinema = await _context.Cinemas.FindAsync(room.CinemaId);
                        model.CinemaName = currentCinema?.Name;
                        // model.CinemasList = new SelectList(await _context.Cinemas.OrderBy(c=>c.Name).ToListAsync(), "Id", "Name", room.CinemaId);
                        return View(model);
                    }
                }
            }
            // Load lại CinemaName...
            var cinemaForModel = await _context.Cinemas.FindAsync(model.CinemaId);
            model.CinemaName = cinemaForModel?.Name;
            // model.CinemasList = new SelectList(await _context.Cinemas.OrderBy(c=>c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
            return View(model);
        }

        // GET: /Room/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Cinema)
                .Include(r => r.Seats)  // Để kiểm tra ràng buộc
                .Include(r => r.Shows)  // Để kiểm tra ràng buộc
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound($"Không tìm thấy phòng với ID {id}.");
            }
            ViewBag.CanDelete = !room.Seats.Any() && !room.Shows.Any();
            return View(room); // Views/Room/Delete.cshtml
        }

        // POST: /Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.Include(r => r.Seats).Include(r => r.Shows).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                TempData["Error"] = "Không tìm thấy phòng để xóa.";
                // Không rõ cinemaId ở đây, nên chuyển về danh sách rạp chung
                return RedirectToAction("Index", "Cinema");
            }

            int cinemaId = room.CinemaId; // Lưu lại để redirect về đúng trang Index của rạp

            if (room.Seats.Any())
            {
                TempData["Error"] = $"Không thể xóa phòng '{room.Name}' vì vẫn còn ghế liên kết. Vui lòng xóa các ghế trước.";
                ViewBag.CanDelete = false;
                return View("Delete", room); // Quay lại view Delete với model
            }
            if (room.Shows.Any())
            {
                TempData["Error"] = $"Không thể xóa phòng '{room.Name}' vì vẫn còn suất chiếu liên kết. Vui lòng xóa các suất chiếu trước.";
                ViewBag.CanDelete = false;
                return View("Delete", room); // Quay lại view Delete với model
            }

            try
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa phòng '{room.Name}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Lỗi khi xóa phòng: {ex.InnerException?.Message ?? ex.Message}.";
                ViewBag.CanDelete = !room.Seats.Any() && !room.Shows.Any(); // Re-evaluate
                return View("Delete", room);
            }
            // Redirect về trang Index của RoomController với cinemaId của rạp chứa phòng vừa xóa
            return RedirectToAction(nameof(Index), new { cinemaId = cinemaId });
        }
    }
}