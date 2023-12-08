namespace Engineer_MVC.Models.ViewModels
{
    public class GroupedCalendarEventViewModel
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int TreatmentId { get; set; }
        public List<CalendarEventViewModel> GroupedEvents { get; set; }
    }
}
