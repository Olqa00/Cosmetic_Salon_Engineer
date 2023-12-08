namespace Engineer_MVC.Models.ViewModels
{
    public class TrainingsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public float Cost { get; set; }
        public string NumberUsers { get; set; }
        public string EmployeeBackgroundColor { get; set; }
        public string Employee { get; set; }
        public string EmployeeId { get; set; }
        public int TreatmentId { get; set; }
        
    }
}
