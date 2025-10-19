namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionItemTag
	{
		public int ItemId { get; set; }
		public AuctionItem Item { get; set; } = null!;

		public int TagId { get; set; }
		public Tag Tag { get; set; } = null!;
	}
}