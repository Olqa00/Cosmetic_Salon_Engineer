using Engineer_MVC.Data;
using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Engineer_MVC.Controllers
{
    
    public class EmployeesController : CustomBaseController
    {
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        public EmployeesController(EngineerContext context,
            UserManager<User> userManager,
            IStringLocalizer<SharedResource> sharedResource,
            IAppointmentService appointmentService)
        {
            _context = context;
            _userManager = userManager;
            _sharedResource = sharedResource;
            _appointmentService = appointmentService;
        }
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> SetStars(int treatmentId, int starsCount)
        {

            var appointment = await _context.Appointment.FindAsync(treatmentId);
            appointment.User = await _userManager.FindByIdAsync(appointment.UserId);
            appointment.Treatment = await _context.Treatment.FindAsync(appointment.TreatmentId);
            appointment.Employee = await _userManager.FindByIdAsync(appointment.EmployeeId);
            if (appointment == null)
            {
                return View();
            }

            // Update the stars count for the treatment
            appointment.Rating = starsCount;
            _context.Appointment.Update(appointment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                treatmentId = treatmentId,
                starsCount = starsCount
            });

        }
        public async Task<IActionResult> UpdateSelects(string selectedTreatmentType)
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var treatmentTypesForEmployees = loggedInUserWithTreatments.Treatments.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployees = loggedInUserWithTreatments.Treatments.Select(t => t.Name).ToList();
            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                treatmentNamesForEmployees = treatmentNamesForEmployees
                    .Where(name => _context.Treatment.Any(t => t.Name == name && t.Type == selectedTreatmentType))
                    .Distinct()
                    .ToList();
                ViewBag.Types = treatmentTypesForEmployees;
                ViewBag.Names = treatmentNamesForEmployees;
            }
            return Json(new
            {
                TreatmentTypes = treatmentTypesForEmployees,
                TreatmentNames = treatmentNamesForEmployees,
            });
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ManageAppointments()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.EmployeeId == loggedInUser.Id).Where(t => t.Status == "To Do").OrderBy(t => t.Date).ToList();
            _appointmentService.UpdateAppointmentsWithUserData(TreatmentsList);

            var usersDictionary = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
            ViewBag.Users = usersDictionary;


            var treatmentTypesForEmployees = loggedInUserWithTreatments.Treatments.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployees = loggedInUserWithTreatments.Treatments.Select(t => t.Name).ToList();

            ViewBag.Types = treatmentTypesForEmployees;
            ViewBag.Names = treatmentNamesForEmployees;
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(TreatmentsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ManageAppointments([FromForm] Appointment appointment)
        {
            var user = await _userManager.FindByIdAsync(appointment.UserId);
            if (user != null)
            {
                appointment.User = user;
                appointment.Treatment = _context.Treatment.FirstOrDefault(t => t.Name == appointment.Treatment.Name && t.Type == appointment.Treatment.Type);
                appointment.TreatmentId = appointment.Treatment.Id;
                appointment.Employee = await _userManager.GetUserAsync(User);
                appointment.EmployeeId = appointment.Employee.Id;

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(appointment, new ValidationContext(appointment), validationResults, true))
                {
                    foreach (var validationResult in validationResults)
                    {
                        ModelState.AddModelError("", validationResult.ErrorMessage);
                    }
                }
                else
                {
                    

                    appointment.Status = "Done";
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            {
                // Jeśli użytkownik nie istnieje, dodaj komunikat o błędzie dla UserId
                ModelState.AddModelError("UserId", "Nieprawidłowy użytkownik.");
            }


            // If the model state is not valid, repopulate ViewBag data and return to the edit view
            ViewBag.Types = await _context.Treatment.Select(t => t.Type).Distinct().ToListAsync();
            ViewBag.Names = await _context.Treatment.Select(t => t.Name).Distinct().ToListAsync();
            ViewBag.Users = await _context.Users.Select(u => u.FullName).ToListAsync();


            return RedirectToAction("ManageAppointments");
        }
        public async Task<IActionResult> UpdateSelectsTraining(string selectedTreatmentType, string employeeId, int trainingId)
        {
            var user = await _userManager.FindByIdAsync(employeeId);
            var userWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            var treatmentTypesForEmployees = userWithTreatments.Treatments.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployees = userWithTreatments.Treatments.Select(t => t.Name).ToList();
            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                treatmentNamesForEmployees = treatmentNamesForEmployees
                    .Where(name => _context.Treatment.Any(t => t.Name == name && t.Type == selectedTreatmentType))
                    .Distinct()
                    .ToList();
                ViewBag.Types = treatmentTypesForEmployees;
                ViewBag.Names = treatmentNamesForEmployees;
            }
            var training = _context.Training.Include(t => t.Users).FirstOrDefault(t => t.Id == trainingId);
            var UserCounts = training.Users?.Count() ?? 0;
            ViewBag.UsersCount = UserCounts;
            return Json(new
            {
                TreatmentTypes = treatmentTypesForEmployees,
                TreatmentNames = treatmentNamesForEmployees,
                UsersCount = UserCounts
            });
        }
        public async Task<IActionResult> ManageTrainings()
        {
            var usersDictionary = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
            ViewBag.Users = usersDictionary;
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var trainingsList = _context.Training.Include(t => t.Users).Include(t => t.Employee).Include(t => t.Treatment).Where(t => t.Status == "To Do" && t.EmployeeId== loggedInUser.Id).OrderBy(t => t.Date).ToList();
            var treatmentTypes = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNames = _context.Treatment.Select(t => t.Name).ToList();

            ViewBag.Types = treatmentTypes;
            ViewBag.Names = treatmentNames;
            ViewBag.UsersCount = 0;
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(trainingsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageTrainings([FromForm] Training training)
        {
            var employee = await _userManager.FindByIdAsync(training.EmployeeId);
            var users = _context.Users.Include(t => t.Treatments)
                .Include(u => u.TrainingsReceive)
                .ToList();
            var selectedUsers = users
            .Where(u => u.TrainingsReceive.Any(tr => tr.Id == training.Id))
            .ToList();

            if (employee != null)
            {
                training.Employee = employee;
                training.Treatment = _context.Treatment.FirstOrDefault(t => t.Name == training.Treatment.Name && t.Type == training.Treatment.Type);
                training.TreatmentId = training.Treatment.Id;

                _context.ChangeTracker.Clear();
                training.Users.Clear();
                training.Users = selectedUsers;
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(training, new ValidationContext(training), validationResults, true))
                {
                    foreach (var validationResult in validationResults)
                    {
                        ModelState.AddModelError("", validationResult.ErrorMessage);
                    }
                }
                else
                {
                    var local = _context.Set<Training>()
                    .Local
                    .FirstOrDefault(entry => entry.Id.Equals(training.Id));

                    // check if local is not null 
                    if (local != null)
                    {
                        // detach
                        _context.Entry(local).State = EntityState.Detached;
                    }
                    // set Modified flag in your entry
                    _context.Entry(training).State = EntityState.Modified;



                    training.Status = "Done";
                    _context.Update(training);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ManageTrainings");
                }

            }
            {
                ModelState.AddModelError("UserId", "Nieprawidłowy użytkownik.");
            }

            ViewBag.Types = await _context.Treatment.Select(t => t.Type).Distinct().ToListAsync();
            ViewBag.Names = await _context.Treatment.Select(t => t.Name).Distinct().ToListAsync();
            ViewBag.UsersCount = 0;
            return RedirectToAction("ManageTrainings");
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ArchiveTrainings()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var trainingsList = _context.Training.Include(t => t.Users).Include(t => t.Employee).Include(t => t.Treatment).Where(t => t.Status == "Done" && t.EmployeeId==loggedInUser.Id).OrderByDescending(t => t.Date).ToList();
            return View(trainingsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ArchiveTrainings([FromForm] Training training)
        {
            var users = _context.Users.Include(t => t.Treatments)
                .Include(u => u.TrainingsReceive)
                .ToList();
            var selectedUsers = users
            .Where(u => u.TrainingsReceive.Any(tr => tr.Id == training.Id))
            .ToList();

            training = _context.Training.FirstOrDefault(a => a.Id == training.Id);
            training.Users = selectedUsers;
            training.Employee = await _userManager.FindByIdAsync(training.EmployeeId);
            training.Treatment = _context.Treatment.FirstOrDefault(a => a.Id == training.TreatmentId);

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(training, new ValidationContext(training), validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    ModelState.AddModelError("", validationResult.ErrorMessage);
                }
            }
            else
            {
                training.Status = "To Do";
                _context.Update(training);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageTrainings");
            }


            return RedirectToAction("ManageTrainings");
        }
        public async Task<IActionResult> LoadTrainingsArchive(int skip)
        {
            try
            {
                var loggedInUser = await _userManager.GetUserAsync(User);

                var trainingsList = _context.Training.Include(t => t.Users)
                                        .Include(t => t.Employee)
                                        .Include(t => t.Treatment)
                                        .Include(t=>t.Ratings)
                                        .Where(t => t.Status == "Done" 
                                        && t.EmployeeId==loggedInUser.Id)
                                        .OrderByDescending(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_TrainingsPartial", trainingsList);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> ArchiveAppointments()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.EmployeeId == loggedInUser.Id).Where(t => t.Status == "Done").OrderByDescending(t => t.Date).ToList();
            _appointmentService.UpdateAppointmentsWithUserData(TreatmentsList);
            return View(TreatmentsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveAppointments([FromForm] Appointment appointment)
        {
            appointment = _context.Appointment.FirstOrDefault(a => a.Id == appointment.Id);
            appointment.User = await _userManager.FindByIdAsync(appointment.UserId);
            appointment.Employee = await _userManager.FindByIdAsync(appointment.EmployeeId);
            appointment.Treatment = _context.Treatment.FirstOrDefault(a => a.Id == appointment.TreatmentId);

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(appointment, new ValidationContext(appointment), validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    ModelState.AddModelError("", validationResult.ErrorMessage);
                }
            }
            else
            {
                appointment.Status = "To Do";
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }


            return RedirectToAction("ManageAppointments");
        }
        private bool AppointmentExists(int id)
        {
            return _context.Appointment.Any(e => e.Id == id);
        }
        public async Task<IActionResult> PullEarniningsStats()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var userAppointments = _context.Appointment.Include(t => t.Employee).Where(u => u.EmployeeId == loggedInUser.Id).Where(t => t.Status == "Done").ToList();
            var weeklyData = userAppointments
                .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                .Select(group => new
                {
                    Week = group.Key,
                    Earnings = group.Sum(appointment => appointment.Price)
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = userAppointments
                .GroupBy(appointment => new { appointment.Date.Year, appointment.Date.Month })
                .Select(group => new
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    Earnings = group.Sum(appointment => appointment.Price)
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToList();
            var yearlyData = userAppointments
                .GroupBy(appointment => appointment.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    Earnings = group.Sum(appointment => appointment.Price)
                })
                .OrderBy(group => group.Year)
                .ToList();
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;

            string weekString = "";
            if (currentCulture == "it-IT")
            {
                weekString = "Settimana ";
            }
            else if (currentCulture == "pl-PL")
            {
                weekString = "Tydzień ";
            }
            var data = new
            {
                WeeklyData = weeklyData.Select(item => item.Earnings).ToList(),
                WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                MonthlyData = monthlyData.Select(item => item.Earnings).ToList(),
                MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                YearlyData = yearlyData.Select(item => item.Earnings).ToList(),
                YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
            };
            return Json(data);
        }
        public async Task<IActionResult> PullTimeStats()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var userAppointments = _context.Appointment.Include(t => t.Employee).Where(u => u.EmployeeId == loggedInUser.Id).Where(t => t.Status == "Done").ToList();

            var weeklyData = userAppointments
                .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                .Select(group => new
                {
                    Week = group.Key,
                    Time = group.Sum(appointment => appointment.Duration)
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = userAppointments
                .GroupBy(appointment => new { appointment.Date.Year, appointment.Date.Month })
                .Select(group => new
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    Time = group.Sum(appointment => appointment.Duration)
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToList();
            var yearlyData = userAppointments
                .GroupBy(appointment => appointment.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    Time = group.Sum(appointment => appointment.Duration)
                })
                .OrderBy(group => group.Year)
                .ToList();
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            string weekString = "";
            if (currentCulture == "it-IT")
            {
                weekString = "Settimana ";
            }
            else if (currentCulture == "pl-PL")
            {
                weekString = "Tydzień ";
            }
            var data = new
            {
                WeeklyData = weeklyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                MonthlyData = monthlyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                YearlyData = yearlyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
            };

            return Json(data);
        }
        public async Task<IActionResult> PullQuantityStats()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var userAppointments = _context.Appointment.Include(t => t.Employee).Where(u => u.EmployeeId == loggedInUser.Id).Where(t => t.Status == "Done").ToList();

            var weeklyData = userAppointments
                .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                .Select(group => new
                {
                    Week = group.Key,
                    Quantity = group.Count()
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = userAppointments
                .GroupBy(appointment => new { appointment.Date.Year, appointment.Date.Month })
                .Select(group => new
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    Quantity = group.Count()
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToList();
            var yearlyData = userAppointments
                .GroupBy(appointment => appointment.Date.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    Quantity = group.Count()
                })
                .OrderBy(group => group.Year)
                .ToList();

            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            string weekString = "";
            if (currentCulture == "it-IT")
            {
                weekString = "Settimana ";
            }
            else if (currentCulture == "pl-PL")
            {
                weekString = "Tydzień ";
            }

            var data = new
            {
                WeeklyData = weeklyData.Select(item => item.Quantity).ToList(),
                WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                MonthlyData = monthlyData.Select(item => item.Quantity).ToList(),
                MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                YearlyData = yearlyData.Select(item => item.Quantity).ToList(),
                YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
            };

            return Json(data);
        }

        public async Task<IActionResult> PullAverageScore()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);

            var currentDate = DateTime.Now;
            var currentMonth = currentDate.Month;
            var currentYear = currentDate.Year;

            var userAppointments = _context.Appointment.Include(t => t.Employee)
                .Where(u => u.EmployeeId == loggedInUser.Id)
                .Where(t => t.Status == "Done")
                .Where(d => d.Date.Month == currentMonth && d.Date.Year == currentYear)
                .ToList();
            var totalRating = userAppointments.Sum(appointment => appointment.Rating);
            var averageRating = (double)totalRating / userAppointments.Count;
            averageRating = Math.Round(averageRating, 1);
            var data = new
            {
                AverageRating = averageRating
            };
            return Json(data);
        }
        public async Task<IActionResult> PullMostTreatments()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            var loggedInUserWithTreatments = await _context.Users
                .Include(u => u.Treatments)
                .FirstOrDefaultAsync(u => u.Id == loggedInUser.Id);
            var userAppointments = _context.Appointment.Include(t => t.Employee)
                .Where(u => u.EmployeeId == loggedInUser.Id)
                .Where(t => t.Status == "Done")
                .GroupBy(g => new { g.Treatment.Type, g.Treatment.Name }) 
                .Select(g => new
                {
                    TreatmentType = g.Key.Type,
                    TreatmentName = g.Key.Name,
                    Count = g.Count() 
                })
                .OrderByDescending(g => g.Count) 
                .Take(5) 
                .ToList();
            return Json(userAppointments);
        }
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Stats()
        {
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View();
        }
    }
}
