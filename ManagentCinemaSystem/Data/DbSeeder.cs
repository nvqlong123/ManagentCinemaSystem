using Microsoft.AspNetCore.Identity;
using ManagentCinemaSystem.Models;

namespace ManagentCinemaSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                // Tạo các roles
                string[] roleNames = { "Admin", "Staff", "Customer" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Tạo admin account
                var adminUser = new Admin
                {
                    UserName = "admin@cinema.com",
                    Email = "admin@cinema.com",
                    Name = "System Admin",
                    Phone = "0123456789",
                    Status = "Active",
                    EmailConfirmed = true
                };

                var userExists = await userManager.FindByEmailAsync(adminUser.Email);
                if (userExists == null)
                {
                    var createAdminUser = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (createAdminUser.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }

        public static async Task SeedSampleDataAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Kiểm tra xem đã có dữ liệu chưa
                if (!context.Cinemas.Any())
                {
                    // Thêm rạp phim mẫu
                    var cinema = new Cinema
                    {
                        Name = "Cinema City",
                        Address = "123 Đường ABC, Quận 1, TP.HCM"
                    };
                    context.Cinemas.Add(cinema);
                    await context.SaveChangesAsync();

                    // Thêm phòng chiếu
                    var room = new Room
                    {
                        Name = "Phòng 1",
                        CinemaId = cinema.Id
                    };
                    context.Rooms.Add(room);
                    await context.SaveChangesAsync();

                    // Thêm loại ghế
                    var seatTypes = new[]
                    {
                        new SeatType { Name = "Thường", Cost = 70000 },
                        new SeatType { Name = "VIP", Cost = 90000 },
                        new SeatType { Name = "Đôi", Cost = 150000 }
                    };
                    context.SeatTypes.AddRange(seatTypes);
                    await context.SaveChangesAsync();

                    // Thêm ghế ngồi
                    var seats = new List<Seat>();
                    for (char row = 'A'; row <= 'E'; row++)
                    {
                        for (int col = 1; col <= 10; col++)
                        {
                            seats.Add(new Seat
                            {
                                Row = row,
                                Col = col,
                                RoomId = room.Id,
                                SeatTypeId = row <= 'C' ? seatTypes[0].Id : seatTypes[1].Id
                            });
                        }
                    }
                    context.Seats.AddRange(seats);
                    await context.SaveChangesAsync();

                    // Thêm thể loại phim
                    var genres = new[]
                    {
                        new Genre { Name = "Hành động" },
                        new Genre { Name = "Tình cảm" },
                        new Genre { Name = "Kinh dị" },
                        new Genre { Name = "Hoạt hình" }
                    };
                    context.Genres.AddRange(genres);
                    await context.SaveChangesAsync();

                    // Thêm phim mẫu
                    var movie = new Movie
                    {
                        Title = "Phim mẫu",
                        Director = "Đạo diễn",
                        Actor = "Diễn viên 1, Diễn viên 2",
                        ReleaseDate = DateTime.Now,
                        Duration = 120,
                        Poster = "https://example.com/poster.jpg"
                    };
                    context.Movies.Add(movie);
                    await context.SaveChangesAsync();

                    // Liên kết phim với thể loại
                    context.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = movie.Id,
                        GenreId = genres[0].Id
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}