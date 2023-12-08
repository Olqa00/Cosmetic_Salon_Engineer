namespace Engineer_MVC.Models
{
    public class CancellationRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public int TrainingId { get; set; }
        public bool? IsCanceled { get; set; } 
    }
}
