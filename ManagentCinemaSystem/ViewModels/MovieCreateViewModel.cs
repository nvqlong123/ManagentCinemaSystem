using ManagentCinemaSystem.Models;
using System; // Required for DateTime
using System.Collections.Generic; // Required for List
using System.ComponentModel.DataAnnotations;

// Custom validation attribute
public class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute() { }

    public string GetErrorMessage() => "Ngày phát hành phải là hôm nay hoặc trong tương lai.";

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime date && date.Date >= DateTime.Today.Date)
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(GetErrorMessage());
    }
}

public class MovieCreateViewModel
{
    [Required(ErrorMessage = "Tên phim không được để trống")]
    [StringLength(100, ErrorMessage = "Tên phim tối đa 100 ký tự")]
    [Display(Name = "Tên phim")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Tên đạo diễn không được để trống")]
    [StringLength(100, ErrorMessage = "Tên đạo diễn tối đa 100 ký tự")]
    [Display(Name = "Đạo diễn")]
    public string Director { get; set; }

    [Required(ErrorMessage = "Diễn viên không được để trống")]
    [StringLength(200, ErrorMessage = "Tên diễn viên tối đa 200 ký tự")]
    [Display(Name = "Diễn viên chính")]
    public string Actors { get; set; }

    [Required(ErrorMessage = "Thời lượng không được để trống")]
    [Range(1, 500, ErrorMessage = "Thời lượng phải từ 1 đến 500 phút")]
    [Display(Name = "Thời lượng (phút)")]
    public int Duration { get; set; }

    [Required(ErrorMessage = "Ngày phát hành không được để trống")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày phát hành")]
    [FutureDate] // Using custom attribute
    public DateTime ReleaseDate { get; set; }

    // Made PosterUrl not required initially, can be updated later
    // If required: [Required(ErrorMessage = "URL poster không được để trống")]
    [Url(ErrorMessage = "URL poster không hợp lệ")]
    [StringLength(2048, ErrorMessage = "URL poster tối đa 2048 ký tự")]
    [Display(Name = "Poster (URL)")]
    public string PosterUrl { get; set; }

    [Display(Name = "Đang hoạt động (chiếu/sắp chiếu)")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Thể loại")]
    public List<int> SelectedGenreIds { get; set; } = new List<int>();

    public List<Genre> AvailableGenres { get; set; } = new List<Genre>();
}