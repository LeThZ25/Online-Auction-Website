using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineAuctionWebsite.Hubs;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineAuctionWebsite.Services
{
	public class BidEngine : IBidEngine
	{
		private readonly ApplicationDbContext _db;
		private readonly IHubContext<AuctionHub> _hub;
		private readonly IMemoryCache _cache;

		public BidEngine(ApplicationDbContext db, IHubContext<AuctionHub> hub, IMemoryCache cache)
		{
			_db = db; _hub = hub; _cache = cache;
		}

		public async Task<(bool ok, string? error)> PlaceBidAsync(int sessionId, string userId, decimal amount, CancellationToken ct = default)
		{
			var s = await _db.Sessions
				.Include(x => x.Item)
				.FirstOrDefaultAsync(x => x.Id == sessionId, ct);

			if (s == null) return (false, "Không tìm thấy phiên.");
			var now = DateTime.UtcNow;

			if (s.Status != AuctionSessionStatus.Live || now < s.StartUtc || now >= s.EndUtc)
				return (false, "Phiên đấu giá chưa mở hoặc đã kết thúc.");

			if (s.Item.SellerId == userId)
				return (false, "Bạn không thể đấu giá sản phẩm của chính mình.");

			// Chặn spam theo cấu hình phiên
			var coolSeconds = Math.Max(1, s.BidCooldownSeconds);
			var key = $"cooldown:{sessionId}:{userId}";
			if (_cache.TryGetValue(key, out _))
				return (false, "Bạn đang thao tác quá nhanh. Vui lòng thử lại sau.");
			_cache.Set(key, 1, TimeSpan.FromSeconds(coolSeconds));

			// Giá hiện tại (SQLite-safe)
			double? curReal = await _db.Bids.Where(b => b.SessionId == sessionId)
											.Select(b => (double?)b.Amount)
											.MaxAsync(ct);
			var current = curReal.HasValue ? (decimal)curReal.Value : s.Item.StartingPrice;
			var minAllowed = current + s.MinIncrement;
			if (amount < minAllowed) return (false, $"Giá tối thiểu hiện tại là {minAllowed:N0}.");

			// Leader cũ để bắn Outbid
			var leader = await _db.Bids
	.Where(b => b.SessionId == sessionId)
	.Select(b => new { b.BidderId, b.CreatedAt, Amt = (double)b.Amount })
	.OrderByDescending(x => x.Amt)        // ✅ giờ sắp xếp theo REAL
	.ThenBy(x => x.CreatedAt)
	.Select(x => x.BidderId)
	.FirstOrDefaultAsync(ct);

			// Ghi nhận bid
			_db.Bids.Add(new Bid { SessionId = sessionId, BidderId = userId, Amount = amount });
			await _db.SaveChangesAsync(ct);

			// Anti-sniping (dùng field thực tế trên model)
			if (s.EnableAntiSniping && s.ExtendCount < s.ExtendMaxCount)
			{
				var remain = (s.EndUtc - now).TotalSeconds;
				if (remain <= s.ExtendWindowSeconds)
				{
					s.EndUtc = s.EndUtc.AddSeconds(s.ExtendBySeconds);
					s.ExtendCount += 1;
					await _db.SaveChangesAsync(ct);

					await _hub.Clients.Group($"session-{sessionId}")
						.SendAsync("TimeExtended", new { sessionId, newEndUtc = s.EndUtc }, ct);
				}
			}

			// Broadcast giá mới
			await _hub.Clients.Group($"session-{sessionId}")
				.SendAsync("BidUpdated", new { sessionId, amount, bidder = userId }, ct);

			// Báo leader cũ bị vượt (nếu có)
			if (!string.IsNullOrEmpty(leader) && leader != userId)
			{
				await _hub.Clients.Group($"user-{leader}")
					.SendAsync("Outbid", new { sessionId, amount }, ct);
			}

			return (true, null);
		}

		public async Task<(bool ok, string? error)> SetAutoBidAsync(int sessionId, string userId, decimal maxAmount, CancellationToken ct = default)
		{
			if (maxAmount <= 0) return (false, "Số tiền không hợp lệ.");

			var s = await _db.Sessions.Include(x => x.Item)
									  .FirstOrDefaultAsync(x => x.Id == sessionId, ct);
			if (s == null) return (false, "Không tìm thấy phiên.");

			var now = DateTime.UtcNow;
			if (s.Status != AuctionSessionStatus.Live || now < s.StartUtc || now >= s.EndUtc)
				return (false, "Phiên đấu giá chưa mở hoặc đã kết thúc.");

			if (s.Item.SellerId == userId)
				return (false, "Bạn không thể đấu giá sản phẩm của chính mình.");

			// Tạo/ cập nhật ProxyBid
			var pb = await _db.ProxyBids
							  .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId, ct);
			if (pb == null)
				_db.ProxyBids.Add(new ProxyBid { SessionId = sessionId, UserId = userId, MaxAmount = maxAmount });
			else { pb.MaxAmount = maxAmount; pb.UpdatedAt = now; }

			await _db.SaveChangesAsync(ct);

			// Áp proxy 1 lần (giống ApplyProxyOnceAsync của controller hiện tại)
			await ApplyProxyOnceAsync(s, ct);

			return (true, null);
		}

		private async Task ApplyProxyOnceAsync(AuctionSession s, CancellationToken ct)
		{
			var sessionId = s.Id;

			double? curReal = await _db.Bids.Where(b => b.SessionId == sessionId)
											.Select(b => (double?)b.Amount)
											.MaxAsync(ct);
			decimal current = curReal.HasValue ? (decimal)curReal.Value : s.Item.StartingPrice;

			var proxies = await _db.ProxyBids.Where(p => p.SessionId == sessionId)
											 .OrderByDescending(p => p.MaxAmount)
											 .ToListAsync(ct);
			if (!proxies.Any()) return;

			var top = proxies.First();
			var second = proxies.Skip(1).FirstOrDefault();

			if (top.MaxAmount > current)
			{
				decimal target = Math.Max(current + s.MinIncrement,
										  (second?.MaxAmount ?? current) + s.MinIncrement);
				var amount = Math.Min(target, top.MaxAmount);

				if (amount > current)
				{
					_db.Bids.Add(new Bid { SessionId = sessionId, BidderId = top.UserId, Amount = amount });
					await _db.SaveChangesAsync(ct);

					await _hub.Clients.Group($"session-{sessionId}")
						.SendAsync("BidUpdated", new { sessionId, amount, bidder = "(proxy)" }, ct);

					var leader = await _db.Bids.Where(b => b.SessionId == sessionId && b.BidderId != top.UserId)
											   .OrderByDescending(b => b.Amount).ThenBy(b => b.CreatedAt)
											   .Select(b => b.BidderId).FirstOrDefaultAsync(ct);
					if (!string.IsNullOrEmpty(leader))
					{
						await _hub.Clients.Group($"user-{leader}")
							.SendAsync("Outbid", new { sessionId, amount }, ct);
					}
				}
			}
		}
	}
}