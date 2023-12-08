using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Engineer_MVC.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? ImagePath { get; set; }
        public string? BackgroundColor { get; set; } 

        public ICollection<Treatment>? Treatments { get; set; }
        public ICollection<Appointment>? AppointmentsMade { get; set; } //for Employee
        public ICollection<Appointment>? AppointmentsReceive { get; set; } //for Customer
        public ICollection<WorkingHours>? WorkingHours { get; set; }
        [InverseProperty("Employee")]
        public ICollection<Training>? TrainingsMade { get; set; } //For Leader
        public ICollection<Training>? TrainingsReceive { get; set; }//For User/Employee
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
