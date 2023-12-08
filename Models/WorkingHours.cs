using System.ComponentModel.DataAnnotations;

namespace Engineer_MVC.Models
{
    public class WorkingHours
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }
        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }
        public string EmployeeId { get; set; } 
        public User User { get; set; }
    }
}
