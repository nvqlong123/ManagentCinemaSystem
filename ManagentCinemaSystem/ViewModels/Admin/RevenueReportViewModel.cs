using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.ViewModels.Admin.Statistics
{
    public class RevenueReportViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Từ Ngày")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7); // Mặc định 7 ngày trước

        [DataType(DataType.Date)]
        [Display(Name = "Đến Ngày")]
        public DateTime EndDate { get; set; } = DateTime.Today; // Mặc định hôm nay

        public List<DailyRevenue> DailyRevenues { get; set; } = new List<DailyRevenue>();
        public decimal TotalRevenueForPeriod { get; set; }

        // Dữ liệu cho biểu đồ (ví dụ)
        public List<string> ChartLabels { get; set; } = new List<string>(); // Ngày
        public List<decimal> ChartData { get; set; } = new List<decimal>(); // Doanh thu

        public string ReportType { get; set; } // "Daily", "Monthly"
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TicketCount { get; set; }
    }
    public class TicketSalesReportViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Từ Ngày")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);

        [DataType(DataType.Date)]
        [Display(Name = "Đến Ngày")]
        public DateTime EndDate { get; set; } = DateTime.Today;

        public List<DailyTicketsSold> DailyTickets { get; set; } = new List<DailyTicketsSold>();
        public int TotalTicketsForPeriod { get; set; }

        public List<string> ChartLabels { get; set; } = new List<string>(); // Ngày hoặc Tháng/Năm
        public List<int> ChartData { get; set; } = new List<int>();    // Số lượng vé

        public string ReportType { get; set; } // "Daily", "Monthly"
    }

    public class DailyTicketsSold // Có thể dùng chung DailyRevenue nếu muốn, chỉ đổi tên biến
    {
        public DateTime Date { get; set; } // Ngày (cho daily) hoặc Ngày đầu tháng (cho monthly)
        public int TicketCount { get; set; }
        public decimal AssociatedRevenue { get; set; } // Doanh thu tương ứng với số vé này
    }
    public class MoviePerformanceReportViewModel
    {
        // Filters (Date range, có thể thêm Genre, Cinema)
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Today;

        public List<MoviePerformanceData> Performances { get; set; } = new List<MoviePerformanceData>();

        public List<string> ChartMovieTitles { get; set; } = new List<string>();
        public List<int> ChartTicketsSold { get; set; } = new List<int>();
        public List<decimal> ChartRevenue { get; set; } = new List<decimal>();
    }

    public class MoviePerformanceData
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public int TicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalShows { get; set; } // Số suất chiếu trong khoảng thời gian
    }
}