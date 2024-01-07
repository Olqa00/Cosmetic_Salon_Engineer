using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Engineer_MVC.Models.Templates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Engineer_MVC.Data.Interfaces;
using Microsoft.Extensions.Localization;
using Engineer_MVC.Data.Services;
using System.ComponentModel.DataAnnotations;
using Engineer_MVC.Models.ViewModels;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using RazorLight;

namespace Engineer_MVC.Controllers
{
    public class TrainingsController : Controller
    {
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public TrainingsController(EngineerContext context,
            IWebHostEnvironment hostEnvironment,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            IStringLocalizer<SharedResource> sharedResource,
            IUserService userService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _sharedResource = sharedResource;
            _userService = userService;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Trainings
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> UpdateSelects(string selectedTreatmentType, string employeeId, int trainingId)
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainingList()
        {
            var usersDictionary = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
            ViewBag.Users = usersDictionary;

            var trainingsList = _context.Training.Include(t => t.Users).Include(t => t.Employee).Include(t => t.Treatment).Where(t => t.Status == "To Do").OrderBy(t => t.Date).ToList();
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
        public async Task<IActionResult> TrainingList([FromForm] Training training)
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
                    return RedirectToAction("TrainingList");
                }

            }
            {
                ModelState.AddModelError("UserId", "Nieprawidłowy użytkownik.");
            }

            ViewBag.Types = await _context.Treatment.Select(t => t.Type).Distinct().ToListAsync();
            ViewBag.Names = await _context.Treatment.Select(t => t.Name).Distinct().ToListAsync();
            ViewBag.UsersCount = 0;
            return RedirectToAction("TrainingList");
        }

