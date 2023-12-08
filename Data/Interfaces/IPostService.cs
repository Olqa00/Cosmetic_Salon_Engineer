
namespace Engineer_MVC.Data.Interfaces
{
    public interface IPostService
    {
        Task<string> UploadImage(IFormFile file);
        void DeleteImage(string imagePath);
    }
}
