namespace Engineer_MVC.Models.ViewModels
{
    public class EmployeeTimeStatsData
    {
        public string EmployeeName { get; set; }
        public string? EmployeeColor { get; set; }
        public List<double> WeeklyData { get; set; }
        public List<string> WeeklyLabels { get; set; }
        public List<double> MonthlyData { get; set; }
        public List<string> MonthlyLabels { get; set; }
        public List<double> YearlyData { get; set; }
        public List<string> YearlyLabels { get; set; }
    }
}
