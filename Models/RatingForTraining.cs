using System.ComponentModel.DataAnnotations;

namespace Engineer_MVC.Models
{
    public class RatingForTraining
    {
        [Key]
        public int Id { get; set; }

        public int TrainingId { get; set; }
        public Training Training { get; set; } 

        public string UserId { get; set; } 
        [Range(1, 5)]
        public int Rating { get; set; }
        
    }
}
