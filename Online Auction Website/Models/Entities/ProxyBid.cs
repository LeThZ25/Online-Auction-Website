using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class ProxyBid
	{
		public int Id { get; set; }

		[Required]
		public int SessionId { get; set; }
		public AuctionSession Session { get; set; } = null!;

		[Required]
		public string UserId { get; set; } = string.Empty;
		public AppUser User { get; set; } = null!;

		[Range(0, double.MaxValue)]
		public decimal MaxAmount { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
	}
}