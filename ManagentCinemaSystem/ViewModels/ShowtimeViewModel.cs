namespace ManagentCinemaSystem.ViewModels.Showtime
{
    public class ShowtimeViewModel
    {
        public int Id { get; set; }
        public string MovieTitle { get; set; }
        public DateTime StartTime { get; set; }
        public string CinemaName { get; set; }
        public string RoomName { get; set; }
    }
}