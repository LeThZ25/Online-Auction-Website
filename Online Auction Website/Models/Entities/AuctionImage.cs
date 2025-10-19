namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionImage
	{
		public int Id { get; set; }

		public int ItemId { get; set; }
		public AuctionItem Item { get; set; } = default!;

		public string FilePath { get; set; } = default!;
		public int SortOrder { get; set; }
	}
}