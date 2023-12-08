using System.ComponentModel.DataAnnotations;

namespace Engineer_MVC.Models
{
    public class Treatment
    {
        [Key]
        public int Id { get; set; } 
        [Required]  
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public string Type { get; set; }
        
        public float? AverageTime { get; set; }
        public float? AverageCost { get; set; }
        public ICollection<User>? Employees { get; set; } 
        public ICollection<Appointment>? Appointments { get; set; }
        public ICollection<Training>? Trainings { get; set; }
        public string TypeAndName => $"{Type} {Name}";
    }
}
