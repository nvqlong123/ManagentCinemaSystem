using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.ViewModels.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagentCinemaSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Tổng doanh thu
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .SumAsync(b => (int?)b.TotalCost) ?? 0;

            // Tổng số vé đã bán
            var totalTickets = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .SelectMany(b => b.ShowSeats)
                .CountAsync();

            // Tổng số khách hàng đã mua vé
            var totalCustomers = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .Select(b => b.CustomerId)
                .Distinct()
                .CountAsync();

            // Doanh thu 7 ngày gần nhất
            var revenueByDates = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => b.Purchased.Date)
                .Select(g => new RevenueByDate
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(b => b.TotalCost)
                })
                .OrderByDescending(x => x.Date)
                .Take(7)
                .ToListAsync();

            // Top 5 phim doanh thu cao nhất
            var revenueByMovies = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .SelectMany(b => b.ShowSeats)
                .GroupBy(ss => ss.Show.Movie.Title)
                .Select(g => new RevenueByMovie
                {
                    MovieTitle = g.Key,
                    TotalRevenue = g.Sum(ss => ss.Seat.SeatType.Cost)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToListAsync();

            // Top 5 suất chiếu gần nhất - tỷ lệ lấp đầy
            var showOccupancies = await _context.Shows
                .OrderByDescending(s => s.StartTime)
                .Take(5)
                .Select(s => new ShowOccupancy
                {
                    MovieTitle = s.Movie.Title,
                    StartTime = s.StartTime,
                    RoomName = s.Room.Name,
                    TotalSeats = s.ShowSeats.Count,
                    BookedSeats = s.ShowSeats.Count(ss => ss.IsBooked),
                    OccupancyRate = s.ShowSeats.Count == 0 ? 0 : Math.Round(100.0 * s.ShowSeats.Count(ss => ss.IsBooked) / s.ShowSeats.Count, 2)
                })
                .ToListAsync();

            // Top 5 khách hàng đặt vé nhiều nhất
            var topCustomers = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => new { b.Customer.Name, b.Customer.Email })
                .Select(g => new TopCustomer
                {
                    CustomerName = g.Key.Name,
                    Email = g.Key.Email,
                    TotalTickets = g.SelectMany(b => b.ShowSeats).Count(),
                    TotalSpent = g.Sum(b => b.TotalCost)
                })
                .OrderByDescending(x => x.TotalTickets)
                .Take(5)
                .ToListAsync();

            var vm = new StatisticsViewModel
            {
                TotalRevenue = totalRevenue,
                TotalTickets = totalTickets,
                TotalCustomers = totalCustomers,
                RevenueByDates = revenueByDates,
                RevenueByMovies = revenueByMovies,
                ShowOccupancies = showOccupancies,
                TopCustomers = topCustomers
            };

            return View(vm);
        }
    }
}