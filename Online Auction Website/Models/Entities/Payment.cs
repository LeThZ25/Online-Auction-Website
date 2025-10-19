using Microsoft.AspNetCore.Mvc;
using System;

namespace OnlineAuctionWebsite.Models.Entities
{
	public enum PaymentMethod { Card = 1, Qr = 2, BankTransfer = 3 }
	public enum PaymentStatus { Pending = 0, Completed = 1, Failed = 2, Cancelled = 3 }

	public class Payment
	{
		public int Id { get; set; }
		public string UserId { get; set; } = null!;
		public int SessionId { get; set; }

		public decimal Amount { get; set; }
		public PaymentMethod Method { get; set; }
		public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
		public string? Provider { get; set; }
		public string? ProviderRef { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? PaidAt { get; set; }

		public AppUser User { get; set; } = null!;
		public AuctionSession Session { get; set; } = null!;
	}
}