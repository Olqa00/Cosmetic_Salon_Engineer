using Engineer_MVC.Models;
using System.Security.Claims;

namespace Engineer_MVC.Data.Interfaces
{
    public interface IUserService
    {
        public Task<string> UploadImage(IFormFile file);
        void DeleteImage(string imagePath);
        Task<List<Treatment>> GetUserTreatments(User user);
        string GetUserImagePath(ClaimsPrincipal user);
        List<string> GetEmployeeAppointmentTypes(string employeeId);
        List<string> GetEmployeeAppointmentNames(string employeeId);
    }
}
