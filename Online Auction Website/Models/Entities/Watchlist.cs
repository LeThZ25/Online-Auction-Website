// Models/Entities/Watchlist.cs
using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class Watchlist
	{
		[Required] public string UserId { get; set; } = "";
		[Required] public int ItemId { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public AppUser User { get; set; } = null!;
		public AuctionItem Item { get; set; } = null!;
	}
}