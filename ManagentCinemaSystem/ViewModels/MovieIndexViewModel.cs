using ManagentCinemaSystem.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Required for List

public class MovieIndexViewModel
{
    // Danh sách phim hiển thị
    public List<Movie> Movies { get; set; } = new List<Movie>();

    // Danh sách thể loại cho dropdown filter
    public List<Genre> Genres { get; set; } = new List<Genre>();

    // Các tham số filter
    [Display(Name = "Tìm kiếm theo tên")]
    public string SearchTerm { get; set; }

    [Display(Name = "Thể loại")]
    public int? SelectedGenreId { get; set; }

    [Display(Name = "Trạng thái")]
    public bool? IsActiveFilter { get; set; } = true; // Default to showing active movies
}