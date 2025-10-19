namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionCategory
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Slug { get; set; }

		public ICollection<AuctionItem> Items { get; set; } = new List<AuctionItem>();
	}
}