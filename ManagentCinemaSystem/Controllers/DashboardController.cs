using ManagentCinemaSystem.Data; // Cho ApplicationDbContext
using ManagentCinemaSystem.ViewModels.Admin; // Namespace cho DashboardViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cho ToListAsync, CountAsync, SumAsync
using Microsoft.Extensions.Logging; // Cho ILogger
using System;
using System.Linq;
using System.Threading.Tasks;


namespace ManagentCinemaSystem.Controllers // Hoặc namespace Area của bạn
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Dashboard/Index (Hoặc /Admin/Dashboard/Index nếu có Area)
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Admin Dashboard Index page accessed.");

            var viewModel = new AdminDashboardViewModel();

            try
            {
                // --- Thống kê cơ bản ---
                viewModel.TotalUsers = await _context.Users.CountAsync();
                viewModel.TotalCustomers = await _context.Users.OfType<Models.Customer>().CountAsync();
                viewModel.TotalStaff = await _context.Users.OfType<Models.Staff>().CountAsync();
                viewModel.TotalAdmins = await _context.Users.OfType<Models.Admin>().CountAsync();

                viewModel.TotalMovies = await _context.Movies.CountAsync();
                viewModel.TotalActiveMovies = await _context.Movies.CountAsync(m => m.IsActive);

                viewModel.TotalCinemas = await _context.Cinemas.CountAsync();
                viewModel.TotalRooms = await _context.Rooms.CountAsync();

                viewModel.TotalBookings = await _context.Bookings.CountAsync();
                viewModel.TotalConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed");
                viewModel.TotalPendingBookings = await _context.Bookings.CountAsync(b => b.Status == "PendingPayment" && (b.PaymentDeadline == null || b.PaymentDeadline > DateTimeOffset.UtcNow));

                // Doanh thu (ví dụ: chỉ tính từ các booking đã Confirmed)
                viewModel.TotalRevenue = await _context.Bookings
                                                .Where(b => b.Status == "Confirmed")
                                                .SumAsync(b => b.TotalCost);

                // --- Lấy một vài dữ liệu gần đây ---
                viewModel.RecentBookings = await _context.Bookings
                                                    .Include(b => b.Customer)
                                                    .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie)
                                                    .OrderByDescending(b => b.Purchased)
                                                    .Take(5) // Lấy 5 booking mới nhất
                                                    .ToListAsync();

                viewModel.RecentlyAddedMovies = await _context.Movies
                                                    .OrderByDescending(m => m.ReleaseDate) // Hoặc ngày thêm vào DB nếu có
                                                    .Take(5)
                                                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for Admin Dashboard.");
                // Có thể hiển thị thông báo lỗi trên Dashboard hoặc xử lý khác
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu cho Dashboard.";
            }


            return View(viewModel); // Trả về Views/Dashboard/Index.cshtml (hoặc Areas/Admin/Views/Dashboard/Index.cshtml)
        }

        // Các Action khác cho các trang thống kê chi tiết hơn (sẽ tạo sau)
        // public IActionResult RevenueReport() { ... }
        // public IActionResult MovieSalesReport() { ... }
    }
}