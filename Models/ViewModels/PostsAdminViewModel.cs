using Microsoft.AspNetCore.Mvc.Rendering;

namespace Engineer_MVC.Models.ViewModels
{
    public class PostsAdminViewModel
    {
        public List<Post> Posts { get; set; }
        public string SelectedLanguage { get; set; }
        public SelectList LanguageOptions { get; set; }
    }
}
