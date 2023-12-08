using Engineer_MVC.Data;
using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NuGet.Packaging;
using PagedList;
using System.Data;
using System.Globalization;

namespace Engineer_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : CustomBaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public readonly EngineerContext _context;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly IUserService _userService;
        public AdminController(UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        EngineerContext context,
        IStringLocalizer<SharedResource> sharedResource,
        IUserService userService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _sharedResource = sharedResource;
            _userService = userService;

        }
        public IActionResult Index()
        {   

            return View();
        }
        public async Task<IActionResult> UserListAsync(int? page)
        {
            int pageSize = 15;
            int pageNumber = page ?? 1;

            var models = await GetUserListToDisplay();

            // Tworzenie obiektu PagedList
            var pagedModels = models.ToPagedList(pageNumber, pageSize);

            return View(pagedModels);
        }

        private async Task<List<AdminViewModel>> GetUserListToDisplay()
        {
            var users = await _userManager.Users.ToListAsync();
            var models = new List<AdminViewModel>();
            foreach (var user in users)
            {
                var thisViewModel = new AdminViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    Roles = await GetUserRoles(user),
                    ImagePath = user.ImagePath
                };
                models.Add(thisViewModel);
            }
            models = models.OrderBy(user => user.LastName).ToList();
            return models;
        }
        public async Task<IActionResult> EmployeeListAsync(int? page)
        {
            int pageSize = 15;
            int pageNumber = page ?? 1;

            var models = await GetEmployeeListToDisplay();

            // Tworzenie obiektu PagedList
            var pagedModels = models.ToPagedList(pageNumber, pageSize);

            return View(pagedModels);
        }
        [HttpPost]
        public IActionResult UpdateUserBackgroundColor([FromBody] UpdateUserBackgroundColorModel model)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == model.UserId);
            if (user != null)
            {
                user.BackgroundColor = model.Color;
                var result = _userManager.UpdateAsync(user).Result;

                if (result.Succeeded)
                {
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }

        private async Task<List<AdminViewModel>> GetEmployeeListToDisplay()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var models = new List<AdminViewModel>();
            foreach (var user in employees)
            {
                var thisViewModel = new AdminViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    Roles = await GetUserRoles(user),
                    TreatmentNames = await GetUserTreatments(user),
                    BackgroundColor = user.BackgroundColor,
                    ImagePath = user.ImagePath
                };
                models.Add(thisViewModel);
            }
            models = models.OrderBy(user => user.LastName).ToList();
            return models;
        }
        private async Task<List<string>> GetUserRoles(User user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }
        private async Task<List<string>> GetUserTreatments(User user)
        {
            var treatmentsList = await _userService.GetUserTreatments(user);
            if (treatmentsList == null)
            {
                return new List<string>();
            }

            var treatmentNames = new List<string>();
            foreach (var treatment in treatmentsList)
            {
                var treatmentTypeName = $"{treatment.Type} {treatment.Name}";
                treatmentNames.Add(treatmentTypeName);
            }

            return treatmentNames;
        }

        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return NotFound();
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                model.Add(userRolesViewModel);
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
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
            var selectedRoleName = model.FirstOrDefault(x => x.Selected)?.RoleName;
            if (selectedRoleName != null)
            {
                result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
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
            return RedirectToAction("UserList");
        }
        public async Task<IActionResult> AddTreatments(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return NotFound();
            }
            ViewBag.UserName = user.UserName;
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();
            var treatments = await _context.Treatment.ToListAsync();
            var userTreatments = user.Treatments?.Select(t => t.Id).ToList() ?? new List<int>();
            var viewModel = treatments.Select(t => new AddTreatmentsViewModel
            {
                TreatmentId = t.Id,
                TreatmentType = t.Type,
                TreatmentName = t.Name,
                Selected = userTreatments.Contains(t.Id)
            }).ToList();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> AddTreatments(List<AddTreatmentsViewModel> viewModel, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return NotFound();
            }
            await _context.Entry(user).Collection(u => u.Treatments).LoadAsync();

            if (user.Treatments == null)
            {
                user.Treatments = new List<Treatment>();
            }
            else
            {
                user.Treatments.Clear();
            }
            var selectedTreatmentsIds = viewModel.Where(t => t.Selected).Select(t => t.TreatmentId).ToList();
            var selectedTreatments = await _context.Treatment.Where(t => selectedTreatmentsIds.Contains(t.Id)).ToListAsync();

            user.Treatments.AddRange(selectedTreatments);
            await _userManager.UpdateAsync(user);

            return RedirectToAction("EmployeeList");
        }

        public async Task<IActionResult> PullEarniningsStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(u => u.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weeklyData = appointments
                .Where(a => a.Status == "Done")
                .GroupBy(a => new
                {
                    Week = calendar.GetWeekOfYear(a.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
                    EmployeeId = a.Employee.Id
                })
                .Select(group => new
                {
                    Week = group.Key.Week,
                    EmployeeId = group.Key.EmployeeId,
                    Earnings = group.Sum(a => a.Price)
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = appointments
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
            var yearlyData = appointments
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
        public IActionResult EarningsStats()
        {
            return View();
        }
        public async Task<IActionResult> PullTimeStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(u => u.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weeklyData = appointments
                .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                .Select(group => new
                {
                    Week = group.Key,
                    Time = group.Sum(appointment => appointment.Duration)
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = appointments
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
            var yearlyData = appointments
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
        public IActionResult WorkingTimeStats()
        {
            return View();
        }
        public async Task<IActionResult> PullQuantityStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(u => u.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weeklyData = appointments
                .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                .Select(group => new
                {
                    Week = group.Key,
                    Quantity = group.Count()
                })
                .OrderBy(group => group.Week)
                .ToList();
            var monthlyData = appointments
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
            var yearlyData = appointments
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
        public IActionResult QuantityStats()
        {
            return View();
        }
        public async Task<IActionResult> PullAllEarniningsStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(a => a.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;

            var employeesData = new List<EmployeeStatsData>();

            foreach (var employee in employees)
            {
                var weeklyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                    .Select(group => new
                    {
                        Week = group.Key,
                        Earnings = group.Sum(a => a.Price)
                    })
                    .OrderBy(group => group.Week)
                    .ToList();

                var monthlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => new { a.Date.Year, a.Date.Month })
                    .Select(group => new
                    {
                        Year = group.Key.Year,
                        Month = group.Key.Month,
                        Earnings = group.Sum(a => a.Price)
                    })
                    .OrderBy(group => group.Year)
                    .ThenBy(group => group.Month)
                    .ToList();

                var yearlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => a.Date.Year)
                    .Select(group => new
                    {
                        Year = group.Key,
                        Earnings = group.Sum(a => a.Price)
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
                var employeeData = new EmployeeStatsData
                {
                    EmployeeName = employee.FullName,
                    EmployeeColor = employee.BackgroundColor,
                    WeeklyData = weeklyData.Select(item => item.Earnings).ToList(),
                    WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                    MonthlyData = monthlyData.Select(item => item.Earnings).ToList(),
                    MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                    YearlyData = yearlyData.Select(item => item.Earnings).ToList(),
                    YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
                };

                employeesData.Add(employeeData);
            }

            return Json(employeesData);
        }
        public async Task<IActionResult> PullAllTimeStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(a => a.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;

            var employeesData = new List<EmployeeTimeStatsData>();

            foreach (var employee in employees)
            {
                var weeklyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => new
                    {
                        Week = calendar.GetWeekOfYear(a.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)
                    })
                    .Select(group => new
                    {
                        Week = group.Key.Week,
                        Time = group.Sum(appointment => appointment.Duration)
                    })
                    .OrderBy(group => group.Week)
                    .ToList();

                var monthlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => new { a.Date.Year, a.Date.Month })
                    .Select(group => new
                    {
                        Year = group.Key.Year,
                        Month = group.Key.Month,
                        Time = group.Sum(appointment => appointment.Duration)
                    })
                    .OrderBy(group => group.Year)
                    .ThenBy(group => group.Month)
                    .ToList();

                var yearlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => a.Date.Year)
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
                var employeeData = new EmployeeTimeStatsData
                {
                    EmployeeName = employee.FullName,
                    EmployeeColor = employee.BackgroundColor,
                    WeeklyData = weeklyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                    WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                    MonthlyData = monthlyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                    MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                    YearlyData = yearlyData.Select(item => TimeSpan.FromMinutes(item.Time).TotalHours).ToList(),
                    YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
                };

                employeesData.Add(employeeData);
            }

            return Json(employeesData);
        }
        public async Task<IActionResult> PullAllQuantityStats()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(a => a.Employee).ToList();
            var calendar = CultureInfo.CurrentCulture.Calendar;

            var employeesData = new List<EmployeeQuantityStatsData>();

            foreach (var employee in employees)
            {
                var weeklyData = appointments
                     .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                     .GroupBy(appointment => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(appointment.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                        .Select(group => new
                        {
                            Week = group.Key,
                            Quantity = group.Count()
                         })
                     .OrderBy(group => group.Week)
                     .ToList();

                var monthlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => new { a.Date.Year, a.Date.Month })
                    .Select(group => new
                    {
                        Year = group.Key.Year,
                        Month = group.Key.Month,
                        Quantity = group.Count()
                    })
                    .OrderBy(group => group.Year)
                    .ThenBy(group => group.Month)
                    .ToList();

                var yearlyData = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id)
                    .GroupBy(a => a.Date.Year)
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
                var employeeData = new EmployeeQuantityStatsData
                {
                    EmployeeName = employee.FullName,
                    EmployeeColor = employee.BackgroundColor,
                    WeeklyData = weeklyData.Select(item => item.Quantity).ToList(),
                    WeeklyLabels = weeklyData.Select(item => weekString + item.Week).ToList(),
                    MonthlyData = monthlyData.Select(item => item.Quantity).ToList(),
                    MonthlyLabels = monthlyData.Select(item => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month)} {item.Year}").ToList(),
                    YearlyData = yearlyData.Select(item => item.Quantity).ToList(),
                    YearlyLabels = yearlyData.Select(item => item.Year.ToString()).ToList(),
                };

                employeesData.Add(employeeData);
            }

            return Json(employeesData);
        }
        public IActionResult EmployeesStats()
        {
            return View();
        }
        public async Task<IActionResult> PullAllScores()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Admin");
            employees.AddRange(await _userManager.GetUsersInRoleAsync("Employee"));
            var appointments = _context.Appointment.Include(a => a.Employee).ToList();

            var currentDate = DateTime.Now;
            var endDate = currentDate.Date;
            var startDate = currentDate.Date.AddDays(-30);

            var scoresData = new List<EmployeeScoresData>();

            foreach (var employee in employees)
            {
                var averageRating = appointments
                    .Where(a => a.Status == "Done" && a.EmployeeId == employee.Id && a.Date >= startDate && a.Date <= endDate)
                    .Average(a => a.Rating);
                
                if (averageRating == null) averageRating = 0;
                scoresData.Add(new EmployeeScoresData
                {
                    EmployeeName = employee.FullName,
                    EmployeeColor = employee.BackgroundColor,
                    AverageRating = averageRating.Value
                });
            }

            var data = new
            {
                averageRatings = scoresData.Select(data => data.AverageRating).ToList(),
                employeeNames = scoresData.Select(data => data.EmployeeName).ToList(),
                EmployeeColor = scoresData.Select(data => data.EmployeeColor).ToList()
            };

            return Json(data);
        }

        public async Task<IActionResult> DeleteUser(string? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'EngineerContext.User'  is null.");
            }
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
