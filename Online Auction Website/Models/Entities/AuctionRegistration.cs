using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionRegistration
	{
		public int Id { get; set; }

		[Required]
		public int SessionId { get; set; }
		public AuctionSession Session { get; set; } = null!;

		[Required]
		public string UserId { get; set; } = null!;
		public AppUser User { get; set; } = null!;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public int? PaymentId { get; set; }
		public Payment? Payment { get; set; }

		// keep your existing Status string, but use these values:
		public const string StatusApproved = "Approved";
		public const string StatusPendingDeposit = "PendingDeposit";

		[StringLength(20)]
		public string Status { get; set; } = "Approved";
	}
}