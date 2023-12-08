using Engineer_MVC.Models;
using Engineer_MVC.Models.ViewModels;

namespace Engineer_MVC.Data.Interfaces
{
    public interface IAppointmentService
    {
        List<User> GetFitsEmployees(string selectedTreatmentType, string selectedTreatmentName);
        List<TimeSlot> GetTimeSlots(List<User> employees);
        //void RemoveTimeInterval(WorkingHours workingHours, DateTime startTime, DateTime endTime);
        Task UpdateAppointmentsWithUserData(List<Appointment> appointments);
    }
}
