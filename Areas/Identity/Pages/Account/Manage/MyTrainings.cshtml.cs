using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Engineer_MVC.Areas.Identity.Pages.Account.Manage
{
    public class MyTrainingsModel : PageModel
    {
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        public MyTrainingsModel(EngineerContext context,
            IStringLocalizer<SharedResource> sharedResource,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            _sharedResource = sharedResource;
        }
        public List<Training> TrainingsToDoNullList { get; set; }
        public List<Training> TrainingsDoneNullList { get; set; }
        public List<Training> TrainingsDoneList { get; set; }
        public List<Training> Trainings { get; set; }
        public string LoggedInUserId { get; set; }
        public async Task<IActionResult> OnGet()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            if (loggedInUser != null)
                LoggedInUserId = loggedInUser.Id;
            TrainingsToDoNullList = _context.Training.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.Requests).Include(t => t.Ratings).Include(t => t.Users).Where(t => t.Status == "To Do").Where(t => t.Users.Contains(loggedInUser)).OrderByDescending(t => t.Date).ToList();
            TrainingsDoneNullList = _context.Training
                .Include(t => t.Treatment)
                .Include(t => t.Users)
                .Include(t=>t.Employee)
                .Where(t => t.Status == "Done")
                .Where(t => (t.Users.Contains(loggedInUser) || t.EmployeeId == loggedInUser.Id) && !t.Ratings.Any(r => r.UserId == loggedInUser.Id))
                .OrderByDescending(t => t.Date)
                .ToList();
            
            TrainingsDoneList = _context.Training
                .Include(t => t.Treatment)
                .Include(t => t.Users)
                .Include(t => t.Employee)
                .Where(t => t.Status == "Done")
                .Where(t => (t.Users.Contains(loggedInUser) || t.EmployeeId == loggedInUser.Id) && t.Ratings.Any(r => r.UserId == loggedInUser.Id))
                .OrderByDescending(t => t.Date)
                .ToList();
            Trainings = _context.Training.Include(t => t.Treatment).Include(t => t.Users).Where(t => t.Status == "Done" && t.Ratings != null).Where(t => t.Users.Contains(loggedInUser) || t.EmployeeId == loggedInUser.Id).OrderByDescending(t => t.Date).ToList();
            return Page();
        }
    }
}
