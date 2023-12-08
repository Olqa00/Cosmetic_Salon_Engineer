namespace Engineer_MVC.Models.ViewModels
{
    public sealed class UserProfileViewModel
    {
        public User User { get; set; }
        public List<ManageUserRolesViewModel> Roles { get; set; }
    }
}