        public async Task<IActionResult> ChangeToCanceled(int trainingId, string userId)
        {
            var training = _context.Training.Include(t => t.Users).Include(t => t.Treatment).FirstOrDefault(t => t.Id == trainingId);
            var user = await _userManager.FindByIdAsync(userId);

            if (training != null)
            {
                var existingRequest = _context.CancellationRequest.FirstOrDefault(r => r.TrainingId == trainingId && r.UserId == userId);

                if (existingRequest == null)
                {
                    var request = new CancellationRequest
                    {
                        TrainingId = trainingId,
                        UserId = userId,
                        IsCanceled = true
                    };
                    _context.CancellationRequest.Add(request);
                }
                else
                {
                    existingRequest.IsCanceled = true;
                }

                _context.SaveChanges();
            }

            var templatePath = "Views/Templates/ParticipationTrainingRequestTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

            var model = new AppointmentEmailModel
            {
                Employee = training.Employee.FullName,
                Treatment = $"{_sharedResource[training.Treatment.Type]} {_sharedResource[training.Treatment.Name]}",
                StartDate = training.Date,
                EndDate = training.EndTime,
                Price = training.Price
            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { user.Email }, _sharedResource["ParticipationTraining"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            return RedirectToAction("Index", "home");
        }
        public async Task<IActionResult> ChangeToNotCanceled(int trainingId, string userId)
        {
            var training = _context.Training.Include(t => t.Users).Include(t => t.Treatment).FirstOrDefault(t => t.Id == trainingId);
            var user = await _userManager.FindByIdAsync(userId);
            if (training != null)
            {
                var existingRequest = _context.CancellationRequest.FirstOrDefault(r => r.TrainingId == trainingId && r.UserId == userId);

                if (existingRequest == null)
                {
                    var request = new CancellationRequest
                    {
                        TrainingId = trainingId,
                        UserId = userId,
                        IsCanceled = false
                    };
                    _context.CancellationRequest.Add(request);
                }
                else
                {
                    existingRequest.IsCanceled = false;
                }

                _context.SaveChanges();
            }

            var templatePath = "Views/Templates/ParticipationTrainingNotCanceledTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

            var model = new AppointmentEmailModel
            {
                Employee = training.Employee.FullName,
                Treatment = $"{_sharedResource[training.Treatment.Type]} {_sharedResource[training.Treatment.Name]}",
                StartDate = training.Date,
                EndDate = training.EndTime,
                Price = training.Price
            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { user.Email }, _sharedResource["ParticipationTraining"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            return RedirectToAction("Index", "home");
        }
        public async Task<IActionResult> CancelTrainings()
        {
            var trainingsWithRequests = new List<TrainingWithRequestsViewModel>();
            var trainings = _context.Training
                .Include(t => t.Users)
                .Include(t => t.Employee)
                .Include(t => t.Treatment)
                .Include(t => t.Requests)
                .Where(t => t.Requests.Any(r => r.IsCanceled == true))
                .ToList();
            foreach (var training in trainings)
            {
                var requests = _context.CancellationRequest
                    .Where(r => r.TrainingId == training.Id)
                    .ToList();

                trainingsWithRequests.Add(new TrainingWithRequestsViewModel
                {
                    Training = training,
                    Requests = requests
                });
            }

            return View(trainingsWithRequests);
        }

        public async Task<IActionResult> CancelTraining(int trainingId, string userId)
        {
            var training = _context.Training
                .Include(t => t.Employee)
                .Include(t => t.Users)
                .Include(t => t.Treatment)
                .FirstOrDefault(t => t.Id == trainingId)
                ;

            if (training == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            training.Users.Remove(user);

            var templatePath = "Views/Templates/ParticipationTrainingCanceledTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

            var model = new AppointmentEmailModel
            {
                Employee = training.Employee.FullName,
                Treatment = $"{_sharedResource[training.Treatment.Type]} {_sharedResource[training.Treatment.Name]}",
                StartDate = training.Date,
                EndDate = training.EndTime,
                Price = training.Price
            };

            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
            var message = new Message(new string[] { user.Email }, _sharedResource["ParticipationTraining"], resultTemplate);
            _emailSender.SendEmailAsync(message);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> GetTrainingsForCalendar(string selectedEmployee, string selectedTreatmentType, string selectedTreatmentName)
        {
            var employee = await _userManager.FindByIdAsync(selectedEmployee);
            var trainings = _context.Training.Include(t => t.Treatment).Include(u => u.Users).Include(e => e.Employee).ToList();
            var treatmentTypesForEmployee = _context.Treatment.Select(t => t.Type).Distinct().ToList();
            var treatmentNamesForEmployee = _context.Treatment.Select(t => t.Name).Distinct().ToList();

            if (selectedEmployee != "all" && selectedEmployee != null)
            {
                trainings = trainings.Where(a => a.EmployeeId == selectedEmployee).ToList();
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
            var events = trainings.Select(w => new
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
                Color = w.Employee.BackgroundColor
            });
            return Json(new
            {
                events = events,
                TreatmentTypes = treatmentTypesForEmployee,
                TreatmentNames = treatmentNamesForEmployee
            });
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Calendar()
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
            return View(); ;
        }
        [Authorize]
        public async Task<IActionResult> CalendarClient()
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var language = "pl";
            if (currentCulture == "it-IT")
            {
                language = "it";
            }
            ViewBag.language = language;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();


            var trainings = await _context.Training
                .Include(t => t.Treatment)
                .Include(u => u.Users)
                .Where(u => u.Status == "To Do" && u.Users.Count() < u.UsersNumber
                && !u.Users.Contains(user)
                && u.Date >= DateTime.Now.AddHours(2))
                .Select(w => new TrainingsViewModel
                {
                    Id = w.Id,
                    Title = $"{_sharedResource[w.Treatment.Type]} {_sharedResource[w.Treatment.Name]}",
                    StartDate = w.Date,
                    EndDate = w.Date.AddMinutes(w.Duration),
                    Cost = w.Price,
                    NumberUsers = $"{w.Users.Count()} / {w.UsersNumber}",
                    EmployeeBackgroundColor = w.Employee.BackgroundColor,
                    Employee = w.Employee.FullName,
                    EmployeeId = w.EmployeeId,
                    TreatmentId = w.TreatmentId
                })
                .ToListAsync();
            ViewBag.SharedResourceTranslations = JsonConvert.SerializeObject(_sharedResource.GetAllStrings());
            return View(trainings);
        }
        public async Task<IActionResult> SignUpTrainingAsync(int trainingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            var training = _context.Training.Include(u => u.Users).Include(u => u.Treatment)
                .Include(u => u.Employee).FirstOrDefault(t => t.Id == trainingId);
            if (user == null || training == null || training.Users.Count >= training.UsersNumber)
            {
                return RedirectToAction("CalendarClient");
            }
            training.Users.Add(user);

            _context.SaveChanges();

            var templatePath = "Views/Templates/SignUpTrainingTemplate.cshtml";
            var template = System.IO.File.ReadAllText(templatePath).ToString();
            var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
            var translatedTemplateResult = translationTemplate.Translate(template);

            var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();
            var model = new AppointmentEmailModel
            {
                Employee = training.Employee.FullName,
                Treatment = $"{_sharedResource[training.Treatment.Type]} {_sharedResource[training.Treatment.Name]}",
                StartDate = training.Date,
                EndDate = training.EndTime,
                Price = training.Price
            };
            var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);

            var message = new Message(new string[] { user.Email }, _sharedResource["NewTrainingRegister"], resultTemplate);
            _emailSender.SendEmailAsync(message);

            return RedirectToAction("CalendarClient");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DoneTrainings()
        {
            var trainingsList = _context.Training.Include(t => t.Users).Include(t => t.Employee).Include(t => t.Treatment).Where(t => t.Status == "Done").OrderByDescending(t => t.Date).ToList();
            return View(trainingsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoneTrainings([FromForm] Training training)
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
                return RedirectToAction("TrainingList");
            }


            return RedirectToAction("TrainingList");
        }
        public async Task<IActionResult> SetStars(int treatmentId, int starsCount)
        {
            var training = await _context.Training
                .Include(t => t.Ratings)
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == treatmentId);

            if (training == null)
            {
                return NotFound();
            }

            if (training.Ratings == null)
            {
                training.Ratings = new List<RatingForTraining>();
            }

            var loggedInUser = await _userManager.GetUserAsync(User);

            if (training.Ratings.Any(r => r.UserId == loggedInUser.Id))
            {
                return BadRequest("User has already rated this training.");
            }

            var rating = new RatingForTraining
            {
                TrainingId = treatmentId,
                UserId = loggedInUser.Id,
                Rating = starsCount
            };

            training.Ratings.Add(rating);
            _context.Entry(training).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Json(new
            {
                treatmentId = treatmentId,
                starsCount = starsCount
            });
        }

        public async Task<IActionResult> GetAverageRating(int treatmentId)
        {
            var training = await _context.Training
                .Include(t => t.Ratings)
                .FirstOrDefaultAsync(t => t.Id == treatmentId);

            if (training == null)
            {
                return NotFound();
            }

            if (training.Ratings == null || training.Ratings.Count == 0)
            {
                return Json(new
                {
                    treatmentId = treatmentId,
                    averageRating = 0 // Średnia ocena wynosi 0, gdy brak ocen
                });
            }

            var totalRating = training.Ratings.Sum(r => r.Rating);
            var averageRating = (float)totalRating / training.Ratings.Count;

            return Json(new
            {
                treatmentId = treatmentId,
                averageRating = averageRating
            });
        }

        public async Task<IActionResult> LoadTrainingsArchive(int skip)
        {
            try
            {
                var loggedInUser = await _userManager.GetUserAsync(User);

                var trainingsList = _context.Training.Include(t => t.Users)
                                        .Include(t => t.Employee)
                                        .Include(t => t.Treatment)
                                        .Where(t => t.Status == "Done")
                                        .Where(t => (t.Users.Contains(loggedInUser) || t.EmployeeId == loggedInUser.Id) && t.Ratings.Any(r => r.UserId == loggedInUser.Id))
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
        public async Task<IActionResult> LoadTrainingsAdminArchive(int skip)
        {
            try
            {
                var trainingsList = _context.Training.Include(t => t.Users)
                                        .Include(t => t.Employee)
                                        .Include(t => t.Treatment)
                                        .Where(t => t.Status == "Done")
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
        [Authorize(Roles = "Admin")]
        public IActionResult EditParticipantsList(int id)
        {
            var training = _context.Training.Include(u => u.Users).FirstOrDefault(t => t.Id == id);
            var users = _context.Users
                .Include(u => u.TrainingsReceive)
                .ToList();

            var selectedUsers = users
                .Where(u => u.TrainingsReceive.Any(tr => tr.Id == id))
                .ToList();
            var selectedUserIds = selectedUsers.Select(u => u.Id).Select(id => id.ToString()).ToList();
            var model = new EditParticipantsListModel
            {
                UsersNumber = training.UsersNumber,
                Users = selectedUsers,
                SelectedUsers = selectedUserIds
            };
            ViewBag.trainingId = id;
            ViewBag.AllUsers = new SelectList(users, "Id", "FullName");
            return View(model);
        }
        [HttpPost]
        public IActionResult EditParticipantsList(EditParticipantsListModel viewModel, int trainingId)
        {
            var training = _context.Training.Include(u => u.Users).FirstOrDefault(t => t.Id == trainingId);
            if (training == null)
            {
                return NotFound();
            }
            var selectedUsers = _context.Users
                .Where(u => viewModel.SelectedUsers.Contains(u.Id))
                .ToList();
            var updatedTrainings = new List<Training> { training };
            foreach (var user in selectedUsers)
            {
                user.TrainingsReceive = updatedTrainings;
            }
            training.Users = _context.Users.Where(u => viewModel.SelectedUsers.Contains(u.Id)).ToList();
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }
        [Authorize(Roles = "Admin")]
        // GET: Trainings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Training == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(t => t.Employee)
                .Include(t => t.Treatment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (training == null)
            {
                return NotFound();
            }

            return View(training);
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

            return Json(new { employeesWithTreatment });
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {

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
            }
            else
            {
                // If no TreatmentId is selected, pass an empty list for employees
                ViewData["EmployeeId"] = new SelectList(new List<dynamic>(), "Id", "FullName");
                ViewData["TreatmentId"] = new SelectList(treatments, "Id", "TypeAndName");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,Duration,Price,Rating,Status,UsersNumber,IsCanceled,TreatmentId,EmployeeId")] Training training)
        {
            var treatment = await _context.Treatment.FindAsync(training.TreatmentId);
            var employee = await _context.Users.FindAsync(training.EmployeeId);
            training.Treatment = treatment;
            training.Employee = employee;
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
                var templatePath = "Views/Templates/NewTrainingTemplate.cshtml";
                var template = System.IO.File.ReadAllText(templatePath).ToString();
                var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
                var translatedTemplateResult = translationTemplate.Translate(template);

                var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

                var engine = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .Build();
                var model = new AppointmentEmailModel
                {
                    Employee = training.Employee.FullName,
                    Treatment = $"{_sharedResource[training.Treatment.Type]} {_sharedResource[training.Treatment.Name]}",
                    StartDate = training.Date,
                    EndDate = training.EndTime,
                    Price = training.Price

                };
                var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);
                var users = _context.Users.ToList();

                foreach (var user in users)
                {
                    var message = new Message(new string[] { user.Email }, _sharedResource["NewTrainingAdded"], resultTemplate);
                    _emailSender.SendEmailAsync(message);
                }

                _context.Add(training);
                var existingWorkingHours = _context.WorkingHours
                .Where(wh => wh.EmployeeId == employee.Id && wh.StartDate <= training.Date && wh.EndDate >= training.Date.AddMinutes(training.Duration))
                .FirstOrDefault();
                if (existingWorkingHours == null)
                {
                    var workingHours = new WorkingHours
                    {
                        StartDate = training.Date,
                        EndDate = training.Date.AddMinutes(training.Duration),
                        EmployeeId = training.EmployeeId,
                        User = employee
                    };
                    _context.Add(workingHours);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(TrainingList));
            }
            var treatments = _context.Treatment
                .Select(t => new
                {
                    Id = t.Id,
                    TypeAndName = $"{_sharedResource[t.Type]} {_sharedResource[t.Name]}"
                })
                .ToList();
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "Id", training.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(treatments, "Id", "TypeAndName", training.TreatmentId);
            return View(training);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Training == null)
            {
                return NotFound();
            }

            var training = await _context.Training.FindAsync(id);
            if (training == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "Id", training.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(_context.Treatment, "Id", "Name", training.TreatmentId);
            return View(training);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,Duration,Price,Rating,Status,UsersNumber,IsCanceled,TreatmentId,EmployeeId")] Training training)
        {
            if (id != training.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(training);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainingExists(training.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Home");
            }
            ViewData["EmployeeId"] = new SelectList(_context.Users, "Id", "Id", training.EmployeeId);
            ViewData["TreatmentId"] = new SelectList(_context.Treatment, "Id", "Name", training.TreatmentId);
            return View(training);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Training == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(t => t.Employee)
                .Include(t => t.Treatment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (training == null)
            {
                return NotFound();
            }

            return View(training);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Training == null)
            {
                return Problem("Entity set 'EngineerContext.Training'  is null.");
            }

            var training = await _context.Training.FindAsync(id);

            if (training != null)
            {
                _context.Training.Remove(training);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        private bool TrainingExists(int id)
        {
            return (_context.Training?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
