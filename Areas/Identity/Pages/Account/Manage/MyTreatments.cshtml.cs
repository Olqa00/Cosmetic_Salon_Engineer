using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Engineer_MVC.Areas.Identity.Pages.Account.Manage
{
    public class MyTreatmentsModel : PageModel
    {
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        public MyTreatmentsModel(EngineerContext context,
            IStringLocalizer<SharedResource> sharedResource,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            _sharedResource = sharedResource;
        }
        public List<Appointment> AppointmentsToDoNullList { get; set; }
        public List<Appointment> AppointmentsDoneNullList { get; set; }
        public List<Appointment> AppointmentsDoneList { get; set; }
        public List<Appointment> Appointments { get; set; }
        public string LoggedInUserId { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            if (loggedInUser != null)
                LoggedInUserId = loggedInUser.Id;
            AppointmentsToDoNullList = _context.Appointment.Include(t=>t.Treatment).Include(t => t.Employee).Where(t=>t.UserId == loggedInUser.Id && t.Status=="To Do" && t.Rating==null).OrderByDescending(t => t.Date).ToList();
            AppointmentsDoneNullList= _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == loggedInUser.Id && t.Status == "Done" && t.Rating == null).OrderByDescending(t => t.Date).ToList();
            AppointmentsDoneList= _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == loggedInUser.Id && t.Status == "Done" && t.Rating != null).OrderByDescending(t => t.Date).ToList();
            Appointments = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == loggedInUser.Id && t.Status == "Done" && t.Rating != null).OrderByDescending(t => t.Date).ToList();
            return Page();
        }
        

    }
}
