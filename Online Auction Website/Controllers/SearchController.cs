using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	public class SearchController : Controller
	{
		private readonly ApplicationDbContext _db;
		public SearchController(ApplicationDbContext db) => _db = db;

		public async Task<IActionResult> Index(
	string? q,
	int? categoryId,
	int? category,
	string? status,
	string? tag,
	int page = 1, int pageSize = 12)
		{
			var catId = categoryId ?? category;
			var now = DateTime.UtcNow;
			q = q?.Trim();
			tag = tag?.Trim();

			// NGỮ CẢNH NGƯỜI DÙNG
			var uid = User.Identity?.IsAuthenticated == true
				? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
				: null;
			var isAdmin = User.IsInRole("Admin");

			// Base: truy vấn theo Item như bạn đang làm
			var query = _db.Items.AsNoTracking()
				.Include(i => i.Images)
				.Include(i => i.Sessions)
				.Include(i => i.ItemTags).ThenInclude(it => it.Tag)
				.AsQueryable();

			// Tìm theo tiêu đề/mã
			if (!string.IsNullOrEmpty(q))
				query = query.Where(i =>
					EF.Functions.Like(i.Title, $"%{q}%") ||
					EF.Functions.Like(i.AssetCode, $"%{q}%"));

			// Lọc theo tag
			if (!string.IsNullOrEmpty(tag))
			{
				string Slugify(string s)
				{
					s = s.Trim().ToLowerInvariant();
					foreach (var ch in new[] { ' ', '_', '.', ',', ';', '/', '\\', ':' })
						s = s.Replace(ch, '-');
					while (s.Contains("--")) s = s.Replace("--", "-");
					return s.Trim('-');
				}
				var slug = Slugify(tag);

				query = query.Where(i => i.ItemTags.Any(t =>
					t.Tag.Name == tag ||
					t.Tag.Slug == slug));
			}

			// Keyword mở rộng (như cũ)
			if (!string.IsNullOrEmpty(q))
			{
				var exact = q.ToLowerInvariant();
				query = query.Where(i =>
					EF.Functions.Like(i.Title, $"%{q}%") ||
					EF.Functions.Like(i.AssetCode, $"%{q}%") ||
					EF.Functions.Like(i.DescriptionHtml ?? "", $"%{q}%") ||
					i.AssetCode.ToLower() == exact
				);
			}

			if (catId.HasValue)
				query = query.Where(i => i.CategoryId == catId.Value);

			if (!string.IsNullOrEmpty(status))
			{
				var st = status.ToLowerInvariant();
				query = st switch
				{
					"live" => query.Where(i => i.Sessions.Any(s =>
						// vis
						(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
							s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
						// điều kiện live
						&& s.Status == AuctionSessionStatus.Live
						&& s.StartUtc <= now && s.EndUtc > now)),

					"upcoming" => query.Where(i => i.Sessions.Any(s =>
						(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
							s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
						&& s.Status == AuctionSessionStatus.Scheduled
						&& s.StartUtc > now)),

					"ended" => query.Where(i => i.Sessions.Any(s =>
						(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
							s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
						&& s.EndUtc <= now)),

					_ => query
				};
			}

			var total = await query.CountAsync();

			// ====== SORT: cũng chỉ dựa trên các phiên nhìn thấy được ======
			IOrderedQueryable<AuctionItem> ordered = query.OrderByDescending(i => i.CreatedAt);

			if (string.Equals(status, "live", StringComparison.OrdinalIgnoreCase))
			{
				ordered = query.OrderBy(i => i.Sessions
					.Where(s =>
						(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
							s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
						&& s.EndUtc > now)
					.Select(s => s.EndUtc)
					.DefaultIfEmpty(DateTime.MaxValue)
					.Min());
			}
			else if (string.Equals(status, "upcoming", StringComparison.OrdinalIgnoreCase))
			{
				ordered = query.OrderBy(i => i.Sessions
					.Where(s =>
						(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
							s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
						&& s.StartUtc > now)
					.Select(s => s.StartUtc)
					.DefaultIfEmpty(DateTime.MaxValue)
					.Min());
			}

			// ====== PROJECTION: chỉ dùng các phiên nhìn thấy được ======
			var model = await ordered
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(i => new AuctionCardVM
				{
					ItemId = i.Id,
					Title = i.Title,
					ThumbnailUrl = i.Images
						.OrderBy(im => im.SortOrder)
						.Select(im => im.FilePath)
						.FirstOrDefault() ?? "/images/placeholder.png",

					Status =
						i.Sessions.Any(s =>
							(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
								s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now)))))
							&& i.Sessions.Any(s =>
								(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
									s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
								&& s.Status == AuctionSessionStatus.Live
								&& s.StartUtc <= now && s.EndUtc > now)
							? "Live"
						: i.Sessions.Any(s =>
								(!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
									s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
								&& s.StartUtc > now)
							? "Upcoming"
						: "Ended",
					EndUtc =
						i.Sessions
							.Where(s => (!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
											s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now))))
										&& s.EndUtc > now)
							.OrderBy(s => s.EndUtc)
							.Select(s => (DateTime?)s.EndUtc)
							.FirstOrDefault()
						?? i.Sessions
							.Where(s => (!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
											s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now)))))
							.OrderByDescending(s => s.EndUtc)
							.Select(s => (DateTime?)s.EndUtc)
							.FirstOrDefault(),

					// SQLite-safe: Max double? rồi cast về decimal — chỉ trên các phiên visible
					CurrentPrice = (decimal)(
						i.Sessions
							.Where(s => (!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
											s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now)))))
							.SelectMany(s => s.Bids)
							.Select(b => (double?)b.Amount)
							.Max()
						?? (double)i.StartingPrice
					),

					// (tuỳ chọn) để hiển thị badge "Riêng tư" nếu người xem có quyền thấy phiên private
					IsPrivate = i.Sessions
						.Where(s => (!s.IsPrivate || isAdmin || (uid != null && (i.SellerId == uid ||
										s.Invites.Any(iv => iv.InviteeUserId == uid && iv.ExpiresAt > now)))))
						.OrderByDescending(s => s.EndUtc)
						.Select(s => s.IsPrivate)
						.FirstOrDefault()
				})
				.ToListAsync();

			ViewBag.Page = page;
			ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
			ViewBag.SelectedCategoryId = catId;
			ViewBag.Categories = await _db.Categories.AsNoTracking()
													 .OrderBy(c => c.Name)
													 .ToListAsync();

			return View(model);
		}
	}
}