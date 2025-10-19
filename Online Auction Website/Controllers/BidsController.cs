using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Hubs;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Services;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class BidsController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IHubContext<AuctionHub> _hub;
		private readonly IBidEngine _engine;

		public BidsController(ApplicationDbContext db, IHubContext<AuctionHub> hub, IBidEngine engine)
		{
			_db = db; _hub = hub; _engine = engine;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Place(int sessionId, decimal amount)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.FirstOrDefaultAsync(s => s.Id == sessionId);

			if (session == null)
			{
				TempData["Error"] = "Không tìm thấy phiên.";
				return RedirectToAction("Index", "Home");
			}

			// Lấy userId 1 lần và dùng cho mọi kiểm tra bên dưới
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Challenge();

			// Nếu phiên là riêng tư, chỉ cho phép ai nằm trong whitelist
			if (session.IsPrivate)
			{
				var inWhiteList = await _db.Set<AuctionSessionWhitelist>()
					.AnyAsync(w => w.SessionId == session.Id && w.UserId == userId);
				if (!inWhiteList)
				{
					TempData["Error"] = "Phiên đấu giá riêng tư: bạn chưa có quyền đặt giá.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}
			}

			// Bắt buộc đã đăng ký (và đã nộp đặt trước nếu có)
			var reg = await _db.Registrations
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == userId);

			if (reg == null || reg.Status != AuctionRegistration.StatusApproved)
			{
				TempData["Error"] = "Bạn cần đăng ký và nộp đặt trước (nếu có) trước khi đặt giá.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Chặn người bán tự đặt giá
			if (session.Item.SellerId == userId)
			{
				TempData["Error"] = "Bạn không thể đấu giá sản phẩm của chính mình.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Kiểm tra ngân sách (nếu bật chế độ chỉ xem khi vượt ngân sách)
			var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
			if (user?.BudgetCeiling is decimal budget && user.ViewOnlyWhenOverBudget && amount > budget)
			{
				TempData["Error"] = $"Số tiền bạn đặt ({amount:N0}) vượt ngân sách thiết lập ({budget:N0}).";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Đặt giá qua engine
			var (ok, error) = await _engine.PlaceBidAsync(sessionId, userId, amount);
			if (!ok)
			{
				TempData["Error"] = error;
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Ping watcher
			var watcherIds = await _db.Watchlists
				.Where(w => w.ItemId == session.ItemId && w.UserId != userId)
				.Select(w => w.UserId)
				.Distinct()
				.ToListAsync();

			foreach (var wid in watcherIds)
			{
				await _hub.Clients.Group($"user-{wid}")
					.SendAsync("WatchPing", new { itemId = session.ItemId, sessionId, amount });
			}

			TempData["Success"] = "Đặt giá thành công!";
			return RedirectToAction("Details", "Items", new { id = session.ItemId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SetProxy(int sessionId, decimal maxAmount)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			if (session.IsPrivate)
			{
				var inWhiteList = await _db.Set<AuctionSessionWhitelist>()
					.AnyAsync(w => w.SessionId == session.Id && w.UserId == userId);
				if (!inWhiteList)
				{
					TempData["Error"] = "Phiên đấu giá riêng tư: bạn chưa có quyền lưu Proxy.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}
			}
			// Không cho người bán đặt proxy
			if (session.Item.SellerId == userId)
			{
				TempData["Error"] = "Bạn không thể đặt proxy cho sản phẩm của chính mình.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Proxy cũng yêu cầu đã đăng ký/đặt cọc
			var reg = await _db.Registrations
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == userId);

			if (reg == null || reg.Status != AuctionRegistration.StatusApproved)
			{
				TempData["Error"] = "Bạn cần đăng ký và nộp đặt trước (nếu có) trước khi lưu Proxy.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			var (ok, error) = await _engine.SetAutoBidAsync(sessionId, userId, maxAmount);
			if (!ok)
			{
				TempData["Error"] = error;
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			TempData["Success"] = "Đã lưu Proxy bid.";
			return RedirectToAction("Details", "Items", new { id = session.ItemId });
		}
	}
}