// File: Controllers/HomeController.cs
using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System;
using ManagentCinemaSystem.Models.ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels.Showtime;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace ManagentCinemaSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Home/ShowtimesByDate?date=yyyy-MM-dd
        public async Task<IActionResult> ShowtimesByDate(
    DateTime? date, string searchBy, string keyword, string sortBy, string sortOrder)
        {
            var selectedDate = date ?? DateTime.Today;
            var start = selectedDate.Date;
            var end = start.AddDays(1);

            var query = _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema)
                .Where(s => s.StartTime >= start && s.StartTime < end);
            if (!string.IsNullOrEmpty(keyword))
            {
                switch (searchBy)
                {
                    case "CinemaName":
                        query = query.Where(s => s.Room.Cinema.Name.Contains(keyword));
                        break;
                    case "RoomName":
                        query = query.Where(s => s.Room.Name.Contains(keyword));
                        break;
                    default:
                        query = query.Where(s => s.Movie.Title.Contains(keyword));
                        break;
                }
            }
            // Sắp xếp
            sortBy = sortBy ?? "StartTime";
            sortOrder = sortOrder ?? "asc";
            switch (sortBy)
            {
                case "MovieTitle":
                    query = sortOrder == "asc" ? query.OrderBy(s => s.Movie.Title) : query.OrderByDescending(s => s.Movie.Title);
                    break;
                case "StartTime":
                    query = sortOrder == "asc" ? query.OrderBy(s => s.StartTime) : query.OrderByDescending(s => s.StartTime);
                    break;
                case "CinemaName":
                    query = sortOrder == "asc" ? query.OrderBy(s => s.Room.Cinema.Name) : query.OrderByDescending(s => s.Room.Cinema.Name);
                    break;
                case "RoomName":
                    query = sortOrder == "asc" ? query.OrderBy(s => s.Room.Name) : query.OrderByDescending(s => s.Room.Name);
                    break;
                default:
                    query = query.OrderBy(s => s.StartTime);
                    break;
            }

            var showtimes = await query
                .Select(s => new ShowtimeViewModel
                {
                    Id = s.Id,
                    MovieTitle = s.Movie.Title,
                    StartTime = s.StartTime,
                    CinemaName = s.Room.Cinema.Name,
                    RoomName = s.Room.Name
                })
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchBy = searchBy;
            ViewBag.Keyword = keyword;
            return View(showtimes);
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var nowShowingMovies = await _context.Movies
                .Where(m => m.IsActive && m.ReleaseDate.Date <= today)
                .OrderByDescending(m => m.ReleaseDate)
                .Take(8)
                .ToListAsync();

            var comingSoonMovies = await _context.Movies
                .Where(m => m.IsActive && m.ReleaseDate.Date > today)
                .OrderBy(m => m.ReleaseDate)
                .Take(8)
                .ToListAsync();

            var viewModel = new HomeIndexViewModel
            {
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = comingSoonMovies
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    }