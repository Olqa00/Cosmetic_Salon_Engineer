namespace Engineer_MVC.Models.ViewModels
{
    public class EmployeeQuantityStatsData
    {
        public string EmployeeName { get; set; }
        public string? EmployeeColor { get; set; }
        public List<int> WeeklyData { get; set; }
        public List<string> WeeklyLabels { get; set; }
        public List<int> MonthlyData { get; set; }
        public List<string> MonthlyLabels { get; set; }
        public List<int> YearlyData { get; set; }
        public List<string> YearlyLabels { get; set; }
    }
}
