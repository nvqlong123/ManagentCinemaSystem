using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ShowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Show
        public async Task<IActionResult> Index(string movieFilter, int? cinemaFilter, DateTime? dateFilter)
        {
            var query = _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
                .AsQueryable();

            if (!string.IsNullOrEmpty(movieFilter))
            {
                query = query.Where(s => s.Movie.Title.Contains(movieFilter));
            }
            if (cinemaFilter.HasValue)
            {
                query = query.Where(s => s.Room.CinemaId == cinemaFilter.Value);
            }
            if (dateFilter.HasValue)
            {
                query = query.Where(s => s.StartTime.Date == dateFilter.Value.Date);
            }

            var shows = await query.OrderByDescending(s => s.StartTime).ToListAsync();

            var viewModel = new ShowIndexViewModel
            {
                Shows = shows.Select(s => new ShowViewModel
                {
                    Id = s.Id,
                    MovieTitle = s.Movie.Title,
                    MovieDuration = s.Movie.Duration,
                    RoomName = s.Room.Name,
                    CinemaName = s.Room.Cinema.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList(),
                MovieFilter = movieFilter,
                CinemaFilter = cinemaFilter,
                DateFilter = dateFilter,
                MovieList = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title"),
                CinemaList = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name")
            };

            return View(viewModel);
        }

        // GET: /Admin/Show/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room)
                    .ThenInclude(r => r.Cinema)
                .Include(s => s.ShowSeats)
                    .ThenInclude(ss => ss.Seat)
                    .ThenInclude(seat => seat.SeatType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (show == null)
            {
                return NotFound();
            }

            return View(show);
        }


        // GET: /Admin/Show/Create
        public async Task<IActionResult> Create()
        {
            var model = new ShowCreateViewModel
            {
                AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title"),
                AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                AvailableRooms = new SelectList(new List<Room>(), "Id", "Name") // Empty initially
            };
            return View(model);
        }

        // POST: /Admin/Show/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowCreateViewModel model)
        {
            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null) ModelState.AddModelError("MovieId", "Phim không hợp lệ.");

            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null) ModelState.AddModelError("RoomId", "Phòng chiếu không hợp lệ.");

            DateTime combinedStartTime = model.ShowDate.Date.Add(model.StartTimeOnly.TimeOfDay);

            if (movie != null && room != null && ModelState.IsValid)
            {
                DateTime endTime = combinedStartTime.AddMinutes(movie.Duration);

                // Check for overlapping shows in the same room
                bool isOverlapping = await _context.Shows
                    .AnyAsync(s => s.RoomId == model.RoomId &&
                                   s.Id != 0 && // Exclude self if editing, though not applicable here
                                   ((combinedStartTime >= s.StartTime && combinedStartTime < s.EndTime) ||
                                    (endTime > s.StartTime && endTime <= s.EndTime) ||
                                    (combinedStartTime <= s.StartTime && endTime >= s.EndTime)));

                if (isOverlapping)
                {
                    ModelState.AddModelError("", $"Đã có suất chiếu khác trong phòng này ({room.Name}) vào thời gian này.");
                }
                else
                {
                    var show = new Show
                    {
                        MovieId = model.MovieId,
                        RoomId = model.RoomId,
                        StartTime = combinedStartTime,
                        EndTime = endTime
                    };
                    _context.Shows.Add(show);
                    await _context.SaveChangesAsync(); // Save show to get its ID

                    // Create ShowSeat entries for this show
                    var seatsInRoom = await _context.Seats.Where(s => s.RoomId == model.RoomId).ToListAsync();
                    foreach (var seat in seatsInRoom)
                    {
                        _context.ShowSeats.Add(new ShowSeat { ShowId = show.Id, SeatId = seat.Id, IsBooked = false });
                    }
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã tạo suất chiếu cho phim '{movie.Title}' thành công.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Repopulate dropdowns if model state is invalid
            model.AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title", model.MovieId);
            model.AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
            if (model.CinemaId > 0)
            {
                model.AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == model.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", model.RoomId);
            }
            else
            {
                model.AvailableRooms = new SelectList(new List<Room>(), "Id", "Name");
            }
            return View(model);
        }

        // GET: /Admin/Show/GetRoomsForCinema
        [HttpGet]
        public async Task<JsonResult> GetRoomsForCinema(int cinemaId)
        {
            var rooms = await _context.Rooms
                                    .Where(r => r.CinemaId == cinemaId)
                                    .OrderBy(r => r.Name)
                                    .Select(r => new { id = r.Id, name = r.Name })
                                    .ToListAsync();
            return Json(rooms);
        }

        // GET: /Admin/Show/GetMovieDuration
        [HttpGet]
        public async Task<JsonResult> GetMovieDuration(int movieId)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return Json(new { duration = 0 });
            return Json(new { duration = movie.Duration });
        }


        // GET: /Admin/Show/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == id);
            if (show == null) return NotFound();

            var movie = await _context.Movies.FindAsync(show.MovieId);
            if (movie == null) return NotFound("Phim của suất chiếu này không còn tồn tại.");


            var model = new ShowEditViewModel
            {
                Id = show.Id,
                MovieId = show.MovieId,
                CinemaId = show.Room.CinemaId,
                RoomId = show.RoomId,
                ShowDate = show.StartTime.Date,
                StartTimeOnly = show.StartTime, // This will take the time part
                OriginalMovieDuration = movie.Duration,
                AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title", show.MovieId),
                AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", show.Room.CinemaId),
                AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == show.Room.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", show.RoomId)
            };
            return View(model);
        }

        // POST: /Admin/Show/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShowEditViewModel model)
        {
            var showToUpdate = await _context.Shows.Include(s => s.ShowSeats).FirstOrDefaultAsync(s => s.Id == model.Id);
            if (showToUpdate == null) return NotFound();

            // Check if any seats are booked for this show. If so, prevent editing.
            if (showToUpdate.ShowSeats.Any(ss => ss.IsBooked))
            {
                TempData["Error"] = "Không thể chỉnh sửa suất chiếu này vì đã có vé được đặt.";
                // Repopulate dropdowns before returning view
                model.AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title", model.MovieId);
                model.AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
                model.AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == model.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", model.RoomId);
                var m = await _context.Movies.FindAsync(model.MovieId);
                if (m != null) model.OriginalMovieDuration = m.Duration;
                return View(model);
            }

            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null) ModelState.AddModelError("MovieId", "Phim không hợp lệ.");

            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null) ModelState.AddModelError("RoomId", "Phòng chiếu không hợp lệ.");

            DateTime combinedStartTime = model.ShowDate.Date.Add(model.StartTimeOnly.TimeOfDay);

            if (movie != null && room != null && ModelState.IsValid)
            {
                DateTime endTime = combinedStartTime.AddMinutes(movie.Duration);

                bool isOverlapping = await _context.Shows
                    .AnyAsync(s => s.RoomId == model.RoomId &&
                                   s.Id != model.Id && // Exclude self
                                   ((combinedStartTime >= s.StartTime && combinedStartTime < s.EndTime) ||
                                    (endTime > s.StartTime && endTime <= s.EndTime) ||
                                    (combinedStartTime <= s.StartTime && endTime >= s.EndTime)));
                if (isOverlapping)
                {
                    ModelState.AddModelError("", $"Đã có suất chiếu khác trong phòng này ({room.Name}) vào thời gian này.");
                }
                else
                {
                    showToUpdate.MovieId = model.MovieId;
                    showToUpdate.RoomId = model.RoomId; // If room changes, ShowSeats need to be recreated
                    showToUpdate.StartTime = combinedStartTime;
                    showToUpdate.EndTime = endTime;

                    // If RoomId changed, existing ShowSeats for the old room are invalid.
                    // Since we prevent editing if booked, we can safely remove old and add new.
                    // The check for booked seats above handles this.
                    if (showToUpdate.RoomId != model.RoomId || showToUpdate.MovieId != model.MovieId) // Check if movie or room changed
                    {
                        _context.ShowSeats.RemoveRange(showToUpdate.ShowSeats); // Remove old show seats
                        await _context.SaveChangesAsync(); // Commit removal

                        var seatsInNewRoom = await _context.Seats.Where(s => s.RoomId == model.RoomId).ToListAsync();
                        foreach (var seat in seatsInNewRoom)
                        {
                            _context.ShowSeats.Add(new ShowSeat { ShowId = showToUpdate.Id, SeatId = seat.Id, IsBooked = false });
                        }
                    }

                    _context.Update(showToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật suất chiếu thành công.";
                    return RedirectToAction(nameof(Index));
                }
            }

            model.AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title", model.MovieId);
            model.AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CinemaId);
            model.AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == model.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", model.RoomId);
            var currentMovie = await _context.Movies.FindAsync(model.MovieId);
            if (currentMovie != null) model.OriginalMovieDuration = currentMovie.Duration;

            return View(model);
        }


        // GET: /Admin/Show/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(s => s.ShowSeats)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (show == null) return NotFound();

            ViewBag.CanDelete = !show.ShowSeats.Any(ss => ss.IsBooked); // Check if any seat is booked

            return View(show);
        }

        // POST: /Admin/Show/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var show = await _context.Shows.Include(s => s.ShowSeats).FirstOrDefaultAsync(s => s.Id == id);
            if (show == null)
            {
                TempData["Error"] = "Không tìm thấy suất chiếu để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (show.ShowSeats.Any(ss => ss.IsBooked))
            {
                TempData["Error"] = "Không thể xóa suất chiếu này vì đã có vé được đặt.";
                var movieForView = await _context.Movies.FindAsync(show.MovieId);
                var roomForView = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == show.RoomId);
                // Re-create a temporary object for the view model if needed or just pass show
                var showForView = await _context.Shows
                   .Include(s => s.Movie)
                   .Include(s => s.Room).ThenInclude(r => r.Cinema)
                   .FirstOrDefaultAsync(m => m.Id == id);
                ViewBag.CanDelete = false;
                return View("Delete", showForView);
            }

            try
            {
                // ShowSeats will be cascade deleted due to DbContext configuration
                _context.Shows.Remove(show);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa suất chiếu thành công.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Lỗi khi xóa suất chiếu: {ex.InnerException?.Message ?? ex.Message}.";
                var showForView = await _context.Shows
                    .Include(s => s.Movie)
                    .Include(s => s.Room).ThenInclude(r => r.Cinema)
                    .FirstOrDefaultAsync(m => m.Id == id);
                ViewBag.CanDelete = !show.ShowSeats.Any(ss => ss.IsBooked); // Re-evaluate
                return View("Delete", showForView);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}