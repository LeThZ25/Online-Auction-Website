using System.ComponentModel.DataAnnotations;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Models.Entities
{
	public enum AuctionSessionStatus
	{
		Scheduled, // Lên lịch, chưa diễn ra
		Live,      // Đang diễn ra
		Ended,     // Đã kết thúc
		Cancelled  // Đã hủy
	}

	public class AuctionSession
	{
		public int Id { get; set; }

		[Required]
		public int ItemId { get; set; }
		public AuctionItem Item { get; set; } = default!;
		public DateTime StartUtc { get; set; }
		public DateTime EndUtc { get; set; }
		public bool IsLive => DateTime.UtcNow >= StartUtc && DateTime.UtcNow <= EndUtc;
		public int BidCooldownSeconds { get; set; } = 3;
		public decimal? CurrentPrice { get; set; }
		public decimal MinIncrement { get; set; } = 10000m;
		public AuctionSessionStatus Status { get; set; } = AuctionSessionStatus.Scheduled;
		public DateTime RegistrationOpenUtc { get; set; }
		public DateTime RegistrationCloseUtc { get; set; }
		[Range(0, double.MaxValue)]
		public decimal PriceStep { get; set; }
		[Range(0, double.MaxValue)]
		public decimal DepositAmount { get; set; } = 0m;

		public DateTime DepositOpenUtc { get; set; }
		public DateTime DepositCloseUtc { get; set; }
		public DateTime ViewOpenUtc { get; set; }
		public DateTime ViewCloseUtc { get; set; }
		public DateTime? RecommendedViewOpenUtc { get; set; }
		public DateTime? RecommendedViewCloseUtc { get; set; }
		[Required, StringLength(200)]
		public string OrganizationName { get; set; } = string.Empty;   

		[StringLength(200)]
		public string? AuctioneerName { get; set; }

		[Required, StringLength(300)]
		public string OrganizationAddress { get; set; } = string.Empty;
		public string? HostId { get; set; }           
		public AppUser? Host { get; set; }             
		[StringLength(64)]
		public string? InviteCode { get; set; }       
		public ICollection<AuctionSessionWhitelist> Whitelist { get; set; }
			= new List<AuctionSessionWhitelist>();
		public ICollection<AuctionDocument> Documents { get; set; } = new List<AuctionDocument>();
		public bool IsPrivate { get; set; } = false;
		public ICollection<SessionInvite> Invites { get; set; } = new List<SessionInvite>();
		public ICollection<Bid> Bids { get; set; } = new List<Bid>();
		public ICollection<AuctionRegistration> Registrations { get; set; }
		   = new List<AuctionRegistration>();
		public bool EnableAntiSniping { get; set; } = true;
		public int ExtendWindowSeconds { get; set; } = 30;   
		public int ExtendBySeconds { get; set; } = 120;      
		public int ExtendMaxCount { get; set; } = 5;        
		public int ExtendCount { get; set; } = 0;
		public int AntiSnipingSeconds { get; set; } = 30;
		public int ExtendOnBidSeconds { get; set; } = 30;
		public int BidThrottleMs { get; set; } = 2000;
		public ICollection<ProxyBid> ProxyBids { get; set; } = new List<ProxyBid>();
		[Timestamp] public byte[]? RowVersion { get; set; }
	}
}