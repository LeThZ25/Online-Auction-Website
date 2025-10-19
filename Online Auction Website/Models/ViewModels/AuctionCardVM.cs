namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class AuctionCardVM
	{
		public int ItemId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? ThumbnailUrl { get; set; }
		public string Status { get; set; } = "Upcoming";
		public DateTime? StartUtc { get; set; }
		public DateTime? EndUtc { get; set; }
		public bool IsPrivate { get; set; }
		public decimal CurrentPrice { get; set; }
		public List<string>? ImageUrls { get; set; }
		public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
	}
}