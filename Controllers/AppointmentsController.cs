using Engineer_MVC.Data;
using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Engineer_MVC.Models.Templates;
using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using RazorLight;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Engineer_MVC.Controllers
{
    public class AppointmentsController : CustomBaseController
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly IUserService _userService;
        private readonly IAppointmentService _appointmentService;
        public AppointmentsController(IWebHostEnvironment hostEnvironment,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        EngineerContext context,
        IEmailSender emailSender,
        IStringLocalizer<SharedResource> sharedResource,
        IUserService userService,
        IAppointmentService appointmentService)
        {
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _sharedResource = sharedResource;
            _userService = userService;
            _appointmentService = appointmentService;
        }

        // GET: Appointments
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.Status == "To Do").OrderBy(t => t.Date).ToList();
            return View(TreatmentsList);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> WithoutScoreAppointmets()
        {
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.Status == "Done" && (t.Rating == null || t.Rating == 0)).OrderBy(t => t.Date).ToList();
            return View(TreatmentsList);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DoneAppointments()
        {
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.Status == "Done" && t.Rating != null).OrderByDescending(t => t.Date).ToList();
            return View(TreatmentsList);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelAppointments()
        {
            var TreatmentsList = _context.Appointment.Include(t => t.Treatment).Include(t => t.Employee).Include(t => t.User).Where(t => t.IsCanceled == true).OrderBy(t => t.Date).ToList();
            return View(TreatmentsList);
        }
        [Authorize(Roles = "Admin")]
        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Appointment == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Employee)
                .Include(a => a.Treatment)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }
        public async Task<IActionResult> ChangeToCanceled(int appointmentId)
        {
            var appointment = _context.Appointment.Include(t => t.Employee).Include(t => t.Treatment).Where(u => u.Id == appointmentId)
                .FirstOrDefault();
            var user = await _userManager.FindByIdAsync(appointment.UserId);
            if (appointment != null)
            {
                appointment.IsCanceled = true;
                _context.SaveChanges();
            }

            var templatePath = "Views/Templates/RequestCancelAppointmentTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);
            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;
            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build()
                ;

            var model = new AppointmentEmailModel
            {
                Employee = appointment.Employee.FullName,
                Treatment = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                StartDate = appointment.Date,
                EndDate = appointment.EndTime,
                Price = appointment.Price

            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { user.Email }, _sharedResource["RequestCancelAppointment"], resultTemplate);
            _emailSender.SendEmailAsync(message);
            return RedirectToAction("Index", "home");
        }
        public async Task<IActionResult> ChangeToNotCanceled(int appointmentId)
        {
            var appointment = _context.Appointment.Include(t=>t.Employee).Include(t=>t.Treatment).Where(u => u.Id == appointmentId)
                .FirstOrDefault();
            var user = await _userManager.FindByIdAsync(appointment.UserId);

            if (appointment != null)
            {
                appointment.IsCanceled = false;
                _context.SaveChanges();
            }

            var templatePath = "Views/Templates/NotAcceptedCancellationRequestAppoTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);
            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;
            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build()
                ;

            var model = new AppointmentEmailModel
            {
                Employee = appointment.Employee.FullName,
                Treatment = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                StartDate = appointment.Date,
                EndDate = appointment.EndTime,
                Price = appointment.Price

            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { user.Email }, _sharedResource["CancelRequestAppointment"], resultTemplate);
            _emailSender.SendEmailAsync(message);
            return RedirectToAction("Index", "home");
        }
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var appointment = _context.Appointment.Include(w => w.Employee).Include(t => t.Treatment).Where(u => u.Id == appointmentId)
                .FirstOrDefault();
            var user = await _userManager.FindByIdAsync(appointment.UserId);

            if (appointment != null)
            {
                _context.Appointment.Remove(appointment);
            }

            var templatePath = "Views/Templates/CancelAppointmentTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

            var model = new AppointmentEmailModel
            {
                Employee = appointment.Employee.FullName,
                Treatment = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                StartDate = appointment.Date,
                EndDate = appointment.EndTime,
                Price = appointment.Price

            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);

            var message = new Message(new string[] { user.Email }, _sharedResource["CanceledAppointment"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "home");
        }
        public async Task<IActionResult> LoadAppointments(int skip)
        {
            try
            {
                var loggedInUser = await _userManager.GetUserAsync(User);
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.UserId == loggedInUser.Id && t.Status == "Done" && t.Rating != null)
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
        public async Task<IActionResult> LoadAppointmentsEmployeeArchive(int skip)
        {
            try
            {
                var loggedInUser = await _userManager.GetUserAsync(User);
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.UserId == loggedInUser.Id && t.Status == "Done")
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
        public async Task<IActionResult> LoadAppointmentsEmployee(int skip)
        {
            try
            {
                var loggedInUser = await _userManager.GetUserAsync(User);
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.UserId == loggedInUser.Id && t.Status == "To Do")
                                        .OrderBy(t => t.Date)
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

        public async Task<IActionResult> LoadAppointmentsAdmin(int skip)
        {
            try
            {
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.Status == "To Do")
                                        .OrderBy(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_AppointmentsAdminPartial", appointments);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        public async Task<IActionResult> LoadAppointmentsNoScoreAdmin(int skip)
        {
            try
            {
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.Status == "Done" && (t.Rating == null || t.Rating == 0))
                                        .OrderBy(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_AppointmentsAdminPartial", appointments);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        public async Task<IActionResult> LoadAppointmentsArchiveAdmin(int skip)
        {
            try
            {
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.Status == "Done" && t.Rating != null)
                                        .OrderByDescending(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_AppointmentsAdminPartial", appointments);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        public async Task<IActionResult> LoadCanceledAdmin(int skip)
        {
            try
            {
                var appointments = _context.Appointment
                                        .Include(t => t.Treatment)
                                        .Include(t => t.Employee)
                                        .Where(t => t.IsCanceled == true)
                                        .OrderBy(t => t.Date)
                                        .Skip(skip)
                                        .Take(10)
                                        .ToList();

                return PartialView("_AppointmentsAdminPartial", appointments);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }
        public IActionResult GetAppointmentsForUserCalendar(string selectedTreatmentType, string selectedTreatmentName)
        {
            var fitsEmployees = _context.Users
                .Include(u => u.Treatments)
                .ToList();
            fitsEmployees = _appointmentService.GetFitsEmployees(selectedTreatmentType, selectedTreatmentName);
            var treatmentTypesForEmployees = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployees = _context.Treatment.Select(t => t.Name).Distinct().ToList();

            var treatments = _context.Treatment.Include(t => t.Employees).ToList();
            var nullAppointments = _context.Appointment.Where(a => a.UserId == null && a.Date >= DateTime.Now.AddHours(2)).ToList();

            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                treatments = treatments.Where(a => a.Type == selectedTreatmentType).ToList();
                nullAppointments = nullAppointments.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
                treatmentNamesForEmployees = treatmentNamesForEmployees
                    .Where(name => _context.Treatment.Any(t => t.Name == name && t.Type == selectedTreatmentType))
                    .Distinct()
                    .ToList();
                ViewData["TreatmentTypes"] = new SelectList(treatmentTypesForEmployees, "Id", "Type", selectedTreatmentType);
            }
            else
            {
                ViewData["TreatmentTypes"] = treatmentTypesForEmployees;
            }
            if (selectedTreatmentName != "all" && selectedTreatmentName != null)
            {
                treatments = treatments.Where(a => a.Name == selectedTreatmentName).ToList();
                nullAppointments = nullAppointments.Where(a => a.Treatment.Name == selectedTreatmentName).ToList();
                treatmentTypesForEmployees = treatmentTypesForEmployees
                    .Where(type => _context.Treatment.Any(t => t.Type == type && t.Name == selectedTreatmentName))
                    .Distinct()
                    .ToList();
                ViewData["TreatmentNames"] = new SelectList(treatmentNamesForEmployees, "Id", "Name", selectedTreatmentName);
            }
            else
            {
                ViewData["TreatmentNames"] = treatmentNamesForEmployees;
            }
            var employeeIdsWithTreatments = treatments.SelectMany(t => t.Employees.Select(e => e.Id)).Distinct().ToList();
            fitsEmployees = fitsEmployees.Where(e => employeeIdsWithTreatments.Contains(e.Id)).ToList();

            var timeSlots = _appointmentService.GetTimeSlots(fitsEmployees);

            var availableEvents = new List<CalendarEventViewModel>();
            var groups = new List<GroupedCalendarEventViewModel>();
            foreach (var employee in fitsEmployees)
            {
                var employeeTimeSlots = timeSlots.Where(ts => ts.User.Id == employee.Id);

                if (employeeTimeSlots != null && employeeTimeSlots.Any())
                {
                    DateTime lastEndTime = employeeTimeSlots.Min(ts => ts.StartTime);

                    foreach (var timeSlot in employeeTimeSlots)
                    {
                        lastEndTime = timeSlot.StartTime;

                        var availableTreatments = treatments
                            .Where(treatment => treatment.Employees.Any(e => e.Id == employee.Id))
                            .ToList();

                        foreach (var treatment in availableTreatments)
                        {
                            if (treatment.AverageTime.HasValue)
                            {
                                double averageTimeInMinutes = (double)treatment.AverageTime.Value;

                                while (lastEndTime.AddMinutes((double)treatment.AverageTime) <= timeSlot.EndTime)
                                {
                                    var availableEvent = new CalendarEventViewModel
                                    {

                                        Title = $"{_sharedResource[treatment.Type]} {_sharedResource[treatment.Name]}",
                                        Start = lastEndTime,
                                        End = lastEndTime.AddMinutes(averageTimeInMinutes),
                                        Employee = employee.FullName,
                                        EmployeeId = employee.Id,
                                        TreatmentId = treatment.Id,
                                        Cost = treatment.AverageCost.HasValue ? (float)treatment.AverageCost : 0f
                                    };
                                    availableEvents.Add(availableEvent);
                                    var existingGroup = groups.FirstOrDefault(g =>
                                            g.TreatmentId == treatment.Id &&
                                            g.Start == availableEvent.Start &&
                                            g.End == availableEvent.End);
                                    if (existingGroup != null)
                                    {
                                        existingGroup.GroupedEvents.Add(availableEvent);
                                    }
                                    else
                                    {
                                        var newGroup = new GroupedCalendarEventViewModel
                                        {
                                            Start = availableEvent.Start,
                                            End = availableEvent.End,
                                            TreatmentId = treatment.Id,
                                            GroupedEvents = new List<CalendarEventViewModel>
                                            {
                                                availableEvent
                                            }
                                        };
                                        groups.Add(newGroup);
                                    }

                                    lastEndTime = lastEndTime.AddMinutes((double)treatment.AverageTime);

                                }
                            }
                        }
                    }
                }
            }
            var nullUserIdEvents = nullAppointments.Select(appointment => new CalendarEventViewModel
            {
                Title = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                Start = appointment.Date,
                End = appointment.EndTime,
                Employee = appointment.Employee.FullName,
                EmployeeId = appointment.EmployeeId,
                TreatmentId = appointment.TreatmentId,
                Cost = appointment.Price
            }).ToList();
            foreach (var nullevent in nullUserIdEvents)
            {
                var existingGroup = groups.FirstOrDefault(g =>
                                            g.TreatmentId == nullevent.TreatmentId &&
                                            g.Start == nullevent.Start &&
                                            g.End == nullevent.End);
                if (existingGroup != null)
                {
                    existingGroup.GroupedEvents.Add(nullevent);
                }
                else
                {
                    var newGroup = new GroupedCalendarEventViewModel
                    {
                        Start = nullevent.Start,
                        End = nullevent.End,
                        TreatmentId = nullevent.TreatmentId,
                        GroupedEvents = new List<CalendarEventViewModel> { nullevent }
                    };
                    groups.Add(newGroup);
                }
            }



            return Json(new
            {
                groups = groups,
                //events = availableEvents,
                TreatmentTypes = treatmentTypesForEmployees,
                TreatmentNames = treatmentNamesForEmployees
            });
        }
        [Authorize]
        public async Task<IActionResult> Calendar()
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;
            var treatmentTypes = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNames = _context.Treatment.Select(t => t.Name).Distinct().ToList();

            ViewBag.TreatmentTypes = treatmentTypes.Select(type => _sharedResource[type].Value).ToList();
            ViewBag.TreatmentNames = treatmentNames.Select(name => _sharedResource[name].Value).ToList();
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View();
        }
        [Authorize]

        public async Task<IActionResult> MakeAppointmentAsync(string employeeId, int treatmentId, string start, string end)
        {
            var employee = _context.Users
                .Where(u => u.Id == employeeId)
                .FirstOrDefault();
            var treatment = _context.Treatment
                .Where(t => t.Id == treatmentId)
                .FirstOrDefault();
            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            TimeSpan duration = endDate - startDate;

            float durationInMinutes = (float)duration.TotalMinutes;
            var appointment = new Appointment
            {
                EmployeeId = employeeId,
                TreatmentId = treatmentId,
                UserId = userId,
                Date = startDate,
                Status = "To Do",
                Duration = durationInMinutes,
                Price = treatment.AverageCost.HasValue ? (float)treatment.AverageCost : 0f,
                Employee = employee,
                Treatment = treatment,
                User = user
            };
            if (!TryValidateModel(appointment))
            {
                return View(appointment);
            }
            _context.Appointment.Add(appointment);
            _context.SaveChanges();

            var templatePath = "Views/Templates/NewAppointmentTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();
            var model = new AppointmentEmailModel
            {
                Employee = appointment.Employee.FullName,
                Treatment = $"{_sharedResource[treatment.Type]} {_sharedResource[treatment.Name]}",
                StartDate = appointment.Date,
                EndDate = appointment.EndTime,
                Price = appointment.Price

            };
            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);

            var message = new Message(new string[] { user.Email }, _sharedResource["NewVisitRegister"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            return RedirectToAction("Calendar");
        }

        public IActionResult GetAppointmentsForCalendar(string selectedEmployee, string selectedTreatmentType, string selectedTreatmentName)
        {
            var appointments = _context.Appointment.Include(t => t.Treatment).Include(u => u.User).Include(e => e.Employee).ToList();
            var treatmentTypesForEmployee = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployee = _context.Treatment.Select(t => t.Name).Distinct().ToList();

            foreach (var appointment in appointments)
            {
                var treatment = _context.Treatment.Find(appointment.TreatmentId);
                var employee = _context.Users.Find(appointment.EmployeeId);
                var user = _context.Users.Find(appointment.UserId);

                appointment.Treatment = treatment;
                appointment.Employee = employee;
                appointment.User = user;
            }

            if (selectedEmployee != "all" && selectedEmployee != null)
            {
                appointments = appointments.Where(a => a.EmployeeId == selectedEmployee).ToList();
                ViewData["EmployeeId"] = new SelectList(selectedEmployee, "Id", "FullName");

                treatmentTypesForEmployee = _userService.GetEmployeeAppointmentTypes(selectedEmployee);

                treatmentNamesForEmployee = _userService.GetEmployeeAppointmentNames(selectedEmployee);
            }
            else
            {
                ViewData["EmployeeId"] = new SelectList(new List<dynamic>(), "Id", "FullName");
            }

            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                appointments = appointments.Where(a => a.Treatment.Type == selectedTreatmentType).ToList();
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

            var events = appointments.Select(a => new
            {
                Id = a.Id,
                Title = $"{_sharedResource[a.Treatment.Type]} {_sharedResource[a.Treatment.Name]}",
                Start = a.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                End = a.Date.AddMinutes(a.Duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                Employee = a.Employee.FullName,
                User = a.User != null ? a.User.FullName : "",
                TreatmentId = a.TreatmentId,
                Cost = a.Price,
                color = a.Employee.BackgroundColor
            });

            return Json(new
            {
                events = events,
                TreatmentTypes = treatmentTypesForEmployee,
                TreatmentNames = treatmentNamesForEmployee
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CalendarAdmin()
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
            var treatmentTypes = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNames = _context.Treatment.Select(t => t.Name).Distinct().ToList();

            ViewData["EmployeeId"] = new SelectList(employeesWithTreatment, "Id", "FullName");
            ViewBag.TreatmentTypes = treatmentTypes.Select(type => _sharedResource[type].Value).ToList();
            ViewBag.TreatmentNames = treatmentNames.Select(name => _sharedResource[name].Value).ToList();
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult GetEmployeesByTreatment(int treatmentId)
        {
            var users = _userManager.Users
                .Include(u => u.Treatments)
                .ToList();
            var employeesWithTreatment = users
                .Where(u => _userManager.IsInRoleAsync(u, "Admin").Result ||
                            _userManager.IsInRoleAsync(u, "Employee").Result)
                .Where(u => u.Treatments != null && u.Treatments.Any(t => t.Id == treatmentId))
                .Select(u => new { u.Id, FullName = $"{u.FirstName} {u.LastName}" })
                .ToList();
            var selectedTreatment = _context.Treatment.FirstOrDefault(t => t.Id == treatmentId);
            var averageTime = selectedTreatment?.AverageTime;
            var averageCost = selectedTreatment?.AverageCost;

            return Json(new { employeesWithTreatment, averageTime, averageCost });
        }
        [Authorize(Roles = "Admin,Employee")]
        // GET: Appointments/Create
        public async Task<IActionResult> Create() // Employee/ Admin
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<User>(users);
            userList.Insert(0, new User { Id = null, FirstName = "", LastName = "" }); // Add an empty option
            var treatments = _context.Treatment
                .Select(t => new
                {
                    Id = t.Id,
                    TypeAndName = $"{_sharedResource[t.Type]} {_sharedResource[t.Name]}"
                })
                .ToList();
            if (int.TryParse(Request.Query["TreatmentId"], out int selectedTreatmentId))
            {
                var result = GetEmployeesByTreatment(selectedTreatmentId) as JsonResult;
                var employeesWithTreatment = result.Value as List<dynamic>;
                ViewData["EmployeeId"] = new SelectList(employeesWithTreatment, "Id", "FullName");
                ViewData["TreatmentId"] = new SelectList(treatments, "Id", "TypeAndName", selectedTreatmentId);

                var selectedTreatment = _context.Treatment.FirstOrDefault(t => t.Id == selectedTreatmentId);
                if (selectedTreatment != null)
                {
                    ViewData["AverageTime"] = selectedTreatment.AverageTime;
                    ViewData["AverageCost"] = selectedTreatment.AverageCost;
                }
            }
            else
            {
                // If no TreatmentId is selected, pass an empty list for employees
                ViewData["EmployeeId"] = new SelectList(new List<dynamic>(), "Id", "FullName");
                ViewData["TreatmentId"] = new SelectList(treatments, "Id", "TypeAndName");
            }

            ViewData["UserId"] = new SelectList(userList, "Id", "FullName");
            return View();
        }

        // POST: Appointments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,Duration,Price,Status,IsLimited,TreatmentId,EmployeeId,UserId")] Appointment appointment)
        {
            var treatment = await _context.Treatment.FindAsync(appointment.TreatmentId);
            var employee = await _context.Users.FindAsync(appointment.EmployeeId);
            var user = await _context.Users.FindAsync(appointment.UserId);
            appointment.Treatment = treatment;
            appointment.Employee = employee;
            appointment.User = user;
            var treatments = _context.Treatment
                .Select(t => new
                {
                    Id = t.Id,
                    TypeAndName = $"{_sharedResource[t.Type]} {_sharedResource[t.Name]}"
                })
                .ToList();
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
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "FullName", appointment.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(treatments, "Id", "Name", appointment.TreatmentId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", appointment.UserId);
            return View(appointment);
        }
        [Authorize(Roles = "Admin,Employee")]
        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Appointment == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "FullName", appointment.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(_context.Treatment, "Id", "Name", appointment.TreatmentId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", appointment.UserId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,Duration,Price,Rating,Status,IsLimited,TreatmentId,EmployeeId,UserId")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }
            var treatment = await _context.Treatment.FindAsync(appointment.TreatmentId);
            var employee = await _context.Users.FindAsync(appointment.EmployeeId);
            var user = await _context.Users.FindAsync(appointment.UserId);
            appointment.Treatment = treatment;
            appointment.Employee = employee;
            appointment.User = user;

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
                _context.Update(appointment);
                var templatePath = "Views/Templates/ChangeInfoAppointmentTemplate.cshtml";
                var template = System.IO.File.ReadAllText(templatePath).ToString();
                var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
                var translatedTemplateResult = translationTemplate.Translate(template);

                var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

                var engine = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .Build()
                    ;

                var model = new AppointmentEmailModel
                {
                    Employee = appointment.Employee.FullName,
                    Treatment = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                    StartDate = appointment.Date,
                    EndDate = appointment.EndTime,
                    Price = appointment.Price

                };

                var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);

                var message = new Message(new string[] { user.Email }, _sharedResource["ChangeInfoAppointment"], resultTemplate);
                _emailSender.SendEmailAsync(message);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "FullName", appointment.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(_context.Treatment, "Id", "Name", appointment.TreatmentId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", appointment.UserId);
           
            

            return View(appointment);
        }
        [Authorize(Roles = "Admin,Employee")]
        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Appointment == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Employee)
                .Include(a => a.Treatment)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Appointment == null)
            {
                return Problem("Entity set 'EngineerContext.Appointment'  is null.");
            }
            
            var appointment = await _context.Appointment.FindAsync(id);
            var treatment = await _context.Treatment.FindAsync(appointment.TreatmentId);
            var employee = await _context.Users.FindAsync(appointment.EmployeeId);
            var user = await _context.Users.FindAsync(appointment.UserId);
            appointment.Treatment = treatment;
            appointment.Employee = employee;
            appointment.User = user;

            if (appointment != null)
            {
                _context.Appointment.Remove(appointment);
            }

            var templatePath = "Views/Templates/DeleteAppointmentTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build()
                ;

            var model = new AppointmentEmailModel
            {
                Employee = appointment.Employee.FullName,
                Treatment = $"{_sharedResource[appointment.Treatment.Type]} {_sharedResource[appointment.Treatment.Name]}",
                StartDate = appointment.Date,
                EndDate = appointment.EndTime,
                Price = appointment.Price

            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { appointment.User.Email }, _sharedResource["DeleteAppointment"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return (_context.Appointment?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
