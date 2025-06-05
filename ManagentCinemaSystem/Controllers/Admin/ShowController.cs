// File: Controllers/ShowController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels; // Đảm bảo namespace này đúng
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers // Bỏ .Admin nếu bạn muốn
{
    // KHÔNG CÓ [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ShowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Show hoặc /Show/Index
        public async Task<IActionResult> Index(string movieFilter, int? cinemaFilter, DateTime? dateFilter)
        {
            var query = _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
                .AsQueryable();

            if (!string.IsNullOrEmpty(movieFilter))
            {
                query = query.Where(s => s.Movie.Title.Contains(movieFilter, StringComparison.OrdinalIgnoreCase));
            }
            if (cinemaFilter.HasValue)
            {
                query = query.Where(s => s.Room.CinemaId == cinemaFilter.Value);
            }
            if (dateFilter.HasValue)
            {
                query = query.Where(s => s.StartTime.Date == dateFilter.Value.Date);
            }

            var shows = await query.OrderByDescending(s => s.StartTime).ThenBy(s => s.Movie.Title).ToListAsync();

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
                // MovieList không còn cần thiết vì có thể tìm theo tên
                CinemaList = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", cinemaFilter)
            };

            return View(viewModel); // Sẽ tìm view trong Views/Show/Index.cshtml
        }

        // GET: /Show/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(s => s.ShowSeats).ThenInclude(ss => ss.Seat).ThenInclude(seat => seat.SeatType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (show == null)
            {
                return NotFound($"Không tìm thấy suất chiếu với ID {id}.");
            }

            var seatTypesList = await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync();
            var seatTypeNamesDict = seatTypesList.ToDictionary(st => st.Id, st => st.Name);

            var viewModel = new ShowDetailsViewModel
            {
                Show = show,
                SeatTypeNames = seatTypeNamesDict
            };

            return View(viewModel); // Trả về ViewModel mới
        }


        // GET: /Show/Create
        public async Task<IActionResult> Create()
        {
            var model = new ShowCreateViewModel
            {
                AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title"),
                AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                AvailableRooms = new SelectList(new List<Room>(), "Id", "Name"), // Trống ban đầu
                ShowDate = DateTime.Today // Mặc định ngày chiếu là hôm nay
            };
            return View(model); // Views/Show/Create.cshtml
        }

        // POST: /Show/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowCreateViewModel model)
        {
            // --- Hàm helper để chuẩn bị lại dropdowns ---
            async Task RepopulateCreateDropdownsAsync(ShowCreateViewModel m)
            {
                m.AvailableMovies = new SelectList(await _context.Movies.Where(mv => mv.IsActive).OrderBy(mv => mv.Title).ToListAsync(), "Id", "Title", m.MovieId);
                m.AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", m.CinemaId);
                if (m.CinemaId > 0)
                {
                    m.AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == m.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", m.RoomId);
                }
                else
                {
                    m.AvailableRooms = new SelectList(new List<Room>(), "Id", "Name");
                }
            }
            // --- Kết thúc hàm helper ---

            // Bước 1: Kiểm tra ModelState ban đầu (từ các Data Annotations trong ViewModel)
            if (!ModelState.IsValid)
            {
                await RepopulateCreateDropdownsAsync(model);
                return View(model); // Trả về View với lỗi validation từ ViewModel
            }

            // Bước 2: Lấy và kiểm tra các đối tượng liên quan (Movie, Room)
            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null)
            {
                ModelState.AddModelError("MovieId", "Phim đã chọn không hợp lệ hoặc không tồn tại.");
            }

            var room = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == model.RoomId); // Include Cinema để lấy tên
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Phòng chiếu đã chọn không hợp lệ hoặc không tồn tại.");
            }

            // Bước 3: Thực hiện các kiểm tra logic nghiệp vụ và thêm lỗi nếu cần
            if (model.ShowDate < DateTime.Today)
            {
                ModelState.AddModelError("ShowDate", "Ngày chiếu không được là ngày trong quá khứ.");
            }

            DateTime combinedStartTime = model.ShowDate.Date.Add(model.StartTimeOnly.TimeOfDay);
            if (combinedStartTime < DateTime.Now.AddMinutes(-5)) // Cho phép tạo sát giờ hiện tại (sai số 5 phút)
            {
                ModelState.AddModelError("StartTimeOnly", "Thời gian bắt đầu không hợp lệ (đã qua hoặc quá sát giờ hiện tại).");
            }

            // Bước 4: Kiểm tra lại ModelState SAU khi đã thêm các lỗi nghiệp vụ
            if (!ModelState.IsValid || movie == null || room == null) // Đảm bảo movie và room không null trước khi dùng
            {
                await RepopulateCreateDropdownsAsync(model);
                return View(model); // Trả về View với tất cả các lỗi
            }

            // Bước 5: Nếu tất cả đều hợp lệ, tiến hành kiểm tra logic phức tạp hơn (isOverlapping) và tạo suất chiếu
            DateTime endTime = combinedStartTime.AddMinutes(movie.Duration);
            bool isOverlapping = await _context.Shows
                .AnyAsync(s => s.RoomId == model.RoomId &&
                            ((combinedStartTime >= s.StartTime && combinedStartTime < s.EndTime) ||
                             (endTime > s.StartTime && endTime <= s.EndTime) ||
                             (combinedStartTime <= s.StartTime && endTime >= s.EndTime)));

            if (isOverlapping)
            {
                ModelState.AddModelError(string.Empty, $"Đã có suất chiếu khác trong phòng '{room.Name}' (Rạp: {room.Cinema?.Name}) bị trùng hoặc chồng chéo với khung thời gian này.");
                await RepopulateCreateDropdownsAsync(model);
                return View(model);
            }

            // Bước 6: Tạo và lưu suất chiếu
            var show = new Show
            {
                MovieId = model.MovieId, // Đã kiểm tra movie != null
                RoomId = model.RoomId,   // Đã kiểm tra room != null
                StartTime = combinedStartTime,
                EndTime = endTime
            };

            try
            {
                _context.Shows.Add(show);
                await _context.SaveChangesAsync(); // Lưu Show để có ID

                var seatsInRoom = await _context.Seats.Where(s => s.RoomId == model.RoomId).ToListAsync();
                if (seatsInRoom.Any())
                {
                    var showSeats = seatsInRoom.Select(seat => new ShowSeat
                    {
                        ShowId = show.Id,
                        SeatId = seat.Id,
                        IsBooked = false
                    }).ToList();
                    _context.ShowSeats.AddRange(showSeats);
                    await _context.SaveChangesAsync(); // Lưu ShowSeats
                }

                TempData["Success"] = $"Đã tạo suất chiếu cho phim '{movie.Title}' vào lúc {combinedStartTime:dd/MM/yyyy HH:mm} tại '{room.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // Log lỗi dbEx.ToString() hoặc dbEx.InnerException
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu dữ liệu vào cơ sở dữ liệu. Vui lòng thử lại. " + dbEx.Message);
                await RepopulateCreateDropdownsAsync(model);
                return View(model);
            }
            catch (Exception ex) // Bắt các lỗi không mong muốn khác
            {
                // Log lỗi ex.ToString()
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi không mong muốn trong quá trình tạo suất chiếu.");
                await RepopulateCreateDropdownsAsync(model);
                return View(model);
            }
        }

        // GET: /Show/GetRoomsForCinema
        [HttpGet("Show/GetRoomsForCinema")] // Route cho AJAX
        public async Task<JsonResult> GetRoomsForCinema(int cinemaId)
        {
            var rooms = await _context.Rooms
                                    .Where(r => r.CinemaId == cinemaId)
                                    .OrderBy(r => r.Name)
                                    .Select(r => new { id = r.Id, name = r.Name })
                                    .ToListAsync();
            return Json(rooms);
        }

        // GET: /Show/GetMovieDuration
        [HttpGet("Show/GetMovieDuration")] // Route cho AJAX
        public async Task<JsonResult> GetMovieDuration(int movieId)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return Json(new { duration = 0 });
            return Json(new { duration = movie.Duration });
        }


        // GET: /Show/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == id);
            if (show == null) return NotFound($"Không tìm thấy suất chiếu với ID {id}.");

            // Kiểm tra nếu đã có vé đặt thì không cho sửa
            if (await _context.ShowSeats.AnyAsync(ss => ss.ShowId == id && ss.IsBooked))
            {
                TempData["Error"] = "Không thể chỉnh sửa suất chiếu này vì đã có vé được đặt. Vui lòng xóa suất chiếu và tạo mới nếu cần thay đổi.";
                return RedirectToAction(nameof(Details), new { id });
            }


            var movie = await _context.Movies.FindAsync(show.MovieId);
            // Nếu phim bị xóa hoặc không active thì cũng không nên cho sửa suất chiếu của nó nữa (tùy logic)
            if (movie == null || !movie.IsActive)
            {
                TempData["Error"] = "Phim của suất chiếu này không còn tồn tại hoặc không hoạt động.";
                return RedirectToAction(nameof(Index));
            }


            var model = new ShowEditViewModel
            {
                Id = show.Id,
                MovieId = show.MovieId,
                CinemaId = show.Room.CinemaId,
                RoomId = show.RoomId,
                ShowDate = show.StartTime.Date,
                StartTimeOnly = show.StartTime,
                OriginalMovieDuration = movie.Duration, // Để hiển thị thời lượng gốc
                AvailableMovies = new SelectList(await _context.Movies.Where(m => m.IsActive).OrderBy(m => m.Title).ToListAsync(), "Id", "Title", show.MovieId),
                AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", show.Room.CinemaId),
                AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == show.Room.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", show.RoomId)
            };
            return View(model); // Views/Show/Edit.cshtml
        }

        // POST: /Show/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShowEditViewModel model)
        {
            // --- Gán lại các SelectList khi ModelState không hợp lệ ---
            async Task RepopulateEditDropdownsAsync(ShowEditViewModel m)
            {
                m.AvailableMovies = new SelectList(await _context.Movies.Where(mv => mv.IsActive).OrderBy(mv => mv.Title).ToListAsync(), "Id", "Title", m.MovieId);
                m.AvailableCinemas = new SelectList(await _context.Cinemas.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", m.CinemaId);
                if (m.CinemaId > 0)
                {
                    m.AvailableRooms = new SelectList(await _context.Rooms.Where(r => r.CinemaId == m.CinemaId).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", m.RoomId);
                }
                else
                {
                    m.AvailableRooms = new SelectList(new List<Room>(), "Id", "Name");
                }
                var currentMovieForDuration = await _context.Movies.FindAsync(m.MovieId);
                if (currentMovieForDuration != null) m.OriginalMovieDuration = currentMovieForDuration.Duration;
            }
            // --- Kết thúc hàm helper ---

            var showToUpdate = await _context.Shows.Include(s => s.ShowSeats).FirstOrDefaultAsync(s => s.Id == model.Id);
            if (showToUpdate == null) return NotFound($"Không tìm thấy suất chiếu với ID {model.Id} để cập nhật.");

            if (showToUpdate.ShowSeats.Any(ss => ss.IsBooked))
            {
                TempData["Error"] = "Không thể chỉnh sửa suất chiếu này vì đã có vé được đặt.";
                await RepopulateEditDropdownsAsync(model);
                return View(model);
            }

            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null) ModelState.AddModelError("MovieId", "Phim đã chọn không hợp lệ.");

            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null) ModelState.AddModelError("RoomId", "Phòng chiếu đã chọn không hợp lệ.");

            if (model.ShowDate < DateTime.Today)
            {
                ModelState.AddModelError("ShowDate", "Ngày chiếu không được là ngày trong quá khứ.");
            }

            DateTime combinedStartTime = model.ShowDate.Date.Add(model.StartTimeOnly.TimeOfDay);

            if (movie != null && room != null && ModelState.IsValid)
            {
                if (combinedStartTime < DateTime.Now.AddMinutes(-10) && showToUpdate.StartTime > DateTime.Now) // Chỉ kiểm tra nếu suất chiếu gốc chưa diễn ra
                {
                    ModelState.AddModelError("StartTimeOnly", "Thời gian bắt đầu không hợp lệ (đã qua hoặc quá sát giờ hiện tại).");
                }
                else
                {
                    DateTime endTime = combinedStartTime.AddMinutes(movie.Duration);

                    bool isOverlapping = await _context.Shows
                        .AnyAsync(s => s.RoomId == model.RoomId &&
                                    s.Id != model.Id && // Loại trừ chính suất chiếu đang sửa
                                    ((combinedStartTime >= s.StartTime && combinedStartTime < s.EndTime) ||
                                        (endTime > s.StartTime && endTime <= s.EndTime) ||
                                        (combinedStartTime <= s.StartTime && endTime >= s.EndTime)));
                    if (isOverlapping)
                    {
                        ModelState.AddModelError(string.Empty, $"Đã có suất chiếu khác trong phòng '{room.Name}' (Rạp: {room.Cinema?.Name}) bị trùng hoặc chồng chéo với khung thời gian này.");
                    }
                    else
                    {
                        bool movieOrRoomChanged = showToUpdate.MovieId != model.MovieId || showToUpdate.RoomId != model.RoomId;

                        showToUpdate.MovieId = model.MovieId;
                        showToUpdate.RoomId = model.RoomId;
                        showToUpdate.StartTime = combinedStartTime;
                        showToUpdate.EndTime = endTime;

                        // Nếu phim hoặc phòng thay đổi, cần tạo lại ShowSeats
                        // Vì đã kiểm tra không có vé đặt, nên có thể xóa ShowSeats cũ
                        if (movieOrRoomChanged)
                        {
                            _context.ShowSeats.RemoveRange(showToUpdate.ShowSeats); // Xóa ShowSeats cũ
                            await _context.SaveChangesAsync(); // Lưu thay đổi xóa ShowSeats

                            var seatsInNewRoom = await _context.Seats.Where(s => s.RoomId == model.RoomId).ToListAsync();
                            if (seatsInNewRoom.Any())
                            {
                                var newShowSeats = seatsInNewRoom.Select(seat => new ShowSeat
                                {
                                    ShowId = showToUpdate.Id,
                                    SeatId = seat.Id,
                                    IsBooked = false
                                }).ToList();
                                _context.ShowSeats.AddRange(newShowSeats); // Thêm ShowSeats mới
                            }
                        }

                        _context.Update(showToUpdate);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Cập nhật suất chiếu thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            await RepopulateEditDropdownsAsync(model);
            return View(model);
        }


        // GET: /Show/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(s => s.ShowSeats) // Để kiểm tra vé đã đặt
                .FirstOrDefaultAsync(m => m.Id == id);

            if (show == null) return NotFound($"Không tìm thấy suất chiếu với ID {id}.");

            ViewBag.CanDelete = !show.ShowSeats.Any(ss => ss.IsBooked);
            ViewBag.BookedSeatsCount = show.ShowSeats.Count(ss => ss.IsBooked);

            return View(show); // Views/Show/Delete.cshtml
        }

        // POST: /Show/Delete/5
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
                // Cần load lại model đầy đủ cho View "Delete" nếu muốn hiển thị lại thông tin chi tiết
                var showForView = await _context.Shows
                   .Include(s => s.Movie)
                   .Include(s => s.Room).ThenInclude(r => r.Cinema)
                   .FirstOrDefaultAsync(m => m.Id == id);
                ViewBag.CanDelete = false;
                ViewBag.BookedSeatsCount = show.ShowSeats.Count(ss => ss.IsBooked);
                return View("Delete", showForView);
            }

            try
            {
                // ShowSeats sẽ tự động bị xóa do cấu hình Cascade Delete trong DbContext
                // Hoặc bạn có thể xóa tường minh: _context.ShowSeats.RemoveRange(show.ShowSeats);
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
                ViewBag.CanDelete = !show.ShowSeats.Any(ss => ss.IsBooked);
                ViewBag.BookedSeatsCount = show.ShowSeats.Count(ss => ss.IsBooked);
                return View("Delete", showForView);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}