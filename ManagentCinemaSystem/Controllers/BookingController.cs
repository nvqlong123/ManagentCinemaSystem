using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels.Booking; // Namespace cho ViewModels đặt vé
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers
{
    [Authorize(Roles = "Customer")] // Chỉ Customer mới được đặt vé
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<BookingController> _logger;
        public BookingController(ApplicationDbContext context, UserManager<User> userManager, ILogger<BookingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Booking/SelectSeats/5 (5 là showId)
        [HttpGet("Booking/SelectSeats/{showId:int}")]
        
        public async Task<IActionResult> SelectSeats(int showId)
        {
            var show = await _context.Shows
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(s => s.ShowSeats).ThenInclude(ss => ss.Seat).ThenInclude(seat => seat.SeatType)
                .FirstOrDefaultAsync(s => s.Id == showId);

            if (show == null)
            {
                TempData["Error"] = "Không tìm thấy suất chiếu.";
                return RedirectToAction("Index", "Home");
            }

            if (show.StartTime <= DateTime.Now)
            {
                TempData["Error"] = "Suất chiếu này đã bắt đầu hoặc đã kết thúc.";
                // Chuyển về trang lịch chiếu trong ngày
                return RedirectToAction("ShowtimesByDate", "Home", new { date = show.StartTime.ToString("yyyy-MM-dd") });
            }

            var seatViewModels = show.ShowSeats
                .OrderBy(ss => ss.Seat.Row).ThenBy(ss => ss.Seat.Col)
                .Select(ss => new ShowSeatViewModel
                {
                    ShowSeatId = ss.Id,
                    SeatRow = ss.Seat.Row,
                    SeatCol = ss.Seat.Col,
                    SeatTypeName = ss.Seat.SeatType.Name,
                    SeatTypeCost = ss.Seat.SeatType.Cost,
                    IsBooked = ss.IsBooked
                }).ToList();

            // Tìm hàng và cột lớn nhất để vẽ sơ đồ (đơn giản hóa)
            char maxRow = 'A';
            int maxCol = 0;
            if (seatViewModels.Any())
            {
                maxRow = seatViewModels.Max(s => s.SeatRow);
                maxCol = seatViewModels.Max(s => s.SeatCol);
            }


            var viewModel = new SeatSelectionViewModel
            {
                ShowId = show.Id,
                MovieId = show.MovieId,
                MovieTitle = show.Movie.Title,
                StartTime = show.StartTime,
                RoomName = show.Room.Name,
                CinemaName = show.Room.Cinema.Name,
                Seats = seatViewModels,
                MaxRow = maxRow,
                MaxCol = maxCol
            };

            return View(viewModel); // Trả về Views/Booking/SelectSeats.cshtml
        }

        // POST: /Booking/ConfirmBooking (Từ form chọn ghế)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(SeatSelectionViewModel model) // Vẫn nhận ViewModel để lấy ShowId và các thông tin khác nếu cần
        {
            // --- BƯỚC 1: Lấy chuỗi ID ghế thô từ Form ---
            // Sử dụng Request.Form để truy cập trực tiếp dữ liệu form gửi lên.
            // Tên key phải khớp chính xác với thuộc tính 'name' của input ẩn trong HTML.
            string rawSeatIds = Request.Form["SelectedShowSeatIds"].FirstOrDefault();

            // Log để debug (xóa sau khi hoạt động ổn định)
            Console.WriteLine($"--- ConfirmBooking Action ---");
            Console.WriteLine($"Raw 'SelectedShowSeatIds' string from Form: '{rawSeatIds}'");
            Console.WriteLine($"ShowId from ViewModel: {model.ShowId}"); // Lấy ShowId từ model

            // --- BƯỚC 2: Parse chuỗi thô thành List<int> ---
            List<int> parsedSeatIds = new List<int>(); // Khởi tạo danh sách rỗng

            if (!string.IsNullOrEmpty(rawSeatIds))
            {
                try
                {
                    // Tách chuỗi bằng dấu phẩy
                    var idStrings = rawSeatIds.Split(',');

                    foreach (var idString in idStrings)
                    {
                        // Loại bỏ khoảng trắng thừa (nếu có) và thử chuyển đổi sang int
                        if (int.TryParse(idString.Trim(), out int parsedId))
                        {
                            // Nếu chuyển đổi thành công, thêm vào danh sách
                            parsedSeatIds.Add(parsedId);
                        }
                        else
                        {
                            // Log lỗi nếu một phần tử không phải là số (không nên xảy ra với JS hiện tại)
                            Console.WriteLine($"Warning: Could not parse '{idString.Trim()}' to an integer.");
                            // Bạn có thể quyết định dừng lại hoặc bỏ qua phần tử lỗi này
                        }
                    }
                    Console.WriteLine($"Successfully parsed {parsedSeatIds.Count} seat IDs.");
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu có vấn đề nghiêm trọng trong quá trình parse
                    Console.WriteLine($"Error parsing seat IDs: {ex.Message}");
                    ModelState.AddModelError("SelectedShowSeatIds", "Định dạng danh sách ghế không hợp lệ.");
                    TempData["Error"] = "Lỗi xử lý danh sách ghế đã chọn.";
                    // Cần trả về view SelectSeats với dữ liệu được load lại
                    // return View("SelectSeats", await PrepareSelectSeatsViewModel(model.ShowId)); // Cần hàm helper Prepare...
                    return RedirectToAction(nameof(SelectSeats), new { showId = model.ShowId });
                }
            }
            else
            {
                // Chuỗi rỗng hoặc null, nghĩa là không có ghế nào được gửi lên
                Console.WriteLine("'SelectedShowSeatIds' string was null or empty.");
            }

            // --- BƯỚC 3: Gán danh sách đã parse vào ViewModel (quan trọng) ---
            // Ghi đè lên giá trị mà Model Binder có thể đã gán sai (hoặc không gán)
            model.SelectedShowSeatIds = parsedSeatIds;

            // --- BƯỚC 4: Kiểm tra kết quả parse và tiếp tục logic ---
            if (model.SelectedShowSeatIds == null || !model.SelectedShowSeatIds.Any())
            {
                // Thêm lỗi vào ModelState nếu muốn hiển thị trên view khác
                // ModelState.AddModelError(nameof(model.SelectedShowSeatIds), "Vui lòng chọn ít nhất một ghế.");
                TempData["Error"] = "Bạn chưa chọn ghế nào hoặc có lỗi xảy ra khi xử lý lựa chọn của bạn.";
                // Chuyển hướng về trang chọn ghế, truyền lại ShowId
                return RedirectToAction(nameof(SelectSeats), new { showId = model.ShowId });
            }

            // --- BƯỚC 5: Tiếp tục logic còn lại của Action với model.SelectedShowSeatIds đã đúng ---

            var user = await _userManager.GetUserAsync(User);



            if (user == null)
            {
                return Challenge(); // Hoặc redirect về trang đăng nhập
            }

            // Lấy thông tin chi tiết của các ghế đã chọn DÙNG DANH SÁCH ĐÃ PARSE
            var selectedShowSeats = await _context.ShowSeats
                .Include(ss => ss.Seat).ThenInclude(s => s.SeatType)
                .Include(ss => ss.Show).ThenInclude(s => s.Movie)
                .Include(ss => ss.Show).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Where(ss => model.SelectedShowSeatIds.Contains(ss.Id) && ss.ShowId == model.ShowId) // Dùng model.SelectedShowSeatIds đã được gán đúng
                .ToListAsync();

            // --- KIỂM TRA ĐỒNG THỜI (Concurrency Check) LẦN 1 ---
            if (selectedShowSeats.Count != model.SelectedShowSeatIds.Count) // So sánh với Count đã parse được
            {
                TempData["Error"] = "Một hoặc nhiều ghế bạn chọn không hợp lệ hoặc thông tin đã thay đổi.";
                return RedirectToAction(nameof(SelectSeats), new { showId = model.ShowId });
            }
            if (selectedShowSeats.Any(ss => ss.IsBooked))
            {
                var bookedSeats = selectedShowSeats.Where(ss => ss.IsBooked)
                                                  .Select(ss => $"{ss.Seat.Row}{ss.Seat.Col}");
                TempData["Error"] = $"Các ghế sau đã có người đặt trong lúc bạn chọn: {string.Join(", ", bookedSeats)}. Vui lòng chọn lại.";
                return RedirectToAction(nameof(SelectSeats), new { showId = model.ShowId });
            }
            // Kiểm tra lại thời gian show (lấy từ ghế đầu tiên)
            var showInfoForCheck = selectedShowSeats.FirstOrDefault()?.Show;
            if (showInfoForCheck == null || showInfoForCheck.StartTime <= DateTime.Now)
            {
                TempData["Error"] = "Suất chiếu không hợp lệ hoặc đã bắt đầu/kết thúc trong lúc bạn chọn ghế.";
                // Chuyển về trang chi tiết phim nếu có MovieId, nếu không về Home
                return RedirectToAction("Details", "Movie", new { id = showInfoForCheck?.MovieId ?? 0 }); // Cần xử lý nếu MovieId = 0
            }
            // --- KẾT THÚC KIỂM TRA ĐỒNG THỜI LẦN 1 ---

            // Tính tổng tiền
            int totalCost = selectedShowSeats.Sum(ss => ss.Seat.SeatType.Cost);

            // Tạo ViewModel cho trang xác nhận
            var confirmationViewModel = new BookingConfirmationViewModel
            {
                ShowId = model.ShowId,
                MovieTitle = showInfoForCheck.Movie.Title,
                StartTime = showInfoForCheck.StartTime,
                RoomName = showInfoForCheck.Room.Name,
                CinemaName = showInfoForCheck.Room.Cinema.Name,
                SelectedShowSeatIds = model.SelectedShowSeatIds, // Truyền danh sách ID đã parse đúng
                SelectedSeatDetails = selectedShowSeats.Select(ss => new ShowSeatViewModel
                {
                    SeatRow = ss.Seat.Row,
                    SeatCol = ss.Seat.Col,
                    SeatTypeName = ss.Seat.SeatType.Name,
                    SeatTypeCost = ss.Seat.SeatType.Cost
                }).ToList(),
                TotalCost = totalCost,
                CustomerName = user.Name,
                CustomerEmail = user.Email
            };

            // Trả về view xác nhận
            return View("ConfirmBooking", confirmationViewModel);
        }


        // POST: /Booking/CompleteBooking (Từ form xác nhận)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(BookingConfirmationViewModel model) // ViewModel này vẫn dùng để nhận ID ghế từ bước trước
        {
            if (model.SelectedShowSeatIds == null || !model.SelectedShowSeatIds.Any())
            {
                TempData["Error"] = "Không tìm thấy thông tin ghế đã chọn để hoàn tất đặt vé.";
                return RedirectToAction("Index", "Home"); // Hoặc quay lại trang trước đó nếu có ShowId
            }

            var user = await _userManager.GetUserAsync(User);
            //var userId = _userManager.GetUserId(User);
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"{claim.Type} = {claim.Value}");
            }
            if (user == null)
            {
                // User không phải Customer hoặc có lỗi, Challenge() sẽ yêu cầu đăng nhập
                _logger.LogWarning("User attempted to complete booking but is not a Customer or user is null.");
                return Challenge();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var showSeatsToBook = await _context.ShowSeats
                    .Include(ss => ss.Seat).ThenInclude(s => s.SeatType) // Để tính tiền và hiển thị
                    .Include(ss => ss.Show) // Để kiểm tra thời gian
                    .Where(ss => model.SelectedShowSeatIds.Contains(ss.Id) && ss.ShowId == model.ShowId)
                    .ToListAsync();

                // --- KIỂM TRA ĐỒNG THỜI (Concurrency Check) ---
                if (showSeatsToBook.Count != model.SelectedShowSeatIds.Count)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Thông tin ghế bạn chọn đã thay đổi. Vui lòng thử lại.";
                    return RedirectToAction("SelectSeats", "Booking", new { showId = model.ShowId });
                }
                if (showSeatsToBook.Any(ss => ss.IsBooked))
                {
                    await transaction.RollbackAsync();
                    var alreadyBookedSeats = showSeatsToBook.Where(ss => ss.IsBooked).Select(ss => $"{ss.Seat.Row}{ss.Seat.Col}");
                    TempData["Error"] = $"Rất tiếc, các ghế sau đã bị người khác đặt trước: {string.Join(", ", alreadyBookedSeats)}. Vui lòng chọn lại.";
                    return RedirectToAction("SelectSeats", "Booking", new { showId = model.ShowId });
                }
                var showForBooking = showSeatsToBook.FirstOrDefault()?.Show;
                if (showForBooking == null || showForBooking.StartTime <= DateTime.Now)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Suất chiếu không hợp lệ hoặc đã bắt đầu/kết thúc.";
                    return RedirectToAction("Details", "Movie", new { id = showForBooking?.MovieId ?? model.ShowId }); // Cần ShowId hoặc MovieId để quay lại
                }
                // --- KẾT THÚC KIỂM TRA ĐỒNG THỜI ---

                string bookingCode = $"BK{DateTime.UtcNow.Ticks.ToString().Substring(8)}{new Random().Next(100, 999)}"; // Tạo mã booking đơn giản
                DateTime paymentDeadline = DateTime.UtcNow.AddMinutes(15); // Ví dụ: 15 phút để thanh toán

                var newBooking = new Models.Booking
                {
                    BookingCode = bookingCode,
                    CustomerId = user.Id,
                    Purchased = DateTime.UtcNow, // Thời điểm bắt đầu quy trình
                    TotalCost = model.TotalCost, // Lấy từ model xác nhận
                    Status = "PendingPayment", // Trạng thái chờ thanh toán
                    TransactionType = "QRCode",
                    PaymentDeadline = paymentDeadline
                };
                _context.Bookings.Add(newBooking);
                await _context.SaveChangesAsync(); // Lưu để lấy BookingId

                // Tạm thời đánh dấu ghế là đã đặt và gán BookingId
                // Sẽ cần quy trình hoàn lại nếu không thanh toán
                foreach (var showSeat in showSeatsToBook)
                {
                    showSeat.IsBooked = true;
                    showSeat.BookingId = newBooking.Id;
                    _context.ShowSeats.Update(showSeat);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Booking {BookingCode} created with status PendingPayment for user {UserId}. Redirecting to QR display.", newBooking.BookingCode, user.Id);
                // Chuyển hướng đến trang hiển thị QR
                return RedirectToAction(nameof(DisplayPaymentQR), new { bookingId = newBooking.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during CompleteBooking for ShowId {ShowId} and User {UserId}", model.ShowId, user?.Id);
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình xử lý. Vui lòng thử lại. " + ex.Message;
                return RedirectToAction("SelectSeats", "Booking", new { showId = model.ShowId });
            }
        }
        [HttpGet("Booking/DisplayPaymentQR/{bookingId:int}")]
        public async Task<IActionResult> DisplayPaymentQR(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            var currentUserId = _userManager.GetUserId(User);

            if (booking == null || booking.CustomerId != currentUserId)
            {
                TempData["Error"] = "Không tìm thấy đặt vé hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index", "Home");
            }

            if (booking.Status != "PendingPayment")
            {
                TempData["Info"] = $"Đặt vé {booking.BookingCode} đã ở trạng thái {booking.Status}.";
                // Nếu đã Confirmed, chuyển đến trang thành công, nếu khác thì về trang chủ/lịch sử
                if (booking.Status == "Confirmed") return RedirectToAction(nameof(BookingSuccess), new { bookingId = booking.Id });
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.PaymentDeadline.HasValue && booking.PaymentDeadline.Value < DateTime.UtcNow)
            {
                // (Tùy chọn) Tự động chuyển trạng thái sang Expired ở đây nếu muốn
                // booking.Status = "Expired";
                // await _context.SaveChangesAsync(); // Cần xử lý hoàn ghế nếu làm vậy
                TempData["Error"] = $"Đặt vé {booking.BookingCode} đã hết hạn thanh toán.";
                return RedirectToAction(nameof(MyBookings));
            }

            // --- Thông tin tài khoản ngân hàng của bạn (Lấy từ cấu hình hoặc hardcode tạm) ---
            string bankName = "Ngân hàng MB"; // Thay thế bằng tên ngân hàng của bạn
            string bankAccountNumber = "123456789"; // Thay thế bằng STK của bạn
            string bankAccountName = "CONG TY TRACH NHIEM HUU HAN DUT CINEMA"; // Thay thế bằng tên chủ TK
                                                    // --------------------------------------------------------------------------------

            // Nội dung chuyển khoản BẮT BUỘC phải có BookingCode để đối soát
            string paymentContent = $"TT {booking.BookingCode}"; // Ví dụ: "TT BK123XYZ789"

            // --- Tạo QR Code ---
            string qrPayload;
            qrPayload = $"Nội dung: {paymentContent} Số tiền: {booking.TotalCost}"; // Payload đơn giản

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImageBytes = qrCode.GetGraphic(20); // Kích thước ảnh QR
            string base64Image = Convert.ToBase64String(qrCodeImageBytes);
            // --- Kết thúc Tạo QR Code ---

            var firstShowSeat = booking.ShowSeats.FirstOrDefault(); // Để lấy thông tin phim/suất chiếu

            var viewModel = new PaymentQRViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                MovieTitle = firstShowSeat?.Show?.Movie?.Title ?? "N/A",
                ShowStartTime = firstShowSeat?.Show?.StartTime ?? DateTime.MinValue,
                RoomName = firstShowSeat?.Show?.Room?.Name ?? "N/A",
                CinemaName = firstShowSeat?.Show?.Room?.Cinema?.Name ?? "N/A",
                TotalCost = booking.TotalCost,
                QrCodeBase64Image = $"data:image/png;base64,{base64Image}",
                BankName = bankName,
                BankAccountNumber = bankAccountNumber,
                BankAccountName = bankAccountName,
                PaymentContent = paymentContent,
                PaymentDeadline = booking.PaymentDeadline.Value // Đã kiểm tra HasValue ở trên
            };

            return View(viewModel); // Views/Booking/DisplayPaymentQR.cshtml
        }

        // Action để client AJAX call kiểm tra trạng thái booking
        [HttpGet]
        public async Task<IActionResult> CheckBookingStatus(int bookingId)
        {
            var booking = await _context.Bookings.Select(b => new { b.Id, b.Status })
                                               .FirstOrDefaultAsync(b => b.Id == bookingId);
            var userId = _userManager.GetUserId(User);
            // Thêm kiểm tra customerId nếu cần thiết để đảm bảo đúng người hỏi
            // var bookingOwner = await _context.Bookings.Where(b => b.Id == bookingId).Select(b => b.CustomerId).FirstOrDefaultAsync();
            // if (booking == null || bookingOwner != userId) return NotFound();

            if (booking == null)
            {
                return NotFound(new { status = "NotFound" });
            }
            return Json(new { status = booking.Status });
        }
        // GET: /Booking/BookingSuccess/10 (10 là bookingId)
        [HttpGet("Booking/BookingSuccess/{bookingId:int}")]
        public async Task<IActionResult> BookingSuccess(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Seat)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            var user = await _userManager.GetUserAsync(User);

            // Kiểm tra xem booking có tồn tại và thuộc về user hiện tại không
            if (booking == null || booking.CustomerId != user.Id)
            {
                TempData["Error"] = "Không tìm thấy thông tin đặt vé hoặc bạn không có quyền xem vé này.";
                return RedirectToAction("Index", "Home");
            }

            // Truyền booking vào view để hiển thị chi tiết vé
            return View(booking); // Trả về Views/Booking/BookingSuccess.cshtml
        }

        // GET: /Booking/BookingFailed
        [HttpGet]
        public IActionResult BookingFailed()
        {
            // View này chỉ hiển thị thông báo lỗi chung chung
            return View(); // Trả về Views/Booking/BookingFailed.cshtml
        }


        // GET: /Booking/MyBookings
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var bookings = await _context.Bookings
                .Where(b => b.CustomerId == user.Id)
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Seat) // Lấy thông tin ghế
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Movie) // Lấy thông tin phim
                .Include(b => b.ShowSeats).ThenInclude(ss => ss.Show).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema) // Lấy thông tin rạp, phòng
                .OrderByDescending(b => b.Purchased) // Sắp xếp theo ngày đặt gần nhất
                .ToListAsync();

            return View(bookings); // Trả về Views/Booking/MyBookings.cshtml
        }
    }
}