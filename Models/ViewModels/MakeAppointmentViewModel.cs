namespace Engineer_MVC.Models
{
    public class MakeAppointmentViewModel
    {
        public User Employee { get; set; }
        public Treatment Treatment { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string EmployeeId { get; set; }
        public int TreatmentId { get; set; }
        public string UserId { get; set; }
    }
}
