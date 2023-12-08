using Microsoft.AspNetCore.Components.Forms;
using Engineer_MVC.Data.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;

namespace Engineer_MVC.Data.Services
{
    public sealed class UserService : IUserService
    {
        private readonly EngineerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public UserService(EngineerContext context, IWebHostEnvironment hostEnvironment, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public void DeleteImage(string imagePath)
        {
            string filePath = GetFullPath(imagePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        private string GetFullPath(string imagePath)
        {
            return Path.Combine(_hostEnvironment.WebRootPath, "ProfileImages", imagePath);
        }
        public async Task<string> UploadImage(IFormFile file)
        {
            long totalBytes = file.Length;
            string filename = file.FileName.Trim('"');
            filename = EnsureFileName(filename);
            byte[] buffer = new byte[16 * 1024];
            using (FileStream output = System.IO.File.Create(GetpathAndFileName(filename)))
            {
                using (Stream input = file.OpenReadStream())
                {
                    int readBytes;
                    while ((readBytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, readBytes);
                        totalBytes += readBytes;
                    }
                }
            }
            return filename;
        }
        private string GetpathAndFileName(string filename)
        {
            string path = _hostEnvironment.WebRootPath + "\\ProfileImages\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path + filename;
        }
        private string EnsureFileName(string filename)
        {
            if (filename.Contains("\\"))
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);
            return filename;
        }

        public async Task<List<Treatment>> GetUserTreatments(User user)
        {
            return await _context.Treatment
                .Where(t => t.Employees.Any(e => e.Id == user.Id))
                .ToListAsync();
        }
        public string GetUserImagePath(ClaimsPrincipal user)
        {
            var userId = _userManager.GetUserId(user);
            var userEntity = _context.Users.Find(userId);
            return userEntity?.ImagePath;
            
        }
        public List<string> GetEmployeeAppointmentTypes(string employeeId)
        {
            var treatmentTypes = _context.Treatment
                .Where(t => t.Employees.Any(e => e.Id == employeeId))
                .Select(t => t.Type)
                .Distinct()
                .ToList();

            return treatmentTypes;
        }

        public List<string> GetEmployeeAppointmentNames(string employeeId)
        {
            var treatmentNames = _context.Treatment
                .Where(t => t.Employees.Any(e => e.Id == employeeId))
                .Select(t => t.Name)
                .Distinct()
                .ToList();

            return treatmentNames;
        }
    }
}
