using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Helpers;
using OnlineAuctionWebsite.Hubs;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class SessionsController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<AppUser> _userManager;
		private readonly IHubContext<AuctionHub> _hub;

		public SessionsController(ApplicationDbContext db, UserManager<AppUser> userManager, IHubContext<AuctionHub> hub)
		{
			_db = db;
			_userManager = userManager;
			_hub = hub;
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(int sessionId)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.Id == sessionId);

			if (session == null) return NotFound();

			var now = DateTime.UtcNow;
			if (session.EndUtc <= now)
			{
				TempData["Error"] = "Phiên này đã kết thúc, không thể đăng ký.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			var userId = _userManager.GetUserId(User)!;
			if (session.IsPrivate)
			{
				var hasInvite = await _db.SessionInvites
					.AnyAsync(x => x.SessionId == sessionId && x.InviteeUserId == userId && x.ExpiresAt > DateTime.UtcNow);
				if (!hasInvite)
				{
					TempData["Error"] = "Phiên riêng tư: bạn chưa được mời.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}
			}
			var existed = await _db.Registrations
				.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == userId);

			if (existed != null)
			{
				if (existed.Status == AuctionRegistration.StatusPendingDeposit)
				{
					TempData["Info"] = "Vui lòng nộp tiền đặt trước để hoàn tất đăng ký.";
					return RedirectToAction("Deposit", "Payments", new { sessionId });
				}

				TempData["Info"] = "Bạn đã đăng ký tham gia phiên này rồi.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// no registration yet -> create
			var needsDeposit = session.DepositAmount > 0m;
			var reg = new AuctionRegistration
			{
				SessionId = sessionId,
				UserId = userId,
				Status = needsDeposit
					? AuctionRegistration.StatusPendingDeposit
					: AuctionRegistration.StatusApproved
			};

			_db.Registrations.Add(reg);
			await _db.SaveChangesAsync();

			if (!needsDeposit)
			{
				TempData["Success"] = "Đăng ký thành công! Bạn có thể đấu giá khi phiên mở.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			TempData["Info"] = "Vui lòng nộp tiền đặt trước để hoàn tất đăng ký.";
			return RedirectToAction("Deposit", "Payments", new { sessionId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AuctionItemCreateVM vm, CancellationToken ct)
		{
			if (!ModelState.IsValid) return View(vm);
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
			static TimeZoneInfo GetIct()
			{
				try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }           // Linux/macOS
				catch { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows
			}
			static DateTime ToUtc(DateTime localUnspecified)
			{
				// value from <input type="datetime-local"> has Kind=Unspecified
				var unspecified = DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified);
				return TimeZoneInfo.ConvertTimeToUtc(unspecified, GetIct());
			}

			// --- Convert your form's ICT times to real UTC before saving ---
			var startUtc = DateTime.SpecifyKind(vm.AuctionStartUtc, DateTimeKind.Utc);
			var endUtc = DateTime.SpecifyKind(vm.AuctionEndUtc, DateTimeKind.Utc);
			var item = new AuctionItem
			{
				Title = vm.Title,
				DescriptionHtml = vm.Description,
				AssetCode = vm.AssetCode,
				StartingPrice = vm.StartingPrice,
				ReservePrice = vm.ReservePrice,
				CategoryId = vm.CategoryId,
				SellerId = userId,
				CreatedAt = DateTime.UtcNow
			};
			_db.Items.Add(item);
			await _db.SaveChangesAsync(ct);

			// Create the session using the converted times
			_db.Sessions.Add(new AuctionSession
			{
				ItemId = item.Id,
				StartUtc = startUtc,
				EndUtc = endUtc,
				MinIncrement = vm.MinIncrement,
				Status = AuctionSessionStatus.Scheduled,
				OrganizationName = vm.OrganizationName,
				OrganizationAddress = vm.OrganizationAddress,
				AuctioneerName = vm.AuctioneerName,
				DepositAmount = vm.DepositAmount
			});

			await _db.SaveChangesAsync();
			return RedirectToAction("Details", "Items", new { id = item.Id });
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreatePrivate(int itemId, DateTime startUtc, DateTime endUtc, decimal minIncrement, bool isPrivate, string? inviteUserEmails)
		{
			var item = await _db.Items.Include(i => i.Sessions).FirstOrDefaultAsync(i => i.Id == itemId);
			if (item == null) return NotFound();

			var userId = _userManager.GetUserId(User)!;

			if (!User.IsInRole("Admin") && item.SellerId != userId) return Forbid();

			var session = new AuctionSession
			{
				ItemId = itemId,
				StartUtc = startUtc,
				EndUtc = endUtc,
				MinIncrement = minIncrement,
				Status = AuctionSessionStatus.Scheduled,
				IsPrivate = isPrivate,
				HostId = userId,
				InviteCode = isPrivate ? Guid.NewGuid().ToString("N") : null
			};

			_db.Sessions.Add(session);
			await _db.SaveChangesAsync();

			// Nếu là private, tự động thêm chủ phiên vào whitelist
			if (isPrivate)
			{
				_db.Set<AuctionSessionWhitelist>().Add(new AuctionSessionWhitelist
				{
					SessionId = session.Id,
					UserId = userId,
					AddedById = userId
				});

				// Mời nhanh qua danh sách email (ngăn cách dấu phẩy)
				if (!string.IsNullOrWhiteSpace(inviteUserEmails))
				{
					var emails = inviteUserEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					foreach (var email in emails)
					{
						var user = await _userManager.FindByEmailAsync(email);
						if (user != null)
						{
							_db.Set<AuctionSessionWhitelist>().Add(new AuctionSessionWhitelist
							{
								SessionId = session.Id,
								UserId = user.Id,
								AddedById = userId
							});
						}
					}
					await _db.SaveChangesAsync();
				}
			}

			TempData["Success"] = isPrivate
				? "Đã tạo phiên riêng tư. Bạn có thể mời người dùng qua mã mời hoặc whitelist."
				: "Đã tạo phiên đấu giá công khai.";
			return RedirectToAction("Details", "Items", new { id = itemId });
		}
		[Authorize(Roles = "Admin")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> FixSessionTimeToUtc(int sessionId, int tzOffset = 420) // 420 = +07:00 ICT
		{
			var s = await _db.Sessions.FindAsync(sessionId);
			if (s == null) return NotFound();

			// Treat current values as *local* time and convert to UTC
			static DateTime ToUtc(DateTime local, int minutesOffset)
			{
				var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
				var dto = new DateTimeOffset(unspecified, TimeSpan.FromMinutes(-minutesOffset)); // JS offset sign
				return dto.UtcDateTime;
			}

			s.StartUtc = ToUtc(s.StartUtc, tzOffset);
			s.EndUtc = ToUtc(s.EndUtc, tzOffset);

			await _db.SaveChangesAsync();
			return RedirectToAction("Details", "Items", new { id = s.ItemId });
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> StartNow(int id)
		{
			var s = await _db.Sessions
				.Include(x => x.Item)
				.FirstOrDefaultAsync(x => x.Id == id);

			if (s == null) return NotFound();

			var now = DateTime.UtcNow;

			// bắt đầu ngay: set StartUtc = now và chuyển sang Live
			s.StartUtc = now;
			if (s.EndUtc <= now) s.EndUtc = now.AddMinutes(30); // phòng thủ nếu end <= now
			s.Status = AuctionSessionStatus.Live;

			await _db.SaveChangesAsync();

			// realtime notify (tuỳ chọn, nếu bạn đang dùng SignalR)
			await _hub.Clients.Group($"session-{s.Id}")
				.SendAsync("SessionStarted", new { sessionId = s.Id, startUtc = s.StartUtc, endUtc = s.EndUtc });

			TempData["Success"] = "Đã bật phiên ngay bây giờ.";
			return RedirectToAction("Details", "Items", new { id = s.ItemId });
		}

		// --- Kết thúc ngay (Admin) ---
		[HttpPost, Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EndNow(int id)
		{
			var s = await _db.Sessions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
			if (s == null) return NotFound();

			// 1) Đánh dấu kết thúc
			s.Status = AuctionSessionStatus.Ended;
			s.EndUtc = DateTime.UtcNow;

			// 2) Tìm người thắng (SQLite không OrderBy decimal trong SQL -> ép double)
			var maxAmtD = await _db.Bids
				.Where(b => b.SessionId == s.Id)
				.Select(b => (double?)b.Amount)
				.MaxAsync();

			Bid? winner = null;
			if (maxAmtD.HasValue)
			{
				winner = await _db.Bids
					.Where(b => b.SessionId == s.Id && (double)b.Amount == maxAmtD.Value)
					.OrderBy(b => b.CreatedAt) // hoà thì ai đặt sớm hơn thắng
					.FirstOrDefaultAsync();
			}

			// 3) Nếu có người thắng -> tạo Payment "FINAL" ở trạng thái Pending
			if (winner != null)
			{
				var winnerId = winner.BidderId;
				var winAmount = (decimal)maxAmtD!.Value;

				// Tiền đặt trước (nếu có & đã Completed)
				var depositPaid = await _db.Registrations
					.Where(r => r.SessionId == id && r.UserId == winnerId && r.PaymentId != null)
					.Join(_db.Payments, r => r.PaymentId, p => p.Id, (r, p) => new { p.Status, p.Amount })
					.Where(x => x.Status == PaymentStatus.Completed)
					.Select(x => (decimal?)x.Amount)
					.FirstOrDefaultAsync() ?? 0m;

				var due = Math.Max(0m, winAmount - depositPaid);

				// Tránh tạo trùng nếu đã có FINAL-Pending
				var existed = await _db.Payments.FirstOrDefaultAsync(p =>
					p.SessionId == id &&
					p.UserId == winnerId &&
					p.Provider == "FINAL" &&
					p.Status == PaymentStatus.Pending);

				if (existed == null)
				{
					_db.Payments.Add(new Payment
					{
						SessionId = id,
						UserId = winnerId,
						Amount = due,                         // số tiền còn phải thanh toán
						Status = PaymentStatus.Pending,
						Method = PaymentMethod.BankTransfer,  // tạm; người dùng sẽ chọn lại (Bank/QR) ở bước thanh toán
						Provider = "FINAL",
						ProviderRef = $"FINAL-{id}-{winnerId}",
						CreatedAt = DateTime.UtcNow
					});
				}
			}

			await _db.SaveChangesAsync();

			// 4) Thông báo realtime (SignalR tuỳ chọn)
			await _hub.Clients.Group($"session-{id}")
				.SendAsync("SessionEnded", new { sessionId = id, winnerId = winner?.BidderId, amount = maxAmtD ?? 0 });

			TempData["Success"] = winner == null
				? "Phiên đã kết thúc. Không có ai đặt giá."
				: $"Phiên đã kết thúc. Người thắng: {(winner.Bidder?.UserName ?? winner.BidderId)}. Hệ thống đã tạo yêu cầu thanh toán.";

			return RedirectToAction("Details", "Items", new { id = s.ItemId });
		}
		[HttpGet]
		public async Task<IActionResult> Whitelist(int sessionId)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.Include(s => s.Whitelist).ThenInclude(w => w.User)
				.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var userId = _userManager.GetUserId(User)!;
			if (!User.IsInRole("Admin") && session.HostId != userId && session.Item.SellerId != userId) return Forbid();

			return View(session);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddToWhitelist(int sessionId, string email)
		{
			var session = await _db.Sessions.Include(s => s.Item).FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var userId = _userManager.GetUserId(User)!;
			if (!User.IsInRole("Admin") && session.HostId != userId && session.Item.SellerId != userId) return Forbid();

			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
			{
				TempData["Error"] = $"Không tìm thấy tài khoản với email: {email}";
				return RedirectToAction(nameof(Whitelist), new { sessionId });
			}

			var exists = await _db.Set<AuctionSessionWhitelist>()
				.AnyAsync(w => w.SessionId == sessionId && w.UserId == user.Id);
			if (!exists)
			{
				_db.Set<AuctionSessionWhitelist>().Add(new AuctionSessionWhitelist
				{
					SessionId = sessionId,
					UserId = user.Id,
					AddedById = userId
				});
				await _db.SaveChangesAsync();
			}

			TempData["Success"] = $"Đã thêm {email} vào danh sách tham gia.";
			return RedirectToAction(nameof(Whitelist), new { sessionId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RemoveFromWhitelist(int sessionId, string userId)
		{
			var session = await _db.Sessions.Include(s => s.Item).FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			var current = _userManager.GetUserId(User)!;
			if (!User.IsInRole("Admin") && session.HostId != current && session.Item.SellerId != current) return Forbid();

			var entry = await _db.Set<AuctionSessionWhitelist>()
				.FirstOrDefaultAsync(w => w.SessionId == sessionId && w.UserId == userId);
			if (entry != null)
			{
				_db.Remove(entry);
				await _db.SaveChangesAsync();
			}

			TempData["Success"] = "Đã xoá người dùng khỏi whitelist.";
			return RedirectToAction(nameof(Whitelist), new { sessionId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> JoinByCode(int sessionId, string code)
		{
			var session = await _db.Sessions.FindAsync(sessionId);
			if (session == null) return NotFound();

			if (!session.IsPrivate || string.IsNullOrWhiteSpace(session.InviteCode))
			{
				TempData["Error"] = "Phiên này không yêu cầu mã mời.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			if (!string.Equals(code?.Trim(), session.InviteCode, StringComparison.OrdinalIgnoreCase))
			{
				TempData["Error"] = "Mã mời không hợp lệ.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Challenge();

			var exists = await _db.SessionWhitelists
				.AnyAsync(w => w.SessionId == sessionId && w.UserId == userId);

			if (!exists)
			{
				_db.SessionWhitelists.Add(new AuctionSessionWhitelist
				{
					SessionId = sessionId,
					UserId = userId,
					AddedById = session.HostId
				});
				await _db.SaveChangesAsync();
			}

			TempData["Success"] = "Đã tham gia phiên riêng tư.";
			return RedirectToAction("Details", "Items", new { id = session.ItemId });
		}
	}
}