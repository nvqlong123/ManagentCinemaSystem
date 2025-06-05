// File: ViewModels/Customer/MovieDetailViewModel.cs
using ManagentCinemaSystem.Models;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ManagentCinemaSystem.ViewModels.Customer
{
    public class MovieDetailViewModel
    {
        public Movie Movie { get; set; }
        public List<ShowtimeGroup> ShowtimesByDate { get; set; } = new List<ShowtimeGroup>();
        // Thêm các thông tin khác nếu cần, ví dụ: đánh giá, phim liên quan
    }
    public class MovieListCustomerViewModel
    {
        public PaginatedList<Movie> Movies { get; set; } // Danh sách phim đã phân trang
        public SelectList Genres { get; set; } // Danh sách thể loại cho dropdown

        // Thông tin cho filter và sort
        public string SearchTerm { get; set; }
        public int? SelectedGenreId { get; set; }
        public string SortOrder { get; set; }

        // Để giữ lại giá trị filter khi chuyển trang (nếu cần)
        public string CurrentSearchTerm { get; set; }
        public int? CurrentSelectedGenreId { get; set; }
        public string CurrentSortOrder { get; set; }

    }
    public class ShowtimeGroup // Nhóm các suất chiếu theo ngày
    {
        public DateTime Date { get; set; }
        public List<Show> Shows { get; set; } = new List<Show>();
    }
}