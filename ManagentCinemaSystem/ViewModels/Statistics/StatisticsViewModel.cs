namespace ManagentCinemaSystem.ViewModels.Statistics
{
    public class StatisticsViewModel
    {
        // Tổng doanh thu
        public int TotalRevenue { get; set; }
        // Tổng số vé đã bán
        public int TotalTickets { get; set; }
        // Tổng số khách hàng đã mua vé
        public int TotalCustomers { get; set; }

        // Doanh thu theo ngày
        public List<RevenueByDate> RevenueByDates { get; set; } = new();
        // Số vé bán ra theo ngày
        public List<TicketCountByDate> TicketCountByDates { get; set; } = new();
        // Doanh thu theo phim
        public List<RevenueByMovie> RevenueByMovies { get; set; } = new();
        // Tỷ lệ lấp đầy suất chiếu
        public List<ShowOccupancy> ShowOccupancies { get; set; } = new();
        // Khách hàng đặt vé nhiều nhất
        public List<TopCustomer> TopCustomers { get; set; } = new();
    }

    public class RevenueByDate
    {
        public DateTime Date { get; set; }
        public int TotalRevenue { get; set; }
    }

    public class TicketCountByDate
    {
        public DateTime Date { get; set; }
        public int TicketCount { get; set; }
    }

    public class RevenueByMovie
    {
        public string MovieTitle { get; set; }
        public int TotalRevenue { get; set; }
    }

    public class ShowOccupancy
    {
        public int ShowId { get; set; }
        public string MovieTitle { get; set; }
        public DateTime StartTime { get; set; }
        public string RoomName { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public double OccupancyRate { get; set; } // %
    }

    public class TopCustomer
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int TotalTickets { get; set; }
        public int TotalSpent { get; set; }
    }
}