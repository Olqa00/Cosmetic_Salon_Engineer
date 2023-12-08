namespace Engineer_MVC.Data.Interfaces
{
	public interface IEmailSender
	{
		void SendEmail(Message message);
		Task SendEmailAsync(Message message);
	}
}
