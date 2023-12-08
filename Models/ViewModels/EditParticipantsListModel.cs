namespace Engineer_MVC.Models.ViewModels
{
    public class EditParticipantsListModel
    {
        public int UsersNumber { get; set; }
        public List<User> Users { get; set; }
        public List<string> SelectedUsers { get; set; }
    }
}
