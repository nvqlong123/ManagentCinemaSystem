using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagentCinemaSystem.Controllers.Admin
{
    //[Area("Admin")] // Assuming Area is configured elsewhere if needed
    //[Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Movie
        //[Route("")] // Route cho action Index (nếu muốn URL là /Admin/Movie)
        //[Route("Index")] // Route cho action Index (nếu muốn URL là /Admin/Movie/Index)
        public async Task<IActionResult> Index(
            [FromQuery] string searchTerm = "",
            [FromQuery] int? selectedGenreId = null,
            [FromQuery] bool? isActiveFilter = true)
        {
            var query = _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .AsQueryable();

            // Filter logic
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m => m.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (selectedGenreId.HasValue)
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == selectedGenreId));

            if (isActiveFilter.HasValue)
                query = query.Where(m => m.IsActive == isActiveFilter);

            var model = new MovieIndexViewModel
            {
                Movies = await query.OrderByDescending(m => m.ReleaseDate).ThenBy(m => m.Title).ToListAsync(),
                Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync(),
                SelectedGenreId = selectedGenreId,
                SearchTerm = searchTerm,
                IsActiveFilter = isActiveFilter
            };

            return View(model);
        }

        // GET: /Admin/Movie/Create
        public async Task<IActionResult> Create()
        {
            return View(new MovieCreateViewModel
            {
                AvailableGenres = await _context.Genres.OrderBy(g => g.Name).ToListAsync(),
                ReleaseDate = DateTime.Today.AddDays(7) // Default: 1 tuần sau
            });
        }

        // POST: /Admin/Movie/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MovieCreateViewModel model,
            List<int> selectedGenreIds) // Ensure the view names this input correctly
        {
            model.SelectedGenreIds = selectedGenreIds ?? new List<int>(); // Handle null case

            if (!ModelState.IsValid)
            {
                model.AvailableGenres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
                return View(model);
            }

            // Tạo movie mới với URL poster
            var movie = new Movie
            {
                Title = model.Title,
                Director = model.Director,
                Actor = model.Actors, // Corrected from model.Actors
                Duration = model.Duration,
                ReleaseDate = model.ReleaseDate,
                Poster = model.PosterUrl ?? "https://via.placeholder.com/300x450.png?text=No+Poster", // Default placeholder
                IsActive = model.IsActive
            };

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync(); // Save movie to get its ID

            // Gán thể loại
            if (model.SelectedGenreIds.Any())
            {
                foreach (var genreId in model.SelectedGenreIds)
                {
                    _context.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = movie.Id,
                        GenreId = genreId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã thêm phim '{movie.Title}' thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Movie/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            var model = new MovieEditViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Director = movie.Director,
                Actors = movie.Actor,
                Duration = movie.Duration,
                ReleaseDate = movie.ReleaseDate,
                PosterUrl = movie.Poster,
                IsActive = movie.IsActive,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                AvailableGenres = await _context.Genres.OrderBy(g => g.Name).ToListAsync()
            };

            return View(model);
        }

        // POST: /Admin/Movie/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieEditViewModel model, List<int> selectedGenreIds)
        {
            model.SelectedGenreIds = selectedGenreIds ?? new List<int>(); // Handle null case

            if (!ModelState.IsValid)
            {
                model.AvailableGenres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
                return View(model);
            }

            var movie = await _context.Movies.Include(m => m.MovieGenres).FirstOrDefaultAsync(m => m.Id == model.Id);
            if (movie == null) return NotFound();

            // Cập nhật thông tin phim
            movie.Title = model.Title;
            movie.Director = model.Director;
            movie.Actor = model.Actors;
            movie.Duration = model.Duration;
            movie.ReleaseDate = model.ReleaseDate;
            movie.Poster = model.PosterUrl ?? "https://via.placeholder.com/300x450.png?text=No+Poster";
            movie.IsActive = model.IsActive;

            // Cập nhật thể loại
            // Remove existing genres
            _context.MovieGenres.RemoveRange(movie.MovieGenres);

            // Add selected genres
            if (model.SelectedGenreIds.Any())
            {
                foreach (var genreId in model.SelectedGenreIds)
                {
                    _context.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genreId });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật phim '{movie.Title}' thành công";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Movies.Any(e => e.Id == model.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData["Error"] = "Lỗi khi cập nhật phim. Dữ liệu có thể đã được thay đổi bởi người khác.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Movie/Delete/5
        [HttpGet] // Explicitly GET
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            // Check for related shows before allowing delete view
            bool hasShows = await _context.Shows.AnyAsync(s => s.MovieId == id);
            ViewBag.HasShows = hasShows;

            return View(movie); // Hiển thị form xác nhận
        }

        // POST: /Admin/Movie/DeleteConfirmed/5
        [HttpPost] // Ensure route matches and ActionName for form post
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                TempData["Error"] = "Không tìm thấy phim để xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Double-check for related shows
            bool hasShows = await _context.Shows.AnyAsync(s => s.MovieId == id);
            if (hasShows)
            {
                TempData["Error"] = $"Không thể xóa phim '{movie.Title}' vì đang có suất chiếu liên kết. Vui lòng xóa các suất chiếu trước.";
                // Pass movie model back to Delete view to show the error
                ViewBag.HasShows = true;
                return View("Delete", movie);
            }

            try
            {
                // MovieGenres will be cascade deleted if Movie is deleted, based on ApplicationDbContext setup
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa phim '{movie.Title}' thành công";
            }
            catch (DbUpdateException) // Should be caught by the explicit check above
            {
                TempData["Error"] = "Không thể xóa vì phim đang có suất chiếu hoặc ràng buộc dữ liệu khác!";
                ViewBag.HasShows = await _context.Shows.AnyAsync(s => s.MovieId == id); // Re-check for view
                return View("Delete", movie);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Movie/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice for state-changing operations
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            movie.IsActive = !movie.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(movie.IsActive ? "kích hoạt" : "bỏ kích hoạt")} phim '{movie.Title}'";
            // Return a JSON response if this is called via AJAX, otherwise redirect
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new
                {
                    newStatus = movie.IsActive,
                    message = $"Đã {(movie.IsActive ? "kích hoạt" : "bỏ kích hoạt")} phim {movie.Title}"
                });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}