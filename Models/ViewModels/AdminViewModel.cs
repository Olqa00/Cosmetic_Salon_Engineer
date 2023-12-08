namespace Engineer_MVC.Models.ViewModels
{
    public class AdminViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? BackgroundColor { get; set; }
        public string? ImagePath { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<string> TreatmentNames { get; set; }
    }
}
