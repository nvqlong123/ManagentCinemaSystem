using ManagentCinemaSystem.Models;
using System.Collections.Generic;

namespace ManagentCinemaSystem.ViewModels.Customer
{
    public class HomeIndexViewModel
    {
        public List<Movie> NowShowingMovies { get; set; } = new List<Movie>();
        public List<Movie> ComingSoonMovies { get; set; } = new List<Movie>();
        // public List<Movie> FeaturedMovies { get; set; } = new List<Movie>(); // Nếu có
    }
}