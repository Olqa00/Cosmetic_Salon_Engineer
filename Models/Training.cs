﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Engineer_MVC.Models
{
    public class Training
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public float Duration { get; set; }
        [Required]
        public float Price { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public int UsersNumber { get; set; } //number of participants
        public int TreatmentId { get; set; }
        public Treatment Treatment { get; set; } //one treatment
        public string? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public User? Employee { get; set; } //one Employee
        public ICollection<User>? Users { get; set; } //many Users
        public ICollection<RatingForTraining>? Ratings { get; set; }
        public ICollection<CancellationRequest>? Requests { get; set; }
        [NotMapped]
        public DateTime EndTime => Date.AddMinutes(Duration);
    }
}
