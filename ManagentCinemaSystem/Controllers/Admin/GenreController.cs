using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class GenreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GenreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Genre
        public async Task<IActionResult> Index()
        {
            var genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(genres);
        }

        // GET: /Admin/Genre/Create
        public IActionResult Create()
        {
            return View(new GenreViewModel());
        }

        // POST: /Admin/Genre/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GenreViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate genre name
                if (await _context.Genres.AnyAsync(g => g.Name == model.Name))
                {
                    ModelState.AddModelError("Name", "Tên thể loại này đã tồn tại.");
                    return View(model);
                }

                var genre = new Genre { Name = model.Name };
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm thể loại '{genre.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: /Admin/Genre/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound();
            }
            var model = new GenreViewModel { Id = genre.Id, Name = genre.Name };
            return View(model);
        }

        // POST: /Admin/Genre/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GenreViewModel model)
        {
            if (ModelState.IsValid)
            {
                var genre = await _context.Genres.FindAsync(model.Id);
                if (genre == null)
                {
                    return NotFound();
                }

                // Check for duplicate genre name (excluding the current genre)
                if (await _context.Genres.AnyAsync(g => g.Name == model.Name && g.Id != model.Id))
                {
                    ModelState.AddModelError("Name", "Tên thể loại này đã tồn tại.");
                    return View(model);
                }

                genre.Name = model.Name;
                try
                {
                    _context.Update(genre);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật thể loại '{genre.Name}' thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GenreExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        // GET: /Admin/Genre/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }

        // POST: /Admin/Genre/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                TempData["Error"] = "Không tìm thấy thể loại để xóa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Check if genre is used by any movies
                var isGenreUsed = await _context.MovieGenres.AnyAsync(mg => mg.GenreId == id);
                if (isGenreUsed)
                {
                    TempData["Error"] = $"Không thể xóa thể loại '{genre.Name}' vì đang được sử dụng bởi một hoặc nhiều phim. Vui lòng gỡ thể loại này khỏi các phim trước khi xóa.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Genres.Remove(genre);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa thể loại '{genre.Name}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                // This catch might be redundant if the check above is thorough,
                // but good for unexpected database-level constraints.
                TempData["Error"] = $"Lỗi khi xóa thể loại: {ex.Message}. Có thể thể loại này đang được sử dụng.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool GenreExists(int id)
        {
            return _context.Genres.Any(e => e.Id == id);
        }
    }
}
