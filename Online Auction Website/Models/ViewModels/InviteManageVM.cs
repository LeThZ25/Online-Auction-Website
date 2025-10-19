namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class InviteManageVM
	{
		public int SessionId { get; set; }
		public string ItemTitle { get; set; } = "";
		public bool IsPrivate { get; set; }
	}
}