using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
using NuGet.Packaging;
using System.ComponentModel.DataAnnotations;
using Engineer_MVC.Models.ViewModels;
using Engineer_MVC.Data.Interfaces;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.Extensions.Localization;
using PagedList;
using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Localization;

namespace Engineer_MVC.Controllers
{
	public class WorkingHoursController : CustomBaseController
	{
		private readonly EngineerContext _context;
		private readonly UserManager<User> _userManager;
		private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _sharedResource;

        public WorkingHoursController(EngineerContext context, UserManager<User> userManager,
            IStringLocalizer<SharedResource> sharedResource,
            IUserService userService)
		{
			_context = context;
			_userManager = userManager;
            _sharedResource = sharedResource;
            _userService = userService;
		}
        [Authorize(Roles = "Admin")]
        // GET: WorkingHours
        public async Task<IActionResult> Index()
		{
			return _context.WorkingHours != null ?
						View(await _context.WorkingHours.Include(x => x.User).ToListAsync()) :
						Problem("Entity set 'EngineerContext.WorkingHours'  is null.");
		}
		public async Task<IActionResult> GetAppointmentsForCalendar(string selectedTreatmentType, string selectedTreatmentName)
		{
			var loggedInUser = await _userManager.GetUserAsync(User);
			var appointments = _context.Appointment.Include(t => t.Treatment).Include(u => u.User).Include(e => e.Employee).Where(w => w.EmployeeId == loggedInUser.Id).ToList();
			var treatmentTypesForEmployee = _userService.GetEmployeeAppointmentTypes(loggedInUser.Id);
			var treatmentNamesForEmployee = _userService.GetEmployeeAppointmentNames(loggedInUser.Id);

			var trainings=_context.Training.Include(t=>t.Treatment).Include(u=>u.Users).Where(w => w.EmployeeId == loggedInUser.Id).ToList();

            foreach (var appointment in appointments)
			{
				var treatment = _context.Treatment.Find(appointment.TreatmentId);
				var employee = _context.Users.Find(appointment.EmployeeId);
				var user = _context.Users.Find(appointment.UserId);

				appointment.Treatment = treatment;
				appointment.Employee = employee;
				appointment.User = user;
			}
			if (selectedTreatmentType != "all" && selectedTreatmentType != null)
			{
				appointments = appointments.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
                trainings= trainings.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
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
                trainings= trainings.Where(a => a.Treatment.Name == selectedTreatmentName).ToList();
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
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> CalendarEmployee()
		{
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;
            var loggedInUser = await _userManager.GetUserAsync(User);
			if (loggedInUser == null)
			{
				return NotFound();
			}

			var workingHours = await _context.WorkingHours
				.Where(w => w.EmployeeId == loggedInUser.Id)
				.Select(w => new WorkingHoursViewModel
				{
					UserId = loggedInUser.Id,
					StartDate = w.StartDate,
					EndDate = w.EndDate,
					UserBackgroundColor = loggedInUser.BackgroundColor
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
			var treatmentTypes = _userService.GetEmployeeAppointmentTypes(loggedInUser.Id);
			var treatmentNames = _userService.GetEmployeeAppointmentNames(loggedInUser.Id);
            ViewBag.TreatmentTypes = treatmentTypes.Select(type => _sharedResource[type].Value).ToList();
            ViewBag.TreatmentNames = treatmentNames.Select(name => _sharedResource[name].Value).ToList();
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            ViewBag.UserId = loggedInUser.Id;
			return View(mergedWorkingHours);
		}
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> WorkingHoursEmployee()
		{
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;
            var loggedInUser = await _userManager.GetUserAsync(User);
			if (loggedInUser == null)
			{
				return NotFound();
			}
			var workingHours = await _context.WorkingHours
				.Where(w => w.EmployeeId == loggedInUser.Id)
				.Select(w => new WorkingHoursViewModel
				{
					UserId = loggedInUser.Id,
					StartDate = w.StartDate,
					EndDate = w.EndDate,
					UserBackgroundColor = loggedInUser.BackgroundColor
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
			ViewBag.UserId = loggedInUser.Id;
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(mergedWorkingHours);
		}
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> NewWorkingHoursEmployee(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.UserId = user.Id;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewWorkingHoursEmployee([Bind("Id,StartDate,EndDate,EmployeeId")] WorkingHours workingHours)
        {
            workingHours.User = _context.Users.Find(workingHours.EmployeeId);
            var user = await _userManager.FindByIdAsync(workingHours.EmployeeId);
            var validationResults = new List<ValidationResult>();
            if (workingHours.StartDate == DateTime.MinValue || workingHours.EndDate == DateTime.MinValue)
            {
                validationResults.Add(new ValidationResult("Date can't be null"));
            }

            if (workingHours.StartDate > workingHours.EndDate)
            {
                validationResults.Add(new ValidationResult("End date must be after start date"));
            }
            if (!Validator.TryValidateObject(workingHours, new ValidationContext(workingHours), validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    ModelState.AddModelError("", validationResult.ErrorMessage);
                }
            }
            else
            {
                _context.Add(workingHours);
                await _context.SaveChangesAsync();
                return RedirectToAction("WorkingHoursEmployee", new { userId = workingHours.EmployeeId });
            }

            return RedirectToAction("Index","Employees");
        }
        // GET: WorkingHours/Details/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Details(int? id)
		{
			if (id == null || _context.WorkingHours == null)
			{
				return NotFound();
			}

			var workingHours = await _context.WorkingHours
				.FirstOrDefaultAsync(m => m.Id == id);
			if (workingHours == null)
			{
				return NotFound();
			}

			return View(workingHours);
		}

        // GET: WorkingHours/Create
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create()
		{
			var adminEmployees = await _userManager.GetUsersInRoleAsync("Admin");
			var employeeEmployees = await _userManager.GetUsersInRoleAsync("Employee");

			var employees = adminEmployees.Concat(employeeEmployees).ToList();

			ViewBag.EmployeeId = new SelectList(employees, "Id", "FullName");

			return View();
		}


		// POST: WorkingHours/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Id,StartDate,EndDate,EmployeeId")] WorkingHours workingHours)
		{
			workingHours.User = await _context.Users.FindAsync(workingHours.EmployeeId);

			var validationResults = new List<ValidationResult>();
			if (!Validator.TryValidateObject(workingHours, new ValidationContext(workingHours), validationResults, true))
			{
				foreach (var validationResult in validationResults)
				{
					ModelState.AddModelError("", validationResult.ErrorMessage);
				}
			}
			else
			{
				_context.Add(workingHours);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(workingHours);
		}
        // GET: WorkingHours/Create
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> CreateForYourself()
		{
			var loggedInUser = await _userManager.GetUserAsync(User);

			ViewBag.EmployeeId = loggedInUser.Id;

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateForYourself([Bind("Id,StartDate,EndDate,EmployeeId")] WorkingHours workingHours)
		{
			workingHours.User = _context.Users.Find(workingHours.EmployeeId);

			var validationResults = new List<ValidationResult>();
			if (!Validator.TryValidateObject(workingHours, new ValidationContext(workingHours), validationResults, true))
			{
				foreach (var validationResult in validationResults)
				{
					ModelState.AddModelError("", validationResult.ErrorMessage);
				}
			}
			else
			{
				_context.Add(workingHours);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(workingHours);
		}
        // GET: WorkingHours/Edit/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int? id)
		{
			if (id == null || _context.WorkingHours == null)
			{
				return NotFound();
			}

			var workingHours = await _context.WorkingHours.FindAsync(id);
			if (workingHours == null)
			{
				return NotFound();
			}
			return View(workingHours);
		}

		// POST: WorkingHours/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,StartDate,EndDate,EmployeeId")] WorkingHours workingHours)
		{
			if (id != workingHours.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(workingHours);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!WorkingHoursExists(workingHours.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(workingHours);
		}
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> EditMerged(string userId, string startWH, string endWH)
		{
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(startWH) || string.IsNullOrEmpty(endWH))
			{
				return BadRequest();
			}

			ViewBag.UserId = userId;
			ViewBag.StartTime = startWH;
			ViewBag.EndTime = endWH;
			ViewBag.OldStartWH = startWH;
			ViewBag.OldEndWH = endWH;

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> UpdateMerged(string startWH, string endWH, string oldStartWH, string oldEndWH, WorkingHours updatedWorkingHours)
		{
			string userId = Request.Form["userId"];
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(startWH) || string.IsNullOrEmpty(endWH))
			{
				return BadRequest("Invalid input data");
			}

			if (!DateTime.TryParse(startWH, out var startDate) || !DateTime.TryParse(endWH, out var endDate) || !DateTime.TryParse(oldStartWH, out var oldStartDate) || !DateTime.TryParse(oldEndWH, out var oldEndDate))
			{
				return BadRequest("Invalid date format");
			}

			var workingHoursToDelete = _context.WorkingHours
				.Where(w => w.EmployeeId == userId && w.StartDate >= oldStartDate && w.EndDate <= oldEndDate)
				.ToList();

			_context.WorkingHours.RemoveRange(workingHoursToDelete);


			var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);
			updatedWorkingHours.EmployeeId = userId;
			updatedWorkingHours.StartDate = startDate;
			updatedWorkingHours.EndDate = endDate;
			updatedWorkingHours.User = user;
			_context.WorkingHours.Add(updatedWorkingHours);

			await _context.SaveChangesAsync();
			return RedirectToAction("Index", "Admin");
		}
		[HttpPost]
		public async Task<IActionResult> DeleteMerged(string oldStartWH, string oldEndWH)
		{
			string userId = Request.Form["userId"];
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(oldStartWH) || string.IsNullOrEmpty(oldEndWH))
			{
				return BadRequest("Invalid input data");
			}

			if (!DateTime.TryParse(oldStartWH, out var oldStartDate) || !DateTime.TryParse(oldEndWH, out var oldEndDate))
			{
				return BadRequest("Invalid date format");
			}

			var workingHoursToDelete = _context.WorkingHours
				.Where(w => w.EmployeeId == userId && w.StartDate >= oldStartDate && w.EndDate <= oldEndDate)
				.ToList();

			_context.WorkingHours.RemoveRange(workingHoursToDelete);


			await _context.SaveChangesAsync();
			return RedirectToAction("Index", "Admin");
		}

		public async Task<IActionResult> GetWorkingHoursForCalendar(string selectedEmployee)
		{
			var workingHours = _context.WorkingHours
				.Include(w => w.User)
				.OrderBy(w => w.StartDate)
				.ToList();

			List<WorkingHoursViewModel> workingHoursViewModel = new List<WorkingHoursViewModel>();

			if (selectedEmployee != "all" && selectedEmployee != null)
			{
				var user = await _userManager.FindByIdAsync(selectedEmployee);
				if (user != null)
				{
					workingHoursViewModel = workingHours
						.Where(a => a.EmployeeId == user.Id)
						.Select(w => new WorkingHoursViewModel
						{
							UserId = user.Id,
                            Name = user.FullName,
                            StartDate = w.StartDate,
							EndDate = w.EndDate,
							UserBackgroundColor = user.BackgroundColor
						})
						.ToList();

					ViewData["EmployeeId"] = new SelectList(new List<dynamic> { new { Id = user.Id, FullName = user.FullName } }, "Id", "FullName");
				}
			}
			else
			{
				var users = _userManager.Users
					.Include(u => u.Treatments)
					.ToList();
				var employeesList = users
					.Where(u => _userManager.IsInRoleAsync(u, "Admin").Result ||
								_userManager.IsInRoleAsync(u, "Employee").Result)
					.ToList();

				foreach (var employee in employeesList)
				{
					//System.Diagnostics.Debug.WriteLine(employee);
					var employeeWorkingHours = workingHours
						.Where(a => a.EmployeeId == employee.Id)
						.Select(w => new WorkingHoursViewModel
						{
							UserId = employee.Id,
							Name=employee.FullName,
							StartDate = w.StartDate,
							EndDate = w.EndDate,
							UserBackgroundColor = employee.BackgroundColor
						})
						.ToList();
					workingHoursViewModel.AddRange(employeeWorkingHours);
				}

				ViewData["EmployeeId"] = new SelectList(new List<dynamic>(), "Id", "FullName");
			}

			var mergedWorkingHours = new List<WorkingHoursViewModel>();

			if (workingHoursViewModel.Count > 0)
			{
				// Sort working hours by UserId and then by StartDate.
				workingHoursViewModel = workingHoursViewModel.OrderBy(w => w.UserId).ThenBy(w => w.StartDate).ToList();

				var currentMerged = workingHoursViewModel[0];

				for (var i = 1; i < workingHoursViewModel.Count; i++)
				{
					// Check if the UserId is the same as the current merged item.
					if (workingHoursViewModel[i].UserId == currentMerged.UserId)
					{
						// Check if StartDate is within the range of the current merged item.
						if (workingHoursViewModel[i].StartDate <= currentMerged.EndDate)
						{
							// Extend the current merged item's EndDate if needed.
							if (workingHoursViewModel[i].EndDate > currentMerged.EndDate)
							{
								currentMerged.EndDate = workingHoursViewModel[i].EndDate;
							}
						}
						else
						{
							// If the UserId is the same but StartDate is outside the range, start a new merged item.
							mergedWorkingHours.Add(currentMerged);
							currentMerged = workingHoursViewModel[i];
						}
					}
					else
					{
						// If the UserId is different, start a new merged item.
						mergedWorkingHours.Add(currentMerged);
						currentMerged = workingHoursViewModel[i];
					}
				}

				// Add the last merged item.
				mergedWorkingHours.Add(currentMerged);
			}


			var events = mergedWorkingHours.Select(w => new
			{
				UserId = w.UserId,
				Name=w.Name,
				StartDate = w.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
				EndDate = w.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
				UserBackgroundColor = w.UserBackgroundColor
			});

			return Json(new
			{
				events = events,
			});
		}

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> WorkingHoursCalendar()
		{
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;
            var users = _userManager.Users
				.Include(u => u.Treatments)
				.ToList();
			var employeesWithTreatment = users
				.Where(u => _userManager.IsInRoleAsync(u, "Admin").Result ||
							_userManager.IsInRoleAsync(u, "Employee").Result)
				.Where(u => u.Treatments != null)
				.Select(u => new { u.Id, FullName = $"{u.FirstName} {u.LastName}" })
				.ToList();
			ViewData["EmployeeId"] = new SelectList(employeesWithTreatment, "Id", "FullName");
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View();
		}
        [Authorize(Roles = "Admin")]

        // GET: WorkingHours/Delete/5
        public async Task<IActionResult> Delete(int? id)
		{
			if (id == null || _context.WorkingHours == null)
			{
				return NotFound();
			}

			var workingHours = await _context.WorkingHours
				.FirstOrDefaultAsync(m => m.Id == id);
			if (workingHours == null)
			{
				return NotFound();
			}

			return View(workingHours);
		}

		// POST: WorkingHours/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (_context.WorkingHours == null)
			{
				return Problem("Entity set 'EngineerContext.WorkingHours'  is null.");
			}
			var workingHours = await _context.WorkingHours.FindAsync(id);
			if (workingHours != null)
			{
				_context.WorkingHours.Remove(workingHours);
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool WorkingHoursExists(int id)
		{
			return (_context.WorkingHours?.Any(e => e.Id == id)).GetValueOrDefault();
		}
	}
}
