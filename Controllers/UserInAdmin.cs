using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace Engineer_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserInAdmin : CustomBaseController
    {
        private readonly UserManager<User> _userManager;
        public readonly EngineerContext _context;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserInAdmin(UserManager<User> userManager,
            EngineerContext context,
            IUserService userService,
            IStringLocalizer<SharedResource> sharedResource,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _context = context;
            _userService = userService;
            _sharedResource = sharedResource;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var Username = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            return View(user);
        }
        public async Task<IActionResult> PullEarniningsStats(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();
            var userAppointments = _context.Appointment.Include(t => t.User).Where(u => u.UserId == user.Id).Where(t => t.Status == "Done").ToList();
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
        public async Task<IActionResult> PullTimeStats(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();
            var userAppointments = _context.Appointment.Include(t => t.User).Where(u => u.UserId == user.Id).Where(t => t.Status == "Done").ToList();

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
        public async Task<IActionResult> PullQuantityStats(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();
            var userAppointments = _context.Appointment.Include(t => t.User).Where(u => u.UserId == user.Id).Where(t => t.Status == "Done").ToList();

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

        public async Task<IActionResult> PullAverageScore(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();

            var currentDate = DateTime.Now;
            var currentMonth = currentDate.Month;
            var currentYear = currentDate.Year;

            var userAppointments = _context.Appointment.Include(t => t.User)
                .Where(u => u.UserId == user.Id)
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
        public async Task<IActionResult> PullMostTreatments(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();
            var userAppointments = _context.Appointment.Include(t => t.User)
                .Where(u => u.UserId == user.Id)
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
        public async Task<IActionResult> Stats(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(user);
        }
        public async Task<IActionResult> GetAppointmentsForCalendar(string selectedTreatmentType, string selectedTreatmentName, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var appointments = _context.Appointment.Include(t => t.Treatment).Include(u => u.User).Include(e => e.Employee).Where(w => w.EmployeeId == user.Id || w.UserId == user.Id).ToList();
            var treatmentTypesForEmployee = _userService.GetEmployeeAppointmentTypes(user.Id);
            var treatmentNamesForEmployee = _userService.GetEmployeeAppointmentNames(user.Id);
            var trainings = _context.Training.Include(t => t.Treatment).Include(u => u.Users).Where(w => w.Users.Contains(user) || w.EmployeeId == user.Id).ToList();
            foreach (var appointment in appointments)
            {
                var treatment = _context.Treatment.Find(appointment.TreatmentId);
                var employee = _context.Users.Find(appointment.EmployeeId);
                var userAppointment = _context.Users.Find(appointment.UserId);

                appointment.Treatment = treatment;
                appointment.Employee = employee;
                appointment.User = userAppointment;
            }
            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                appointments = appointments.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
                trainings = trainings.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
                treatmentNamesForEmployee = treatmentNamesForEmployee
                    .Where(name => _context.Treatment.Any(t => t.Name == name && t.Type == selectedTreatmentType))
                    .Distinct()
                    .ToList();
                ViewData["TreatmentTypes"] = new SelectList(treatmentTypesForEmployee, "Id", "Type", selectedTreatmentType);
            }
            else
            {
                ViewData["TreatmentTypes"] = treatmentTypesForEmployee;
            }

            if (selectedTreatmentName != "all" && selectedTreatmentName != null)
            {
                appointments = appointments.Where(a => a.Treatment.Name == selectedTreatmentName).ToList();
                trainings = trainings.Where(a => a.Treatment.Name == selectedTreatmentName).ToList();
                treatmentTypesForEmployee = treatmentTypesForEmployee
                    .Where(type => _context.Treatment.Any(t => t.Type == type && t.Name == selectedTreatmentName))
                    .Distinct()
                    .ToList();
                ViewData["TreatmentNames"] = new SelectList(treatmentNamesForEmployee, "Id", "Name", selectedTreatmentName);
            }
            else
            {
                ViewData["TreatmentNames"] = treatmentNamesForEmployee;
            }

            var appointmentsEvents = appointments.Select(a => new CalendarEventViewModel
            {
                Id = a.Id,
                Title = $"{_sharedResource[a.Treatment.Type]} {_sharedResource[a.Treatment.Name]}",
                Start = a.Date,
                End = a.Date.AddMinutes(a.Duration),
                Employee = a.Employee.FullName,
                EmployeeId = a.EmployeeId,
                User = a.User != null ? a.User.FullName : "",
                TreatmentId = a.TreatmentId,
                Cost = a.Price,
                Color = a.Status == "Done" ? "green" : "blue"
            });
            var trainingsEvents = trainings.Select(w => new CalendarEventViewModel
            {
                Id = w.Id,
                Title = $"{_sharedResource[w.Treatment.Type]} {_sharedResource[w.Treatment.Name]}",
                Start = w.Date,
                End = w.Date.AddMinutes(w.Duration),
                Cost = w.Price,
                NumberUsers = $"{w.Users.Count()} / {w.UsersNumber}",
                Employee = w.Employee.FullName,
                EmployeeId = w.EmployeeId,
                TreatmentId = w.TreatmentId,
                Color = "#F08A69"
            });

            var events = appointmentsEvents.Concat(trainingsEvents);
            return Json(new
            {
                events = events,
                TreatmentTypes = treatmentTypesForEmployee,
                TreatmentNames = treatmentNamesForEmployee
            });
        }
        public async Task<IActionResult> Calendar(string userId)
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            var workingHours = await _context.WorkingHours
                .Where(w => w.EmployeeId == user.Id)
                .Select(w => new WorkingHoursViewModel
                {
                    UserId = user.Id,
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    UserBackgroundColor = user.BackgroundColor
                })
                .OrderBy(w => w.StartDate)
                .ToListAsync();

            var mergedWorkingHours = new List<WorkingHoursViewModel>();
            if (workingHours.Count > 0)
            {
                var currentMerged = workingHours[0];

                for (var i = 1; i < workingHours.Count; i++)
                {
                    if (workingHours[i].StartDate == currentMerged.StartDate)
                    {
                        if (workingHours[i].EndDate > currentMerged.EndDate)
                            currentMerged.EndDate = workingHours[i].EndDate;
                    }
                    else if (workingHours[i].StartDate <= currentMerged.EndDate)
                    {
                        if (workingHours[i].EndDate > currentMerged.EndDate)
                            currentMerged.EndDate = workingHours[i].EndDate;
                    }
                    else
                    {
                        mergedWorkingHours.Add(currentMerged);
                        currentMerged = workingHours[i];
                    }
                }
                mergedWorkingHours.Add(currentMerged);
            }
            var treatmentTypes = _userService.GetEmployeeAppointmentTypes(user.Id);
            var treatmentNames = _userService.GetEmployeeAppointmentNames(user.Id);
            ViewBag.UserId = user.Id;
            ViewBag.TreatmentTypes = treatmentTypes.Select(type => _sharedResource[type].Value).ToList();
            ViewBag.TreatmentNames = treatmentNames.Select(name => _sharedResource[name].Value).ToList();
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(mergedWorkingHours);
        }
        public async Task<IActionResult> AppointmentsList(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            var appointmentsViewModel = new AppointmentsViewModel
            {
                AppointmentsToDoNullList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == userId && t.Status == "To Do" && t.Rating == null).OrderByDescending(t => t.Date).ToList(),
                AppointmentsDoneNullList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == userId && t.Status == "Done" && t.Rating == null).OrderByDescending(t => t.Date).ToList(),
                AppointmentsDoneList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Where(t => t.UserId == userId && t.Status == "Done" && t.Rating != null).OrderByDescending(t => t.Date).ToList()
            };
            ViewBag.UserId = user.Id;
            return View(appointmentsViewModel);
        }
        public async Task<IActionResult> TrainingsList(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            var trainingsViewModel = new TrainingsListsViewModel
            {
                TrainingsToDoNullList = _context.Training.Include(t => t.Treatment).Include(t => t.Ratings).Include(t => t.Users).Where(t => t.Status == "To Do").Where(t => t.Users.Contains(user) || t.EmployeeId == user.Id).OrderByDescending(t => t.Date).ToList(),
                TrainingsDoneNullList = _context.Training
                .Include(t => t.Treatment)
                .Include(t => t.Users)
                .Where(t => t.Status == "Done")
                .Where(t => (t.Users.Contains(user) || t.EmployeeId == user.Id) && !t.Ratings.Any(r => r.UserId == user.Id))
                .OrderByDescending(t => t.Date)
                .ToList(),
                TrainingsDoneList = _context.Training
                .Include(t => t.Treatment)
                .Include(t => t.Users)
                .Where(t => t.Status == "Done")
                .Where(t => (t.Users.Contains(user) || t.EmployeeId == user.Id) && t.Ratings.Any(r => r.UserId == user.Id))
                .OrderByDescending(t => t.Date)
                .ToList()
            };
            ViewBag.UserId = user.Id;
            return View(trainingsViewModel);
        }

        public async Task<IActionResult> LoadAppointments(int skip, string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.UserId == userId && t.Status == "Done" && t.Rating != null)
                                        .OrderByDescending(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_AppointmentsPartial", appointments);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        public async Task<IActionResult> LoadTrainings(int skip, string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                var trainingsList = _context.Training
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Users)
                                        .Include(t => t.Ratings)
                                        .Where(t => t.Status == "Done")
                                        .Where(t => (t.Users.Contains(user) || t.EmployeeId == user.Id) && t.Ratings.Any(r => r.UserId == user.Id))
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
        public async Task<IActionResult> Roles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return NotFound();
            }
            var rolesViewModel = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                rolesViewModel.Add(userRolesViewModel);
            }

            var userProfileViewModel = new UserProfileViewModel
            {
                User = user,
                Roles = rolesViewModel
            };
            return View(userProfileViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Roles(UserProfileViewModel model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            model.User = user;
            if (user == null)
            {
                return View();
            }

            var rolesToRemove = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", _sharedResource["CannotRemoveUserRole"]);
                return View(model);
            }
            var selectedRoleName = model.Roles.FirstOrDefault(x => x.Selected)?.RoleName;
            if (selectedRoleName != null)
            {
                result = await _userManager.AddToRolesAsync(user, model.Roles.Where(x => x.Selected).Select(y => y.RoleName));
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", _sharedResource["CannotAddUserToRole"]);
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", _sharedResource["CannotAddUserToRole"]);
                return View(model);
            }

            return RedirectToAction("userlist", "admin");
        }
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.UserId = user.Id;
            return View(user);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId)
        {
            var appointmentsToUpdate = _context.Appointment.Where(a => a.UserId == userId).ToList();
            foreach (var appointment in appointmentsToUpdate)
            {
                appointment.UserId = null;
                appointment.User = null;
            }
            await _context.SaveChangesAsync();
            if (_context.Users == null)
            {
                return Problem("Entity set 'EngineerContext.Users'  is null.");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Admin");
        }
    }
}
