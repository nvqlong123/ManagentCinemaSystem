using Microsoft.EntityFrameworkCore;
using ManagentCinemaSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ManagentCinemaSystem.Data
{
    /// <summary>
    /// Lớp DbContext chính của ứng dụng, kế thừa từ IdentityDbContext để sử dụng ASP.NET Core Identity
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSet cho các entity
        // Lưu ý: Không cần DbSet của Admin, Staff, Customer vì:
        // - Sử dụng Table Per Hierarchy (TPH) pattern của EF Core
        // - Tất cả User được lưu trong 1 bảng AspNetUsers
        // - Phân biệt loại User bằng Discriminator column (UserType)

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<SeatType> SeatTypes { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<ShowSeat> ShowSeats { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Gọi phương thức cơ sở trước

            /* 1. CẤU HÌNH KẾ THỪA USER */
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType") // Thêm cột Discriminator để phân biệt loại User
                .HasValue<Customer>("Customer")       // Giá trị cho Customer
                .HasValue<Staff>("Staff")            // Giá trị cho Staff
                .HasValue<Admin>("Admin");           // Giá trị cho Admin

            /* 2. CẤU HÌNH USER */
            modelBuilder.Entity<User>(entity =>
            {
                // Thiết lập độ dài tối đa cho các trường
                entity.Property(u => u.Name).HasMaxLength(100);
                entity.Property(u => u.Status).HasMaxLength(50);

                // Thêm index để tối ưu truy vấn
                entity.HasIndex(u => u.Email).IsUnique(); // Email phải là duy nhất
                entity.HasIndex(u => u.PhoneNumber);      // Index cho số điện thoại
            });

            /* 3. CẤU HÌNH BOOKING */
            modelBuilder.Entity<Booking>(entity =>
            {
                // Quan hệ 1-nhiều với Customer (cho phép NULL khi Staff đặt hộ)
                entity.HasOne(b => b.Customer)
                    .WithMany(c => c.Bookings)
                    .HasForeignKey(b => b.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull); // Khi xóa Customer, set CustomerId thành NULL

                // Thiết lập độ dài tối đa
                entity.Property(b => b.Status).HasMaxLength(50);
                entity.Property(b => b.TransactionType).HasMaxLength(20);

                // Thêm index để tối ưu truy vấn
                entity.HasIndex(b => b.Purchased); // Index cho ngày đặt vé
                entity.HasIndex(b => b.Status);    // Index cho trạng thái booking
                entity.HasIndex(b => b.BookingCode).IsUnique(); // Đảm bảo BookingCode là duy nhất
            });

            /* 4. CẤU HÌNH QUAN HỆ CINEMA-ROOM */
            modelBuilder.Entity<Room>(entity =>
            {
                // Quan hệ 1-nhiều với Cinema
                entity.HasOne(r => r.Cinema)
                    .WithMany(c => c.Rooms)
                    .HasForeignKey(r => r.CinemaId)
                    .OnDelete(DeleteBehavior.Cascade); // Khi xóa Cinema thì xóa luôn các Room

                entity.HasIndex(r => r.Name); // Index cho tên phòng chiếu
            });

            /* 5. CẤU HÌNH SEAT */
            modelBuilder.Entity<Seat>(entity =>
            {
                // Quan hệ với Room
                entity.HasOne(s => s.Room)
                    .WithMany(r => r.Seats)
                    .HasForeignKey(s => s.RoomId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa Seat khi xóa Room

                // Quan hệ với SeatType
                entity.HasOne(s => s.SeatType)
                    .WithMany(st => st.Seats)
                    .HasForeignKey(s => s.SeatTypeId)
                    .OnDelete(DeleteBehavior.Restrict); // Không cho xóa SeatType nếu còn Seat

                // Unique constraint: Mỗi ghế có vị trí duy nhất trong phòng
                entity.HasIndex(s => new { s.RoomId, s.Row, s.Col }).IsUnique();
            });

            /* 6. CẤU HÌNH SHOW */
            modelBuilder.Entity<Show>(entity =>
            {
                // Quan hệ với Movie
                entity.HasOne(s => s.Movie)
                    .WithMany(m => m.Shows)
                    .HasForeignKey(s => s.MovieId)
                    .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Movie nếu còn Show

                // Quan hệ với Room
                entity.HasOne(s => s.Room)
                    .WithMany(r => r.Shows)
                    .HasForeignKey(s => s.RoomId)
                    .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Room nếu còn Show

                // Ràng buộc: Thời gian kết thúc phải sau thời gian bắt đầu
                entity.ToTable(t => t.HasCheckConstraint("CK_Show_Time", "EndTime > StartTime"));

                // Index để tối ưu truy vấn theo thời gian
                entity.HasIndex(s => s.StartTime);
                entity.HasIndex(s => s.EndTime);
            });

            /* 7. CẤU HÌNH SHOWSEAT */
            modelBuilder.Entity<ShowSeat>(entity =>
            {
                // Quan hệ với Show
                entity.HasOne(ss => ss.Show)
                    .WithMany(s => s.ShowSeats)
                    .HasForeignKey(ss => ss.ShowId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa ShowSeat khi xóa Show

                // Quan hệ với Seat
                entity.HasOne(ss => ss.Seat)
                    .WithMany()
                    .HasForeignKey(ss => ss.SeatId)
                    .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Seat nếu còn ShowSeat

                // Quan hệ với Booking (cho phép NULL khi chưa đặt)
                entity.HasOne(ss => ss.Booking)
                    .WithMany(b => b.ShowSeats)
                    .HasForeignKey(ss => ss.BookingId)
                    .OnDelete(DeleteBehavior.SetNull); // Khi xóa Booking, set BookingId thành NULL

                entity.HasIndex(ss => ss.IsBooked); // Index để lọc nhanh ghế đã đặt/chưa đặt
            });

            /* 8. CẤU HÌNH MOVIEGENRE (QUAN HỆ NHIỀU-NHIỀU) */
            modelBuilder.Entity<MovieGenre>(entity =>
            {
                // Composite key từ MovieId và GenreId
                entity.HasKey(mg => new { mg.MovieId, mg.GenreId });

                // Quan hệ với Movie
                entity.HasOne(mg => mg.Movie)
                    .WithMany(m => m.MovieGenres)
                    .HasForeignKey(mg => mg.MovieId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa MovieGenre khi xóa Movie

                // Quan hệ với Genre
                entity.HasOne(mg => mg.Genre)
                    .WithMany(g => g.MovieGenres)
                    .HasForeignKey(mg => mg.GenreId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa MovieGenre khi xóa Genre
            });

            /* 9. CẤU HÌNH MOVIE */
            modelBuilder.Entity<Movie>(entity =>
            {
                // Thiết lập độ dài tối đa
                entity.Property(m => m.Title).HasMaxLength(100);
                entity.Property(m => m.Director).HasMaxLength(100);
                entity.Property(m => m.Actor).HasMaxLength(200);
                entity.Property(m => m.Poster).HasMaxLength(2048); // Độ dài URL tối đa

                // Index để tối ưu tìm kiếm
                entity.HasIndex(m => m.Title);       // Index cho tên phim
                entity.HasIndex(m => m.ReleaseDate); // Index cho ngày phát hành
            });
        }
    }
}