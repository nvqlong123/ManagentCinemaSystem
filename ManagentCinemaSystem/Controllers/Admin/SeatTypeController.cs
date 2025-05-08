// File: Controllers/SeatTypeController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels; // Đảm bảo namespace này đúng cho SeatTypeViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers // Bỏ .Admin nếu bạn muốn
{
    // KHÔNG CÓ [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SeatTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeatTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /SeatType hoặc /SeatType/Index
        public async Task<IActionResult> Index()
        {
            var seatTypes = await _context.SeatTypes
                                          .Include(st => st.Seats) // << THÊM DÒNG NÀY
                                          .OrderBy(st => st.Name)
                                          .ToListAsync();
            return View(seatTypes);
        }

        // GET: /SeatType/Create
        public IActionResult Create()
        {
            return View(new SeatTypeViewModel()); // Views/SeatType/Create.cshtml
        }

        // POST: /SeatType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeatTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Chuẩn hóa tên để so sánh không phân biệt hoa thường
                string normalizedModelName = model.Name.Trim().ToLower();
                if (await _context.SeatTypes.AnyAsync(st => st.Name.ToLower() == normalizedModelName))
                {
                    ModelState.AddModelError("Name", "Tên loại ghế này đã tồn tại.");
                    return View(model);
                }

                var seatType = new SeatType { Name = model.Name.Trim(), Cost = model.Cost };
                _context.SeatTypes.Add(seatType);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm loại ghế '{seatType.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: /SeatType/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var seatType = await _context.SeatTypes.FindAsync(id);
            if (seatType == null)
            {
                return NotFound($"Không tìm thấy loại ghế với ID {id}.");
            }
            var model = new SeatTypeViewModel { Id = seatType.Id, Name = seatType.Name, Cost = seatType.Cost };
            return View(model); // Views/SeatType/Edit.cshtml
        }

        // POST: /SeatType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SeatTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var seatType = await _context.SeatTypes.FindAsync(model.Id);
                if (seatType == null)
                {
                    return NotFound($"Không tìm thấy loại ghế với ID {model.Id} để cập nhật.");
                }

                string normalizedModelName = model.Name.Trim().ToLower();
                if (await _context.SeatTypes.AnyAsync(st => st.Name.ToLower() == normalizedModelName && st.Id != model.Id))
                {
                    ModelState.AddModelError("Name", "Tên loại ghế này đã tồn tại.");
                    return View(model);
                }

                seatType.Name = model.Name.Trim();
                seatType.Cost = model.Cost;

                try
                {
                    _context.Update(seatType);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật loại ghế '{seatType.Name}' thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.SeatTypes.Any(e => e.Id == model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể lưu thay đổi. Loại ghế có thể đã bị xóa hoặc cập nhật bởi người khác.");
                        return View(model);
                    }
                }
            }
            return View(model);
        }

        // GET: /SeatType/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var seatType = await _context.SeatTypes.FindAsync(id);
            if (seatType == null)
            {
                return NotFound($"Không tìm thấy loại ghế với ID {id}.");
            }
            // Check if the seat type is in use before showing delete confirmation
            ViewBag.IsInUse = await _context.Seats.AnyAsync(s => s.SeatTypeId == id);
            return View(seatType); // Views/SeatType/Delete.cshtml
        }

        // POST: /SeatType/Delete/5 (ActionName("Delete") giúp form post đến đây)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seatType = await _context.SeatTypes.FindAsync(id);
            if (seatType == null)
            {
                TempData["Error"] = "Không tìm thấy loại ghế để xóa.";
                return RedirectToAction(nameof(Index));
            }

            var isSeatTypeUsed = await _context.Seats.AnyAsync(s => s.SeatTypeId == id);
            if (isSeatTypeUsed)
            {
                TempData["Error"] = $"Không thể xóa loại ghế '{seatType.Name}' vì đang được sử dụng bởi một hoặc nhiều ghế. Vui lòng thay đổi loại ghế của các ghế đó trước khi xóa.";
                ViewBag.IsInUse = true;
                return View("Delete", seatType);
            }

            try
            {
                _context.SeatTypes.Remove(seatType);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa loại ghế '{seatType.Name}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Lỗi khi xóa loại ghế: {ex.InnerException?.Message ?? ex.Message}.";
                ViewBag.IsInUse = true; // Re-set for the view
                return View("Delete", seatType);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}