using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text;
namespace Engineer_MVC.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public DateTime date { get; set; } = DateTime.Now;
        [Required]
        public bool IsDeleted { get; set; } = false; //are you sure?
        [Required]
        public bool IsVisible { get; set; }
        [Required]
        [RegularExpression("^(Italiano|Polski)$", ErrorMessage = "Wybierz poprawny język.")]
        public string Language { get; set; }

        public string? ImagePath { get; set; }

    }
}
