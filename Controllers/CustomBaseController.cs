using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Engineer_MVC.Controllers
{
    public class CustomBaseController : Controller
    {
        public PageData GetPageData(string controllerName, string actionName)
        {
            PageData pageData = new PageData();
            pageData.BackgroundImage = "Background.png";
            switch (controllerName)
            {
                case "Home":
                    switch (actionName)
                    {
                        case "Index":
                            pageData.BackgroundImage = "Eyes_Banner.png";
                            pageData.State = "Home";
                            break;
                        case "AboutUs":
                            pageData.BackgroundImage = "tlo_nowe.jpg";
                            pageData.State = "AboutUs";
                            break;
                        case "Contact":
                            pageData.State = "Header";
                            pageData.TopText = "StayUpdated";
                            pageData.BottomText = "ContactUs";
                            break;
                    }
                    break;
                case "Admin":

                    if (actionName == "Index")
                    {
                        pageData.State = "Admin";
                    }
                    else
                    {
                        pageData.State = "Header";
                        pageData.TopText = "AdminPanel";
                        pageData.BottomText = actionName switch
                        {
                            "EarningsStats" => "EarningsStats",
                            "EmployeeList" => "EmployeeList",
                            "EmployeesStats" => "EmployeesStats",
                            "QuantityStats" => "QuantityStats",
                            "UserList" => "UserList",
                            "WorkingTimeStats" => "WorkingTimeStats",
                            _ => ""
                        };
                    }
                    break;
                case "Identity":
                    switch (actionName)
                    {
                        case "Login":
                            pageData.State = "Login";
                            break;
                        case "Register":
                            pageData.State = "Register";
                            break;
                        case "ForgotPassword":
                            pageData.State = "Login";
                            break;
                        case "ForgotPasswordConfirmation":
                            pageData.State = "Register";
                            break;
                        case "ResendEmailConfirmation":
                            pageData.State = "Login";
                            break;
                        case "Manage":
                            pageData.State = "Header";
                            pageData.TopText = "AccountSettings";
                            pageData.BottomText = "MyAccount";
                            break;
                        case "AccessDenied":
                            pageData.State = "Header";
                            pageData.TopText = "AccessDenied";
                            pageData.BottomText = "NoPermissions";
                            break;

                    }
                    break;
                case "Posts":
                    switch (actionName)
                    {
                        case "PostsView":
                            pageData.State = "Header";
                            pageData.TopText = "StayUpdated";
                            pageData.BottomText = "ArticlesNews";
                            break;
                        case "SinglePost":
                            pageData.State = "SinglePost";
                            break;
                        case "Create":
                            pageData.State = "Header";
                            pageData.TopText = "Posts";
                            pageData.BottomText = "CreatePost";
                            break;
                        case "PolishPosts":
                            pageData.State = "Header";
                            pageData.TopText = "BlogList";
                            pageData.BottomText = "PolishPosts";
                            break;
                        case "ItalianPosts":
                            pageData.State = "Header";
                            pageData.TopText = "BlogList";
                            pageData.BottomText = "ItalianPosts";
                            break;
                        case "Edit":
                            pageData.State = "Header";
                            pageData.TopText = "Post";
                            pageData.BottomText = "EditPost";
                            break;
                        case "Details":
                            pageData.State = "Header";
                            pageData.TopText = "Post";
                            pageData.BottomText = "DetailsPost";
                            break;
                        case "Delete":
                            pageData.State = "Header";
                            pageData.TopText = "Post";
                            pageData.BottomText = "DeletePost";
                            break;
                    }
                    break;
                case "Treatments":
                    switch (actionName)
                    {
                        case "TreatmentsList":
                            pageData.State = "Header";
                            pageData.TopText = "TreatBeauty";
                            pageData.BottomText = "OurTreatments";
                            break;
                        case "Lashes":
                            pageData.State = "SingleTreatment";
                            pageData.TreatmentImage = "rzęsy_view.jpg";
                            pageData.TreatmentName = "Lashes";
                            pageData.TreatmentDescription = "LashesDescriptionView";
                            break;
                        case "Eyebrows":
                            pageData.State = "SingleTreatment";
                            pageData.TreatmentImage = "brwi_view.jpg";
                            pageData.TreatmentName = "Eyebrows";
                            pageData.TreatmentDescription = "EyebrowsDescriptionView";
                            break;
                        case "Nails":
                            pageData.State = "SingleTreatment";
                            pageData.TreatmentImage = "blue_nails.jpg";
                            pageData.TreatmentName = "Nails";
                            pageData.TreatmentDescription = "NailsDescriptionView";
                            break;
                        case "Skin":
                            pageData.State = "SingleTreatment";
                            pageData.TreatmentImage = "depilacja_view.jpg";
                            pageData.TreatmentName = "Skin";
                            pageData.TreatmentDescription = "SkinDescriptionView";
                            break;
                    }
                    break;
                case "EmployeeInAdmin":
                    pageData.State = "Header";
                    pageData.TopText = "AccountSettings";
                    pageData.BottomText = actionName switch
                    {
                        "Index" => "EmployeeProfile",
                        "AddTreatments" => "EditSkillsEmployee",
                        "AppointmentsList" => "AppointmentsList",
                        "Calendar" => "CalendarEmployee",
                        "Delete" => "DeleteEmployee",
                        "Roles" => "ChangeRolesEmployee",
                        "Stats" => "StatsEmployee",
                        "WorkingHours" => "WorkingHoursEmployee",
                        "NewWorkingHours" => "NewWorkingHours",
                        _ => "EmployeeProfile"
                    };
                    break;
                case "UserInAdmin":
                    pageData.State = "Header";
                    pageData.TopText = "AccountSettings";
                    pageData.BottomText = actionName switch
                    {
                        "Index" => "ProfileUser",
                        "AppointmentsList" => "AppointmentsList",
                        "Calendar" => "CalendarUser",
                        "Delete" => "DeleteUser",
                        "Roles" => "ChangeRolesUser",
                        "Stats" => "StatsUser",
                        _ => "ProfileUser"
                    };
                    break;

                case "WorkingHours":
                    switch (actionName)
                    {
                        case "CalendarEmployee":
                            pageData.State = "Header";
                            pageData.TopText = "AccountSettings";
                            pageData.BottomText = "CalendarEmployee";
                            break;
                        case "WorkingHoursEmployee":
                            pageData.State = "Header";
                            pageData.TopText = "AccountSettings";
                            pageData.BottomText = "WorkingHoursCalendar";
                            break;
                        case "EditMerged":
                            pageData.State = "Header";
                            pageData.TopText = "WorkingHours";
                            pageData.BottomText = "EditWorkingHours";
                            break;
                        case "NewWorkingHoursEmployee":
                            pageData.State = "Header";
                            pageData.TopText = "WorkingHours";
                            pageData.BottomText = "NewWorkingHours";
                            break;
                        case "WorkingHoursCalendar":
                            pageData.State = "Header";
                            pageData.TopText = "Calendar";
                            pageData.BottomText = "WorkingHoursCalendar";
                            break;

                    }
                    break;

                case "Employees":
                    switch (actionName)
                    {
                        case "ManageAppointments":
                            pageData.State = "Header";
                            pageData.TopText = "EmployeePanel";
                            pageData.BottomText = "ManageAppointments";
                            break;

                        case "ArchiveAppointments":
                            pageData.State = "Header";
                            pageData.TopText = "EmployeePanel";
                            pageData.BottomText = "AppointmentsArchive";
                            break;
                        case "ManageTrainings":
                            pageData.State = "Header";
                            pageData.TopText = "EmployeePanel";
                            pageData.BottomText = "ManageTraining";
                            break;
                        case "ArchiveTrainings":
                            pageData.State = "Header";
                            pageData.TopText = "EmployeePanel";
                            pageData.BottomText = "TrainingsArchives";
                            break;
                        case "Stats":
                            pageData.State = "Header";
                            pageData.TopText = "EmployeePanel";
                            pageData.BottomText = "EmployeeStats";
                            break;
                        case "Index":
                            pageData.State = "Admin";
                            break;
                    }
                    break;
                case "Appointments":
                    switch (actionName)
                    {
                        case "Index":
                            pageData.State = "Header";
                            pageData.TopText = "ManageAppointments";
                            pageData.BottomText = "ListAppointments";
                            break;
                        case "Create":
                            pageData.State = "Header";
                            pageData.TopText = "ManageAppointments";
                            pageData.BottomText = "MakeAppointment";
                            break;
                        case "WithoutScoreAppointmets":
                            pageData.State = "Header";
                            pageData.TopText = "ManageAppointments";
                            pageData.BottomText = "NoScoreList";
                            break;
                        case "DoneAppointments":
                            pageData.State = "Header";
                            pageData.TopText = "ManageAppointments";
                            pageData.BottomText = "AppointmentsArchive";
                            break;
                        case "CancelAppointments":
                            pageData.State = "Header";
                            pageData.TopText = "ManageAppointments";
                            pageData.BottomText = "ListOfRequests";
                            break;
                        case "Calendar":
                            pageData.State = "Header";
                            pageData.TopText = "Calendar";
                            pageData.BottomText = "CalendarAppointments";
                            break;
                        case "CalendarAdmin":
                            pageData.State = "Header";
                            pageData.TopText = "Calendar";
                            pageData.BottomText = "CalendarAppointments";
                            break;
                        
                    }
                    break;
                case "Trainings":
                    switch (actionName)
                    {
                        case "Index":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "TrainingList";
                            break;
                        case "Create":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "CreateTraining";
                            break;
                        case "Calendar":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "Calendar";
                            break;
                        case "DoneTrainings":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "ListOfDoneTrainings";
                            break;
                        case "EditParticipantsList":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "EditParticipantsList";
                            break;
                        case "CancelTrainings":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "ListOfRequests";
                            break;
                        case "Edit":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "EditTraining";
                            break;
                        case "Delete":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "DeleteTraining";
                            break;
                        case "Details":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "DetailsTraining";
                            break;
                        case "CalendarClient":
                            pageData.State = "Header";
                            pageData.TopText = "Calendar";
                            pageData.BottomText = "CalendarTrainings";
                            break;
                        case "TrainingList":
                            pageData.State = "Header";
                            pageData.TopText = "ManageTraining";
                            pageData.BottomText = "TrainingList";
                            break;
                    }
                    break;

            }
            return pageData;
        }
    }
}

