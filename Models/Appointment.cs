using System.ComponentModel.DataAnnotations;

namespace Engineer_MVC.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public float Duration { get; set; }
        [Required]
        public float Price { get; set; } 
        [Range(1, 5)]
        public int? Rating { get; set; }
        [Required]
        public string Status { get; set; } 
        [Required]
        public bool IsLimited { get; set; }

        public bool? IsCanceled { get; set; }
        public int TreatmentId { get; set; }
        public Treatment Treatment { get; set; }
        public string? EmployeeId { get; set; }
        public User? Employee { get; set; }
        public string? UserId { get; set; }
        public User? User { get; set; }

        public DateTime EndTime => Date.AddMinutes(Duration);
    }
}
