using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;
using System.Security.Claims;
using OnlineAuctionWebsite.Helpers;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class ItemsController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IWebHostEnvironment _env;
		private readonly UserManager<AppUser> _userManager;

		public ItemsController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<AppUser> userManager)
		{
			_db = db;
			_env = env;
			_userManager = userManager;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Details(int id)
		{
			var item = await _db.Items
				.AsNoTracking()
				.Include(i => i.Images)
				.Include(i => i.Sessions)
					.ThenInclude(s => s.Bids)
				.FirstOrDefaultAsync(i => i.Id == id);
			if (item == null) return NotFound();

			var now = DateTime.UtcNow;

			// phiên ưu tiên hiển thị
			var session = item.Sessions
				.Where(s => s.Status == AuctionSessionStatus.Live && s.StartUtc <= now && s.EndUtc > now)
				.OrderBy(s => s.EndUtc)
				.FirstOrDefault()
				?? item.Sessions.Where(s => s.StartUtc > now).OrderBy(s => s.StartUtc).FirstOrDefault()
				?? item.Sessions.OrderByDescending(s => s.EndUtc).FirstOrDefault();

			ViewBag.ActiveSession = session;

			// giá hiện tại
			var current = item.StartingPrice;
			if (session?.Bids?.Count > 0)
				current = session.Bids.Max(b => b.Amount);
			ViewBag.CurrentPrice = current;

			// hiển thị ICT
			if (session != null)
			{
				ViewBag.StartLocal = TimeHelper.ToIctFromUtc(session.StartUtc);
				ViewBag.EndLocal = TimeHelper.ToIctFromUtc(session.EndUtc);
			}

			var userId = User.Identity!.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
			var isOwnerOrAdmin = User.IsInRole("Admin") || (userId != null && item.SellerId == userId);

			bool hasInvite = true;
			if (session != null && session.IsPrivate && !isOwnerOrAdmin)
			{
				hasInvite = userId != null && await _db.SessionInvites
					.AnyAsync(i => i.SessionId == session.Id
								&& i.InviteeUserId == userId
								&& i.RevokedAt == null
								&& i.ExpiresAt > now);

				if (!hasInvite)
				{
					ViewBag.PrivateBlocked = true;
					session = null;
				}
			}

			ViewBag.ActiveSession = session;
			ViewBag.HasInvite = hasInvite;

			bool canSeePrivate = true;
			if (session != null && session.IsPrivate)
			{
				if (userId == null)
					canSeePrivate = false;
				else
				{
					var isHostOrSeller = (session.HostId == userId) || (session.Item.SellerId == userId) || User.IsInRole("Admin");
					var inWhiteList = await _db.Set<AuctionSessionWhitelist>()
						.AnyAsync(w => w.SessionId == session.Id && w.UserId == userId);

					canSeePrivate = isHostOrSeller || inWhiteList;
				}
			}

			ViewBag.CanSeePrivate = canSeePrivate;
			ViewBag.CanSeePrivate = !(session?.IsPrivate ?? false) || isOwnerOrAdmin || hasInvite;

			bool isRegistered = false, isApproved = false;
			if (userId != null && session != null)
			{
				var reg = await _db.Registrations.AsNoTracking()
					.FirstOrDefaultAsync(r => r.SessionId == session.Id && r.UserId == userId);
				isRegistered = reg != null;
				isApproved = reg?.Status == AuctionRegistration.StatusApproved;
				ViewBag.RegStatus = reg?.Status;
			}
			ViewBag.IsRegistered = isRegistered;
			ViewBag.IsApproved = isApproved;
			ViewBag.CanManageInvites = isOwnerOrAdmin && session != null && session.IsPrivate;

			decimal? budget = null;
			if (userId != null)
			{
				budget = await _db.Users.Where(u => u.Id == userId).Select(u => u.BudgetCeiling).FirstOrDefaultAsync();
			}
			ViewBag.Budget = budget;

			// Gợi ý theo ngân sách: khoảng ±30%
			if (budget.HasValue && budget.Value > 0)
			{
				var min = budget.Value * 0.7m; var max = budget.Value * 1.3m;
				ViewBag.BudgetSuggestions = await _db.Items.AsNoTracking()
					.Where(i => i.StartingPrice >= min && i.StartingPrice <= max && i.Id != item.Id)
					.OrderBy(i => i.StartingPrice).Take(6).ToListAsync();
			}

			// ===== Winner & quyền "thanh toán tiền trúng" =====
			bool isWinner = false;
			decimal winAmount = 0m;
			bool canSettle = false;

			if (userId != null && session != null && session.Status == AuctionSessionStatus.Ended)
			{
				var top = session.Bids?.OrderByDescending(b => b.Amount).FirstOrDefault();
				if (top != null && top.BidderId == userId)
				{
					isWinner = true;
					winAmount = top.Amount;

					// đã thanh toán trúng chưa?
					var alreadyPaid = await _db.Payments.AnyAsync(p =>
						p.SessionId == session.Id &&
						p.UserId == userId &&
						p.Status == PaymentStatus.Completed &&
						p.Amount >= winAmount  // hoặc == nếu bạn muốn chính xác tuyệt đối
					);
					canSettle = !alreadyPaid;
				}
			}

			ViewBag.IsWinner = isWinner;
			ViewBag.WinAmount = winAmount;
			ViewBag.CanSettle = canSettle;

			return View(item);
		}

		[HttpGet]
		[Authorize]
		public IActionResult Create()
		{
			ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
			return View(new AuctionItemCreateVM());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize]
		public async Task<IActionResult> Create(AuctionItemCreateVM vm)
		{
			// ---- Timezone coming from hidden input tzOffset (minutes)
			var tzOk = int.TryParse(Request.Form["tzOffset"], out var tzOffsetMinutes);
			if (!tzOk) tzOffsetMinutes = 0;

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Challenge();

			// ---- Server-side validations
			if (string.IsNullOrWhiteSpace(vm.AssetCode))
				ModelState.AddModelError(nameof(vm.AssetCode), "Vui lòng nhập mã tài sản.");

			// End must be > start
			if (vm.AuctionEndUtc <= vm.AuctionStartUtc)
				ModelState.AddModelError(nameof(vm.AuctionEndUtc), "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

			// Convert local -> UTC theo offset trình duyệt
			var startLocal = DateTime.SpecifyKind(vm.AuctionStartUtc, DateTimeKind.Unspecified);
			var endLocal = DateTime.SpecifyKind(vm.AuctionEndUtc, DateTimeKind.Unspecified);
			var startUtc = startLocal.AddMinutes(-tzOffsetMinutes);
			var endUtc = endLocal.AddMinutes(-tzOffsetMinutes);

			// AssetCode unique
			bool assetCodeExists = await _db.Items.AnyAsync(i => i.AssetCode == vm.AssetCode);
			if (assetCodeExists)
				ModelState.AddModelError(nameof(vm.AssetCode), "Mã tài sản đã tồn tại.");

			if (!ModelState.IsValid)
			{
				ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
				return View(vm);
			}

			// ---- Create Item
			var item = new AuctionItem
			{
				Title = vm.Title,
				DescriptionHtml = vm.Description,  // bạn đang dùng editor HTML -> map vào DescriptionHtml
				AssetCode = vm.AssetCode,
				StartingPrice = vm.StartingPrice,
				ReservePrice = vm.ReservePrice,
				CategoryId = vm.CategoryId,
				SellerId = userId,
				CreatedAt = DateTime.UtcNow,
				// đảm bảo collection không null nếu entity của bạn không set default
				Images = new List<AuctionImage>(),
				ItemTags = new List<AuctionItemTag>()
			};

			_db.Items.Add(item);
			await _db.SaveChangesAsync(); // cần Id để lưu media

			// ---- Create Session (đấu giá)
			var session = new AuctionSession
			{
				ItemId = item.Id,
				StartUtc = startUtc,
				EndUtc = endUtc,
				Status = AuctionSessionStatus.Scheduled,

				// Giá/bước giá/đặt trước
				MinIncrement = vm.MinIncrement,
				DepositAmount = vm.DepositAmount,

				// Tổ chức đấu giá
				OrganizationName = vm.OrganizationName,
				AuctioneerName = vm.AuctioneerName,
				OrganizationAddress = vm.OrganizationAddress,

				// Cooldown & Anti-sniping (nếu VM có — dùng mặc định khi null)
				BidCooldownSeconds = vm.BidCooldownSeconds > 0 ? vm.BidCooldownSeconds : 0,
				EnableAntiSniping = vm.EnableAntiSniping,
				ExtendWindowSeconds = vm.ExtendWindowSeconds > 0 ? vm.ExtendWindowSeconds : 0,
				ExtendBySeconds = vm.ExtendBySeconds > 0 ? vm.ExtendBySeconds : 0,
				ExtendMaxCount = vm.ExtendMaxCount > 0 ? vm.ExtendMaxCount : 0,
				ExtendCount = 0
			};
			_db.Sessions.Add(session);
			await _db.SaveChangesAsync();

			//Session whitelist & invites
			session.IsPrivate = vm.IsPrivate;
			session.HostId = userId;
			if (vm.IsPrivate && string.IsNullOrEmpty(session.InviteCode))
				session.InviteCode = Guid.NewGuid().ToString("N");

			// luôn whitelist host
			_db.SessionWhitelists.Add(new AuctionSessionWhitelist
			{
				SessionId = session.Id,
				UserId = userId,
				AddedById = userId
			});

			// mời nhanh qua email (nếu có)
			if (!string.IsNullOrWhiteSpace(vm.InviteUserEmails))
			{
				var emails = vm.InviteUserEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var email in emails)
				{
					var u = await _userManager.FindByEmailAsync(email);
					if (u != null)
					{
						_db.SessionWhitelists.Add(new AuctionSessionWhitelist
						{
							SessionId = session.Id,
							UserId = u.Id,
							AddedById = userId
						});
					}
				}
			}

			// ---- Tags
			static string Slugify(string s)
			{
				s = (s ?? "").Trim().ToLowerInvariant();
				foreach (var ch in new[] { ' ', '_', '.', ',', ';', '/', '\\', ':' }) s = s.Replace(ch, '-');
				while (s.Contains("--")) s = s.Replace("--", "-");
				return s.Trim('-');
			}

			var tagNames = (vm.Tags ?? "")
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (tagNames.Count > 0)
			{
				var slugs = tagNames.Select(Slugify).ToList();
				var existing = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync();

				foreach (var (name, slug) in tagNames.Zip(slugs, (n, s) => (n, s)))
				{
					var tag = existing.FirstOrDefault(x => x.Slug == slug);
					if (tag == null)
					{
						tag = new Tag { Name = name, Slug = slug };
						_db.Tags.Add(tag);
						existing.Add(tag);
					}
					item.ItemTags.Add(new AuctionItemTag { Item = item, Tag = tag });
				}
			}

			// ---- Uploads (images + documents)
			var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

			// IMAGES
			var imgDir = Path.Combine(root, "uploads", item.Id.ToString());
			Directory.CreateDirectory(imgDir);

			int sort = 0;
			var allowedImg = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

			foreach (var img in vm.Images ?? new())
			{
				if (img?.Length > 0)
				{
					var ext = Path.GetExtension(img.FileName);
					if (!allowedImg.Contains(ext)) continue;

					var fileName = $"{Guid.NewGuid()}{ext}";
					var fullPath = Path.Combine(imgDir, fileName);
					using (var fs = System.IO.File.Create(fullPath))
						await img.CopyToAsync(fs);

					_db.Images.Add(new AuctionImage
					{
						ItemId = item.Id,
						FilePath = $"/uploads/{item.Id}/{fileName}",
						SortOrder = sort++
					});
				}
			}

			var docDir = Path.Combine(root, "uploads", "docs", session.Id.ToString());
			Directory.CreateDirectory(docDir);

			var allowedDoc = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{ ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };

			foreach (var doc in vm.Documents ?? new())
			{
				if (doc?.Length > 0)
				{
					var ext = Path.GetExtension(doc.FileName);
					if (!allowedDoc.Contains(ext)) continue;

					var fileName = $"{Guid.NewGuid()}{ext}";
					var fullPath = Path.Combine(docDir, fileName);
					using (var fs = System.IO.File.Create(fullPath))
						await doc.CopyToAsync(fs);

					_db.Documents.Add(new AuctionDocument
					{
						SessionId = session.Id,
						FileName = Path.GetFileName(doc.FileName),
						FilePath = $"/uploads/docs/{session.Id}/{fileName}",
						ContentType = doc.ContentType,
						Size = doc.Length,
						UploadedAt = DateTime.UtcNow
					});
				}
			}

			await _db.SaveChangesAsync();

			var absolute = Url.Action("Details", "Items", new { id = item.Id }, Request.Scheme, Request.Host.Value);
			return Redirect(absolute!);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var item = await _db.Items.FindAsync(id);
			if (item == null) return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (User.IsInRole("Admin") || item.SellerId == userId)
			{
				return View(item);
			}
			else
			{
				return Forbid();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, AuctionItem updated)
		{
			if (id != updated.Id) return BadRequest();
			var item = await _db.Items.FindAsync(id);
			if (item == null) return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!User.IsInRole("Admin") && item.SellerId != userId)
			{
				return Forbid();
			}

			item.Title = updated.Title;
			item.DescriptionHtml = updated.DescriptionHtml;
			item.StartingPrice = updated.StartingPrice;
			item.ReservePrice = updated.ReservePrice;
			item.CategoryId = updated.CategoryId;

			await _db.SaveChangesAsync();
			return RedirectToAction(nameof(Details), new { id = item.Id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var item = await _db.Items.FindAsync(id);
			if (item == null) return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!User.IsInRole("Admin") && item.SellerId != userId)
			{
				return Forbid();
			}

			_db.Items.Remove(item);
			await _db.SaveChangesAsync();
			return RedirectToAction("Index", "Home");
		}
		[Authorize(Roles = "Admin")]
		[HttpPost, ValidateAntiForgeryToken]
		public async Task<IActionResult> AdminDelete(int id)
		{
			var item = await _db.Items.Include(i => i.Sessions).FirstOrDefaultAsync(i => i.Id == id);
			if (item == null) return NotFound();
			_db.Items.Remove(item);
			await _db.SaveChangesAsync();
			TempData["Success"] = "Đã xoá tài sản.";
			return RedirectToAction("Index", "Home");
		}
	}
}