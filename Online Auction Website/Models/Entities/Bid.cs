namespace OnlineAuctionWebsite.Models.Entities
{
	public class Bid
	{
		public int Id { get; set; }

		public int SessionId { get; set; }
		public AuctionSession Session { get; set; } = default!;

		public string BidderId { get; set; } = default!;
		public AppUser? Bidder { get; set; }

		public decimal Amount { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public byte[]? RowVersion { get; set; }
	}
}