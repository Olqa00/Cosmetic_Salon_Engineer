namespace Engineer_MVC.Models.ViewModels
{
    public class EmployeeStatsData
    {
        public string EmployeeName { get; set; }
        public string? EmployeeColor { get; set; }
        public List<float> WeeklyData { get; set; }
        public List<string> WeeklyLabels { get; set; }
        public List<float> MonthlyData { get; set; }
        public List<string> MonthlyLabels { get; set; }
        public List<float> YearlyData { get; set; }
        public List<string> YearlyLabels { get; set; }
    }
}
