using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System; // For DateTimeOffset
using Microsoft.Extensions.Logging;
using ManagentCinemaSystem.ViewModels.Staff;
using Microsoft.AspNetCore.Mvc.Rendering; // For ILogger


// Đổi namespace về gốc
namespace ManagentCinemaSystem.Controllers
{
    // Bỏ [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class StaffBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<StaffBookingController> _logger;

        public StaffBookingController(ApplicationDbContext context, UserManager<User> userManager, ILogger<StaffBookingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /StaffBooking/PendingList
        public async Task<IActionResult> PendingList(string searchTerm, int pageNumber = 1)
        {
            int pageSize = 10;
            _logger.LogInformation("Fetching pending bookings. SearchTerm: {SearchTerm}, PageNumber: {PageNumber}", searchTerm, pageNumber);


            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie)
                .Where(b => b.Status == "PendingPayment")
                .OrderByDescending(b => b.Purchased)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                ViewData["CurrentFilter"] = searchTerm; // Lưu lại search term cho phân trang
                query = query.Where(b => b.BookingCode.Contains(searchTerm) ||
                                         (b.Customer != null && b.Customer.Name.Contains(searchTerm)) ||
                                         (b.Customer != null && b.Customer.Email.Contains(searchTerm)));
            }

            var paginatedBookings = await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);
            _logger.LogInformation("Found {Count} pending bookings after filtering and pagination.", paginatedBookings.Count);

            // Đường dẫn View bây giờ là Views/StaffBooking/PendingList.cshtml
            return View(paginatedBookings);
        }

        // GET: /StaffBooking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details action called with null ID.");
                return NotFound();
            }
            _logger.LogInformation("Fetching details for BookingId: {BookingId}", id);

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Seat).ThenInclude(s => s.SeatType)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(b => b.StaffConfirmed)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found.", id);
                return NotFound();
            }

            return View(booking); // Views/StaffBooking/Details.cshtml
        }


        // POST: /StaffBooking/ConfirmPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int bookingId)
        {
            var staffUser = await _userManager.GetUserAsync(User);
            if (staffUser == null)
            {
                _logger.LogWarning("ConfirmPayment: Staff user not authenticated.");
                return Challenge();
            }
            _logger.LogInformation("Staff {StaffUserName} attempting to confirm payment for BookingId: {BookingId}", staffUser.UserName, bookingId);


            var booking = await _context.Bookings
                                  .Include(b => b.ShowSeats)
                                  .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đặt vé.";
                _logger.LogWarning("ConfirmPayment: BookingId {BookingId} not found.", bookingId);
                return RedirectToAction(nameof(PendingList));
            }

            if (booking.Status != "PendingPayment")
            {
                TempData["Warning"] = $"Đặt vé {booking.BookingCode} không ở trạng thái chờ thanh toán (Trạng thái hiện tại: {booking.Status}).";
                _logger.LogWarning("ConfirmPayment: BookingId {BookingId} is not in PendingPayment status. Current status: {Status}", bookingId, booking.Status);
                return RedirectToAction(nameof(PendingList));
            }

            if (booking.PaymentDeadline.HasValue)
            {
                var deadlineUtc = DateTime.SpecifyKind(booking.PaymentDeadline.Value, DateTimeKind.Utc);
                var deadlineOffset = new DateTimeOffset(deadlineUtc);

                if (deadlineOffset < DateTimeOffset.UtcNow)
                {
                    TempData["Error"] = $"Đặt vé {booking.BookingCode} đã quá hạn thanh toán.";
                    _logger.LogWarning("ConfirmPayment: BookingId {BookingId} payment deadline has passed. Deadline: {Deadline}", bookingId, deadlineOffset);
                    return RedirectToAction(nameof(PendingList));
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.Status = "Confirmed";
                booking.StaffIdConfirmed = staffUser.Id;
                booking.StaffConfirmed = staffUser as Staff;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Staff {StaffUserName} confirmed payment for Booking {BookingCode} successfully.", staffUser.UserName, booking.BookingCode);
                TempData["Success"] = $"Đã xác nhận thanh toán thành công cho đặt vé #{booking.BookingCode}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error confirming payment for Booking {BookingCode} by Staff {StaffUserName}.", booking.BookingCode, staffUser.UserName);
                TempData["Error"] = "Lỗi khi xác nhận thanh toán: " + ex.Message;
            }

            return RedirectToAction(nameof(PendingList));
        }

        // POST: /StaffBooking/CancelBookingByStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBookingByStaff(int bookingId, string reason = "StaffCancelled")
        {
            var staffUser = await _userManager.GetUserAsync(User);
            if (staffUser == null)
            {
                _logger.LogWarning("CancelBookingByStaff: Staff user not authenticated.");
                return Challenge();
            }
            _logger.LogInformation("Staff {StaffUserName} attempting to cancel BookingId: {BookingId} with reason: {Reason}", staffUser.UserName, bookingId, reason);


            var booking = await _context.Bookings
                                .Include(b => b.ShowSeats)
                                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đặt vé.";
                _logger.LogWarning("CancelBookingByStaff: BookingId {BookingId} not found.", bookingId);
                return RedirectToAction(nameof(PendingList));
            }

            if (booking.Status != "PendingPayment" && booking.Status != "Expired")
            {
                TempData["Warning"] = $"Không thể hủy đặt vé #{booking.BookingCode} vì đang ở trạng thái {booking.Status}.";
                _logger.LogWarning("CancelBookingByStaff: Cannot cancel BookingId {BookingId}. Current status: {Status}", bookingId, booking.Status);
                return RedirectToAction(nameof(PendingList));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string oldStatus = booking.Status;
                booking.Status = reason;
                booking.StaffIdConfirmed = staffUser.Id;
                booking.StaffConfirmed = staffUser as Staff;
                var showSeatsToRelease = booking.ShowSeats.ToList();
                if (showSeatsToRelease.Any(ss => ss.IsBooked && ss.BookingId == booking.Id))
                {
                    _logger.LogInformation("Releasing {Count} seats for cancelled BookingId {BookingId}", showSeatsToRelease.Count(ss => ss.BookingId == booking.Id), bookingId);
                    foreach (var ss in showSeatsToRelease)
                    {
                        if (ss.BookingId == booking.Id)
                        {
                            ss.IsBooked = false;
                            ss.BookingId = null;
                            _context.ShowSeats.Update(ss);
                        }
                    }
                }

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Staff {StaffUserName} cancelled Booking {BookingCode} (Reason: {Reason}). Old status: {OldStatus}. Seats released.", staffUser.UserName, booking.BookingCode, reason, oldStatus);
                TempData["Success"] = $"Đã hủy đặt vé #{booking.BookingCode} thành công và hoàn lại ghế (nếu có).";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling Booking {BookingCode} by Staff {StaffUserName}.", booking.BookingCode, staffUser.UserName);
                TempData["Error"] = "Lỗi khi hủy đặt vé: " + ex.Message;
            }
            return RedirectToAction(nameof(PendingList));
        }
        public async Task<IActionResult> AllBookings(
    string searchTerm,
    string statusFilter,
    DateTime? dateFromFilter,
    DateTime? dateToFilter,
    int? cinemaFilter,
    // int? movieFilter, // Lọc theo Movie phức tạp hơn, tạm thời bỏ qua hoặc xử lý sau
    int pageNumber = 1)
        {
            _logger.LogInformation("Fetching all bookings. Search: '{SearchTerm}', Status: '{StatusFilter}', DateFrom: '{DateFrom}', DateTo: '{DateTo}', Cinema: {CinemaFilter}, Page: {PageNumber}",
                searchTerm, statusFilter, dateFromFilter, dateToFilter, cinemaFilter, pageNumber);

            int pageSize = 15; // Số lượng booking mỗi trang

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.StaffConfirmed) // Nhân viên xác nhận
                .Include(b => b.ShowSeats)
                    .ThenInclude(ss => ss.Show)
                        .ThenInclude(s => s.Movie) // Lấy thông tin Phim
                .Include(b => b.ShowSeats)
                    .ThenInclude(ss => ss.Show)
                        .ThenInclude(s => s.Room)
                            .ThenInclude(r => r.Cinema) // Lấy thông tin Rạp
                .OrderByDescending(b => b.Purchased)
                .AsQueryable();

            // --- Áp dụng Filters ---
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b => b.BookingCode.Contains(searchTerm) ||
                                         (b.Customer != null && (b.Customer.Name.Contains(searchTerm) || b.Customer.Email.Contains(searchTerm))));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(b => b.Status == statusFilter);
            }

            if (dateFromFilter.HasValue)
            {
                query = query.Where(b => b.Purchased.Date >= dateFromFilter.Value.Date);
            }

            if (dateToFilter.HasValue)
            {
                // Để bao gồm cả ngày kết thúc, ta cần lấy đến cuối ngày đó
                var toDateEndOfDay = dateToFilter.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(b => b.Purchased.Date <= toDateEndOfDay);
            }

            if (cinemaFilter.HasValue && cinemaFilter.Value > 0)
            {
                // Điều kiện này yêu cầu ShowSeats phải có ít nhất 1 ghế thuộc Cinema đó
                // Điều này sẽ đúng nếu tất cả ghế trong 1 booking thuộc cùng 1 show/room/cinema
                query = query.Where(b => b.ShowSeats.Any() && b.ShowSeats.FirstOrDefault().Show.Room.CinemaId == cinemaFilter.Value);
            }

            // Lọc theo Movie phức tạp hơn vì Booking không trực tiếp liên kết Movie.
            // Bạn cần join hoặc subquery nếu muốn lọc theo Movie. Tạm thời bỏ qua để đơn giản.
            // if (movieFilter.HasValue && movieFilter.Value > 0)
            // {
            //     query = query.Where(b => b.ShowSeats.Any() && b.ShowSeats.FirstOrDefault().Show.MovieId == movieFilter.Value);
            // }

            // --- Chuẩn bị dữ liệu cho ViewModel ---
            var paginatedBookings = await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);

            // Tạo SelectList cho các bộ lọc
            var statusOptions = new List<string> { "PendingPayment", "Confirmed", "CancelledByCustomer", "CancelledBySystem", "Expired", "StaffCancelled" }; // Thêm các status bạn có
                                                                                                                                                             // Lấy danh sách rạp
            var cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();

            var viewModel = new BookingListViewModel
            {
                Bookings = paginatedBookings,
                SearchTerm = searchTerm, // Để giữ lại giá trị trên form filter
                StatusFilter = statusFilter,
                DateFromFilter = dateFromFilter,
                DateToFilter = dateToFilter,
                CinemaFilter = cinemaFilter,
                // MovieFilter = movieFilter,

                StatusOptions = new SelectList(statusOptions, statusFilter),
                CinemaOptions = new SelectList(cinemas, "Id", "Name", cinemaFilter),
                // MovieOptions = ... // Sẽ cần load danh sách phim

                // Lưu lại các filter hiện tại để dùng cho link phân trang
                CurrentSearchTerm = searchTerm,
                CurrentStatusFilter = statusFilter,
                CurrentDateFromFilter = dateFromFilter,
                CurrentDateToFilter = dateToFilter,
                CurrentCinemaFilter = cinemaFilter,
                // CurrentMovieFilter = movieFilter,
            };

            return View(viewModel); // Trả về Views/StaffBooking/AllBookings.cshtml
        }
    }
}