using ManagentCinemaSystem.Models; // Namespace cho Booking, Movie
using System.Collections.Generic;

namespace ManagentCinemaSystem.ViewModels.Admin // Hoặc namespace ViewModel của bạn
{
    public class AdminDashboardViewModel
    {
        // Thống kê Người dùng
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalAdmins { get; set; }

        // Thống kê Phim
        public int TotalMovies { get; set; }
        public int TotalActiveMovies { get; set; }

        // Thống kê Rạp
        public int TotalCinemas { get; set; }
        public int TotalRooms { get; set; }

        // Thống kê Đặt vé
        public int TotalBookings { get; set; }
        public int TotalConfirmedBookings { get; set; }
        public int TotalPendingBookings { get; set; }
        public decimal TotalRevenue { get; set; } // Doanh thu có thể là decimal

        // Danh sách gần đây
        public List<ManagentCinemaSystem.Models.Booking> RecentBookings { get; set; } = new List<ManagentCinemaSystem.Models.Booking>();
        public List<Movie> RecentlyAddedMovies { get; set; } = new List<Movie>();

        // Có thể thêm các biểu đồ sau này
        // public ChartData BookingTrendChart { get; set; }
        // public ChartData RevenueByMovieChart { get; set; }

        public AdminDashboardViewModel()
        {
            // Khởi tạo giá trị mặc định nếu cần
            RecentBookings = new List<ManagentCinemaSystem.Models.Booking>();
            RecentlyAddedMovies = new List<Movie>();
        }
    }
}