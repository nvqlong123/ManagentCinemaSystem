// File: Controllers/SeatController.cs

using ManagentCinemaSystem.Data;
using ManagentCinemaSystem.Models;
using ManagentCinemaSystem.ViewModels; // Đảm bảo namespace này đúng
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagentCinemaSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Seat/Manage/{roomId}
        [HttpGet("Seat/Manage/{roomId:int}")]
        public async Task<IActionResult> Manage(int roomId)
        {
            var room = await _context.Rooms
                                     .Include(r => r.Cinema) // Include Cinema để lấy CinemaId và CinemaName
                                     .Include(r => r.Seats)
                                     .ThenInclude(s => s.SeatType)
                                     .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
            {
                TempData["Error"] = $"Không tìm thấy phòng với ID {roomId} để quản lý ghế.";
                return RedirectToAction("Index", "Cinema"); // Chuyển về danh sách rạp
            }

            var seatTypes = await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync();

            var viewModel = new ManageSeatsViewModel
            {
                RoomId = room.Id,
                RoomName = room.Name,
                CinemaId = room.CinemaId, // <<<===== GÁN GIÁ TRỊ CHO CinemaId
                CinemaName = room.Cinema.Name,
                Seats = room.Seats.OrderBy(s => s.Row).ThenBy(s => s.Col).Select(s => new SeatViewModel
                {
                    Id = s.Id,
                    Row = s.Row,
                    Col = s.Col,
                    SeatTypeId = s.SeatTypeId,
                    RoomId = s.RoomId
                    // Có thể thêm SeatTypeName ở đây nếu cần hiển thị trong bảng thay vì dùng SelectList lookup
                    // SeatTypeName = s.SeatType.Name
                }).ToList(),
                AvailableSeatTypes = new SelectList(seatTypes, "Id", "Name")
            };

            return View(viewModel); // Sẽ tìm view trong Views/Seat/Manage.cshtml
        }

        // GET: /Seat/Create/{roomId}
        [HttpGet("Seat/Create/{roomId:int}")]
        public async Task<IActionResult> Create(int roomId)
        {
            var room = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null)
            {
                TempData["Error"] = $"Không tìm thấy phòng với ID {roomId} để thêm ghế.";
                // Có thể redirect về Manage của phòng trước đó nếu có thông tin, hoặc về Cinema Index
                return RedirectToAction("Index", "Cinema");
            }

            var model = new SeatViewModel
            {
                RoomId = roomId,
                RoomName = room.Name,
                CinemaName = room.Cinema.Name,
                AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name")
            };
            return View(model); // Views/Seat/Create.cshtml
        }

        // POST: /Seat/Create/{roomId}
        [HttpPost("Seat/Create/{roomId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int roomId, SeatViewModel model)
        {
            var roomExists = await _context.Rooms.AnyAsync(r => r.Id == roomId);
            if (!roomExists)
            {
                TempData["Error"] = $"Phòng với ID {roomId} không tồn tại.";
                return RedirectToAction("Index", "Cinema");
            }
            model.RoomId = roomId;

            if (ModelState.IsValid)
            {
                if (await _context.Seats.AnyAsync(s => s.RoomId == model.RoomId && s.Row == model.Row && s.Col == model.Col))
                {
                    ModelState.AddModelError(string.Empty, $"Ghế {model.Row}{model.Col} đã tồn tại trong phòng này.");
                }
                else
                {
                    var seat = new Seat
                    {
                        Row = model.Row,
                        Col = model.Col,
                        SeatTypeId = model.SeatTypeId,
                        RoomId = model.RoomId
                    };
                    _context.Seats.Add(seat);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã thêm ghế {seat.Row}{seat.Col} thành công.";
                    return RedirectToAction(nameof(Manage), new { roomId = model.RoomId });
                }
            }
            // Nếu ModelState không hợp lệ, load lại thông tin phòng và loại ghế
            var roomForView = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == roomId);
            model.RoomName = roomForView?.Name;
            model.CinemaName = roomForView?.Cinema?.Name;
            model.AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", model.SeatTypeId);
            return View(model);
        }

        // POST: /Seat/BatchCreate/{roomId}
        [HttpPost("Seat/BatchCreate/{roomId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchCreate(int roomId, ManageSeatsViewModel model)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                TempData["Error"] = $"Phòng với ID {roomId} không tồn tại.";
                return RedirectToAction("Index", "Cinema");
            }

            // ModelState.IsValid sẽ tự động kiểm tra các thuộc tính [Required] trong ViewModel
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin hợp lệ để tạo ghế hàng loạt.";
                // Cần load lại AvailableSeatTypes cho view Manage nếu trả về
                model.AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", model.BatchDefaultSeatTypeId);
                model.RoomId = roomId; // Đảm bảo roomId có trong model trả về
                model.RoomName = room.Name;
                var cinema = await _context.Cinemas.FindAsync(room.CinemaId);
                model.CinemaName = cinema?.Name;
                // Lấy lại danh sách ghế hiện tại để hiển thị
                model.Seats = await _context.Seats
                                   .Where(s => s.RoomId == roomId)
                                   .OrderBy(s => s.Row).ThenBy(s => s.Col)
                                   .Select(s => new SeatViewModel { Id = s.Id, Row = s.Row, Col = s.Col, SeatTypeId = s.SeatTypeId, RoomId = s.RoomId })
                                   .ToListAsync();

                return View("Manage", model); // Trả về view Manage với lỗi
            }


            if (model.BatchStartRow.Value > model.BatchEndRow.Value)
            {
                TempData["Error"] = "Hàng bắt đầu không được lớn hơn hàng kết thúc.";
                return RedirectToAction(nameof(Manage), new { roomId });
            }
            if (model.BatchStartCol.Value > model.BatchEndCol.Value)
            {
                TempData["Error"] = "Cột bắt đầu không được lớn hơn cột kết thúc.";
                return RedirectToAction(nameof(Manage), new { roomId });
            }

            char startRowUpper = Char.ToUpper(model.BatchStartRow.Value);
            char endRowUpper = Char.ToUpper(model.BatchEndRow.Value);

            // Không cần check A-Z nữa vì Regex đã làm trong ViewModel

            var seatTypeExists = await _context.SeatTypes.AnyAsync(st => st.Id == model.BatchDefaultSeatTypeId.Value);
            // Không cần check nữa vì ModelState.IsValid đã bao gồm Required

            List<Seat> newSeats = new List<Seat>();
            List<string> existingSeatsMessages = new List<string>();
            int seatsAddedCount = 0;

            for (char row = startRowUpper; row <= endRowUpper; row++)
            {
                for (int col = model.BatchStartCol.Value; col <= model.BatchEndCol.Value; col++)
                {
                    if (await _context.Seats.AnyAsync(s => s.RoomId == roomId && s.Row == row && s.Col == col))
                    {
                        existingSeatsMessages.Add($"{row}{col}"); // Ngắn gọn hơn
                        continue;
                    }
                    newSeats.Add(new Seat
                    {
                        Row = row,
                        Col = col,
                        SeatTypeId = model.BatchDefaultSeatTypeId.Value,
                        RoomId = roomId
                    });
                    seatsAddedCount++;
                }
            }

            if (newSeats.Any())
            {
                _context.Seats.AddRange(newSeats);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm thành công {seatsAddedCount} ghế mới.";
            }
            else
            {
                TempData["Info"] = "Không có ghế mới nào được thêm.";
            }

            if (existingSeatsMessages.Any())
            {
                TempData["Warning"] = $"Các ghế sau đã tồn tại và bị bỏ qua: {string.Join(", ", existingSeatsMessages)}.";
            }

            return RedirectToAction(nameof(Manage), new { roomId });
        }


        // GET: /Seat/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var seat = await _context.Seats
                                     .Include(s => s.Room)
                                     .ThenInclude(r => r.Cinema)
                                     .FirstOrDefaultAsync(s => s.Id == id);
            if (seat == null)
            {
                TempData["Error"] = $"Không tìm thấy ghế với ID {id}.";
                return RedirectToAction("Index", "Cinema");
            }

            var model = new SeatViewModel
            {
                Id = seat.Id,
                Row = seat.Row,
                Col = seat.Col,
                SeatTypeId = seat.SeatTypeId,
                RoomId = seat.RoomId,
                RoomName = seat.Room.Name,
                CinemaName = seat.Room.Cinema.Name,
                AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", seat.SeatTypeId)
            };
            return View(model);
        }

        // POST: /Seat/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SeatViewModel model) // model chứa Id ghế
        {
            // Không cần tìm seatToUpdate ở đây nếu ModelState không hợp lệ
            if (!ModelState.IsValid)
            {
                // Load lại thông tin cần thiết cho View nếu validation fail
                var roomForView = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == model.RoomId); // Cần RoomId trong model
                if (roomForView != null)
                {
                    model.RoomName = roomForView.Name;
                    model.CinemaName = roomForView.Cinema.Name;
                }
                model.AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", model.SeatTypeId);
                return View(model);
            }

            var seatToUpdate = await _context.Seats.FindAsync(model.Id);
            if (seatToUpdate == null)
            {
                TempData["Error"] = $"Không tìm thấy ghế với ID {model.Id} để cập nhật.";
                // Không biết roomId ở đây, nên redirect về Cinema Index
                return RedirectToAction("Index", "Cinema");
            }

            // Kiểm tra xung đột vị trí
            if (await _context.Seats.AnyAsync(s => s.RoomId == seatToUpdate.RoomId && s.Row == model.Row && s.Col == model.Col && s.Id != model.Id))
            {
                ModelState.AddModelError(string.Empty, $"Ghế {model.Row}{model.Col} đã tồn tại trong phòng này.");
                // Load lại thông tin cho view
                var roomForView = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == seatToUpdate.RoomId);
                model.RoomId = seatToUpdate.RoomId; // Gán lại RoomId
                if (roomForView != null)
                {
                    model.RoomName = roomForView.Name;
                    model.CinemaName = roomForView.Cinema.Name;
                }
                model.AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", model.SeatTypeId);
                return View(model);
            }

            seatToUpdate.Row = model.Row;
            seatToUpdate.Col = model.Col;
            seatToUpdate.SeatTypeId = model.SeatTypeId;

            try
            {
                _context.Update(seatToUpdate);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật ghế {seatToUpdate.Row}{seatToUpdate.Col} thành công.";
                return RedirectToAction(nameof(Manage), new { roomId = seatToUpdate.RoomId });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật. Ghế có thể đã bị thay đổi bởi người khác.");
                // Load lại thông tin cho view
                var roomForView = await _context.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == seatToUpdate.RoomId);
                model.RoomId = seatToUpdate.RoomId; // Gán lại RoomId
                if (roomForView != null)
                {
                    model.RoomName = roomForView.Name;
                    model.CinemaName = roomForView.Cinema.Name;
                }
                model.AvailableSeatTypes = new SelectList(await _context.SeatTypes.OrderBy(st => st.Name).ToListAsync(), "Id", "Name", model.SeatTypeId);
                return View(model);
            }
        }

        // POST: /Seat/Delete/{id}
        [HttpPost("Seat/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
            {
                TempData["Error"] = "Không tìm thấy ghế để xóa.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return NotFound("Seat not found.");
                return RedirectToAction("Index", "Cinema"); // Redirect chung nếu không biết roomId
            }

            int roomId = seat.RoomId; // Lưu lại để redirect

            var isSeatReferencedInShowSeats = await _context.ShowSeats.AnyAsync(ss => ss.SeatId == id);
            if (isSeatReferencedInShowSeats)
            {
                string errorMsg = $"Không thể xóa ghế {seat.Row}{seat.Col} vì nó đã được lên lịch trong một hoặc nhiều suất chiếu. Các suất chiếu liên quan cần được xóa hoặc chỉnh sửa trước.";
                TempData["Error"] = errorMsg;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { message = errorMsg }); // Trả về lỗi cho AJAX
                return RedirectToAction(nameof(Manage), new { roomId });
            }

            try
            {
                _context.Seats.Remove(seat);
                await _context.SaveChangesAsync();
                string successMsg = $"Đã xóa ghế {seat.Row}{seat.Col} thành công.";
                TempData["Success"] = successMsg;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { message = successMsg }); // Trả về thành công cho AJAX
            }
            catch (DbUpdateException ex)
            {
                string errorMsg = $"Không thể xóa ghế {seat.Row}{seat.Col} do ràng buộc dữ liệu. Lỗi: {ex.InnerException?.Message ?? ex.Message}";
                TempData["Error"] = errorMsg;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { message = errorMsg });
            }
            return RedirectToAction(nameof(Manage), new { roomId });
        }
    }
}