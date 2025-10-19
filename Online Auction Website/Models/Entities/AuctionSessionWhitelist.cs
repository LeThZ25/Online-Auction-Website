using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionSessionWhitelist
	{
		public int Id { get; set; }

		[Required]
		public int SessionId { get; set; }
		public AuctionSession Session { get; set; } = default!;

		[Required]
		public string UserId { get; set; } = default!;
		public AppUser User { get; set; } = default!;

		[Required]
		public string AddedById { get; set; } = default!;
		public AppUser AddedBy { get; set; } = default!;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}