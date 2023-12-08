namespace Engineer_MVC.Models.ViewModels
{
	public class UserTreatmentsViewModel
	{
		public User User { get; set; }
		public List<AddTreatmentsViewModel> Treatments { get; set; }
	}
}
