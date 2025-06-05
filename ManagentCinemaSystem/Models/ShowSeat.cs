using System.ComponentModel.DataAnnotations;

namespace ManagentCinemaSystem.Models
{
    public class ShowSeat
    {
        [Key]
        public int Id { get; set; }

        public bool IsBooked { get; set; }

        public int ShowId { get; set; }
        public virtual Show Show { get; set; }

        public int SeatId { get; set; }
        public virtual Seat Seat { get; set; }
        public int? BookingId { get; set; }
        public virtual Booking Booking { get; set; }
    }
}
