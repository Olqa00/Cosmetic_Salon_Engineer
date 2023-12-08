using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Engineer_MVC.Data.Services
{
    public sealed class AppointmentService : IAppointmentService
    {
        private readonly EngineerContext _context;
        private readonly UserManager<User> _userManager;
        public AppointmentService(EngineerContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task UpdateAppointmentsWithUserData(List<Appointment> appointments)
        {
            foreach (var appointment in appointments)
            {
                var user = await _context.Users.FindAsync(appointment.UserId);
                if (user != null)
                {
                    appointment.User = user;
                }
            }
            await _context.SaveChangesAsync();
        }
        public List<User> GetFitsEmployees(string selectedTreatmentType, string selectedTreatmentName)
        {
            var users = _userManager.Users
                .Include(u => u.Treatments)
                .ToList();
            var eligibleEmployees = users
                .Where(u => _userManager.IsInRoleAsync(u, "Admin").Result ||
                            _userManager.IsInRoleAsync(u, "Employee").Result)
                .Where(u => u.Treatments != null)
                .ToList();
            var treatments = _context.Treatment.Include(e => e.Employees).ToList();
            if (selectedTreatmentType != "all" && selectedTreatmentType != null)
            {
                treatments = treatments.Where(a => a.Type == selectedTreatmentType).ToList();
            }
            if (selectedTreatmentName != "all" && selectedTreatmentName != null)
            {
                treatments = treatments.Where(a => a.Name == selectedTreatmentName).ToList();
            }
            var eligibleEmployeeIds = treatments.SelectMany(t => t.Employees.Select(e => e.Id)).Distinct();
            eligibleEmployees = eligibleEmployees.Where(e => eligibleEmployeeIds.Contains(e.Id)).ToList();
            return eligibleEmployees;
        }
        public List<TimeSlot> GetTimeSlots(List<User> employees)
        {
            var workingHoursList = _context.WorkingHours.Include(e => e.User).ToList();
            var appointmentsList = _context.Appointment.Include(e => e.Employee).ToList();
            var trainingsList = _context.Training.Include(e => e.Employee).ToList();
            var timeSlots = new List<TimeSlot>();

            foreach (var employee in employees)
            {
                var workingHours = workingHoursList.Where(a => a.EmployeeId == employee.Id).ToList();
                var employeeAppointments = appointmentsList.Where(a => a.EmployeeId == employee.Id).ToList();
                employeeAppointments.Sort((slot1, slot2) => slot1.Date.CompareTo(slot2.Date));
                var employeeTrainings = trainingsList.Where(a => a.EmployeeId == employee.Id).ToList();
                employeeTrainings.Sort((slot1, slot2) => slot1.Date.CompareTo(slot2.Date));
                var combinedSlots = new List<AppointmentOrTraining>();
                combinedSlots.AddRange(employeeAppointments.Select(appointment => new AppointmentOrTraining
                {
                    Date = appointment.Date,
                    EndTime=appointment.EndTime,
                    IsAppointment = true
                }));

                combinedSlots.AddRange(employeeTrainings.Select(training => new AppointmentOrTraining
                {
                    Date = training.Date,
                    EndTime = training.EndTime,
                    IsAppointment = false
                })) ;

                combinedSlots.Sort((slot1, slot2) => slot1.Date.CompareTo(slot2.Date));


                workingHours.Sort((slot1, slot2) => slot1.StartDate.CompareTo(slot2.StartDate));

                if (workingHours != null)
                {
                    foreach (var interval in workingHours)
                    {
                        var hasAppointmentsInInterval = combinedSlots.Any(a => a.Date >= interval.StartDate && a.Date <= interval.EndDate);
                        if (!hasAppointmentsInInterval)
                        {
                            if (interval.StartDate >= DateTime.Now.AddHours(2))
                            {
                                timeSlots.Add(new TimeSlot
                                {
                                    StartTime = interval.StartDate,
                                    EndTime = interval.EndDate,
                                    User = employee
                                });
                            }
                            else
                            {
                                var currentTime = DateTime.Now.AddHours(2);
                                var roundedStartTime = new DateTime(
                                    currentTime.Year,
                                    currentTime.Month,
                                    currentTime.Day,
                                    currentTime.Hour,
                                    (currentTime.Minute / 5) * 5, // Round to nearest multiple of 5
                                    0
                                );
                                timeSlots.Add(new TimeSlot
                                {
                                    StartTime = roundedStartTime,
                                    EndTime = interval.EndDate,
                                    User = employee
                                });
                            }

                        }
                        else if (hasAppointmentsInInterval)
                        {
                            if (interval.EndDate <= DateTime.Now.AddHours(2))
                            {
                                continue;
                            }
                            else if (!(interval.StartDate >= DateTime.Now.AddHours(2)))
                            {
                                var currentTime = DateTime.Now.AddHours(2);
                                var roundedStartTime = new DateTime(
                                    currentTime.Year,
                                    currentTime.Month,
                                    currentTime.Day,
                                    currentTime.Hour,
                                    (currentTime.Minute / 5) * 5, // Round to nearest multiple of 5
                                    0
                                );
                                interval.StartDate = roundedStartTime;
                            }
                            var lastEndTime = interval.StartDate;
                            foreach (var slot in combinedSlots) 
                            {
                                if (slot.Date >= lastEndTime && slot.Date <= interval.EndDate)
                                {
                                    timeSlots.Add(new TimeSlot
                                    {
                                        StartTime = lastEndTime,
                                        EndTime = slot.Date,
                                        User = employee
                                    });
                                    lastEndTime = slot.EndTime;
                                }
                                
                            }

                            if (lastEndTime < interval.EndDate)
                            {
                                timeSlots.Add(new TimeSlot
                                {
                                    StartTime = lastEndTime,
                                    EndTime = interval.EndDate,
                                    User = employee
                                });
                            }
                        }
                    }
                }
            }

            return timeSlots;
        }


        private void RemoveTimeInterval(WorkingHours workingHours, DateTime startTime, DateTime endTime)
        {
            if (endTime > workingHours.StartDate && startTime < workingHours.EndDate)
            {
                if (startTime >= workingHours.StartDate && endTime <= workingHours.EndDate)
                {
                    // Aktualizacja interwału czasowego w WorkingHours
                    workingHours.StartDate = endTime;
                }
                else if (startTime <= workingHours.StartDate && endTime >= workingHours.EndDate)
                {
                    // Usunięcie całego interwału, bo jest w nim zawarty cały WorkingHours
                    workingHours.StartDate = workingHours.EndDate;
                    workingHours.EndDate = workingHours.StartDate;
                }
                else if (startTime <= workingHours.StartDate)
                {
                    // Aktualizacja początku interwału
                    workingHours.StartDate = endTime;
                }
                else if (endTime >= workingHours.EndDate)
                {
                    // Aktualizacja końca interwału
                    workingHours.EndDate = startTime;
                }
            }
        }

    }
}
