using Engineer_MVC.Data.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace Engineer_MVC.Data.Services
{
    public sealed class PostService : IPostService
    {
        private readonly EngineerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        public PostService(EngineerContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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
            return Path.Combine(_hostEnvironment.WebRootPath, "PostImages", imagePath);
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
            string path = _hostEnvironment.WebRootPath + "\\PostImages\\";
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
    }
}
