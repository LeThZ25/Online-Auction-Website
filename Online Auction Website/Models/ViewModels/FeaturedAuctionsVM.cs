namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class FeaturedAuctionsVM
	{
		public List<AuctionCardVM> Live { get; set; } = new();
		public List<AuctionCardVM> Upcoming { get; set; } = new();
		public List<AuctionCardVM> Newest { get; set; } = new();
	}
}