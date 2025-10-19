using System;
using System.ComponentModel.DataAnnotations;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class SessionInvite
	{
		public int Id { get; set; }

		// phiên được mời
		public int SessionId { get; set; }
		public AuctionSession Session { get; set; } = default!;

		// người gửi lời mời (host/admin)
		[Required]
		public string InviterUserId { get; set; } = string.Empty;
		public AppUser? Inviter { get; set; }

		// người được mời
		public string? InviteeUserId { get; set; }
		public AppUser? Invitee { get; set; }

		[EmailAddress]
		public string? InviteeEmail { get; set; }

		// token link mời
		[Required, StringLength(200)]
		public string Token { get; set; } = Guid.NewGuid().ToString("N");

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
		public DateTime? AcceptedAt { get; set; }
		public DateTime? RevokedAt { get; set; }
	}
}