using Microsoft.AspNetCore.Mvc;

namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class HeaderCountdownVM
	{
		public DateTime? TargetUtc { get; set; }
		public string Mode { get; set; } = "clock";
	}
}