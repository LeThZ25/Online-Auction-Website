using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class InvitesController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<AppUser> _um;

		public InvitesController(ApplicationDbContext db, UserManager<AppUser> um)
		{
			_db = db; _um = um;
		}

		// Trang quản lý lời mời của một phiên
		public async Task<IActionResult> Manage(int sessionId)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.Include(s => s.Invites).ThenInclude(i => i.Invitee)
				.Include(s => s.Invites).ThenInclude(i => i.Inviter)
				.FirstOrDefaultAsync(s => s.Id == sessionId);

			if (session == null) return NotFound();

			var userId = _um.GetUserId(User)!;
			var isOwner = User.IsInRole("Admin") || session.Item.SellerId == userId;
			if (!isOwner) return Forbid();

			ViewBag.Session = session;
			return View(session.Invites.OrderByDescending(i => i.CreatedAt).ToList());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TogglePrivacy(int sessionId, bool isPrivate)
		{
			var session = await _db.Sessions.Include(s => s.Item).FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var userId = _um.GetUserId(User)!;
			var isOwner = User.IsInRole("Admin") || session.Item.SellerId == userId;
			if (!isOwner) return Forbid();

			session.IsPrivate = isPrivate;
			await _db.SaveChangesAsync();

			TempData["Success"] = isPrivate
				? "Phiên đã chuyển sang chế độ riêng tư."
				: "Phiên đã chuyển sang công khai.";
			return RedirectToAction(nameof(Manage), new { sessionId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public Task<IActionResult> MakePrivate(int sessionId) => TogglePrivacy(sessionId, true);

		[HttpPost]
		[ValidateAntiForgeryToken]
		public Task<IActionResult> MakePublic(int sessionId) => TogglePrivacy(sessionId, false);

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(int sessionId, string? email, string? userName, DateTime? expiresAt)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.Include(s => s.Invites)
				.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var userId = _um.GetUserId(User)!;
			var isOwner = User.IsInRole("Admin") || session.Item.SellerId == userId;
			if (!isOwner) return Forbid();

			if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(userName))
			{
				TempData["Error"] = "Hãy nhập email hoặc username.";
				return RedirectToAction(nameof(Manage), new { sessionId });
			}

			AppUser? invitee = null;
			if (!string.IsNullOrWhiteSpace(userName))
				invitee = await _um.Users.FirstOrDefaultAsync(u => u.UserName == userName);

			if (invitee == null && !string.IsNullOrWhiteSpace(email))
				invitee = await _um.FindByEmailAsync(email);

			// chống tạo trùng (nếu đã có invite active cho user này)
			if (invitee != null)
			{
				var dup = session.Invites.Any(i =>
					i.InviteeUserId == invitee.Id &&
					i.RevokedAt == null &&
					i.AcceptedAt == null &&
					i.ExpiresAt > DateTime.UtcNow);

				if (dup)
				{
					TempData["Info"] = "Đã tồn tại một lời mời hợp lệ cho người này.";
					return RedirectToAction(nameof(Manage), new { sessionId });
				}
			}

			var inv = new SessionInvite
			{
				SessionId = sessionId,
				InviterUserId = userId,
				InviteeUserId = invitee?.Id,
				InviteeEmail = invitee?.Email ?? email,
				ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7)
			};

			_db.SessionInvites.Add(inv);
			await _db.SaveChangesAsync();

			var link = Url.Action(nameof(Accept), "Invites", new { token = inv.Token }, Request.Scheme);
			TempData["Success"] = $"Đã tạo lời mời. Link: {link}";
			return RedirectToAction(nameof(Manage), new { sessionId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Revoke(int id)
		{
			var inv = await _db.SessionInvites
				.Include(i => i.Session).ThenInclude(s => s.Item)
				.FirstOrDefaultAsync(i => i.Id == id);
			if (inv == null) return NotFound();

			var userId = _um.GetUserId(User)!;
			var isOwner = User.IsInRole("Admin") || inv.Session.Item.SellerId == userId;
			if (!isOwner) return Forbid();

			inv.RevokedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();

			TempData["Success"] = "Đã thu hồi lời mời.";
			return RedirectToAction(nameof(Manage), new { sessionId = inv.SessionId });
		}

		// Người nhận bấm link này để chấp nhận
		[AllowAnonymous]
		public async Task<IActionResult> Accept(string token)
		{
			var inv = await _db.SessionInvites
				.Include(i => i.Session).ThenInclude(s => s.Item)
				.FirstOrDefaultAsync(i => i.Token == token);
			if (inv == null)
			{
				TempData["Error"] = "Lời mời không tồn tại.";
				return RedirectToAction("Index", "Home");
			}

			if (inv.RevokedAt != null || inv.ExpiresAt <= DateTime.UtcNow)
			{
				TempData["Error"] = "Lời mời đã hết hạn hoặc bị thu hồi.";
				return RedirectToAction("Index", "Home");
			}

			if (!User.Identity!.IsAuthenticated)
			{
				// quay lại đây sau khi đăng nhập
				var returnUrl = Url.Action(nameof(Accept), "Invites", new { token }, Request.Scheme);
				return RedirectToAction("Login", "Account", new { returnUrl });
			}

			var userId = _um.GetUserId(User)!;

			if (inv.InviteeUserId != null && inv.InviteeUserId != userId)
			{
				TempData["Error"] = "Lời mời này không dành cho tài khoản hiện tại.";
				return RedirectToAction("Index", "Home");
			}

			// ghép lời mời với user vừa đăng nhập
			inv.InviteeUserId ??= userId;
			inv.AcceptedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();

			// tự động đăng ký tham gia phiên (để có thể đặt giá)
			var exist = await _db.Registrations.AnyAsync(r => r.SessionId == inv.SessionId && r.UserId == userId);
			if (!exist)
			{
				_db.Registrations.Add(new AuctionRegistration
				{
					SessionId = inv.SessionId,
					UserId = userId,
					CreatedAt = DateTime.UtcNow
				});
				await _db.SaveChangesAsync();
			}
			var link = Url.Action(nameof(Accept), "Invites", new { token = inv.Token }, Request.Scheme);
			TempData["Success"] = "Bạn đã tham gia phiên riêng tư. Có thể đấu giá khi phiên mở.";
			return RedirectToAction("Details", "Items", new { id = inv.Session.ItemId });
		}
	}
}