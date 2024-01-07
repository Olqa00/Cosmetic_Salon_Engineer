namespace Engineer_MVC.Models.Templates
{
    public class AppointmentEmailModel
    {
        public string Employee { get; set; }
        public string Treatment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public float Price { get; set; }
    }
}
