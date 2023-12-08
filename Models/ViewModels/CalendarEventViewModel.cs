namespace Engineer_MVC.Models.ViewModels
{
    public class CalendarEventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public float Cost { get; set; }
        public string Employee { get; set; }
        public string EmployeeId { get; set; }
        public int TreatmentId { get; set; }
        public string NumberUsers { get; set; }
        public string User { get; set; }
        public string Color { get; set; }
    }
}
