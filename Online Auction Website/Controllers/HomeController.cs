using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Auction_Website.Models;
using OnlineAuctionWebsite.Helpers;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Online_Auction_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }
		public async Task<IActionResult> Index()
		{
			var now = DateTime.UtcNow;
			var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var isAdmin = User.IsInRole("Admin");
			Expression<Func<AuctionSession, bool>> visible = s =>
				!s.IsPrivate
				|| (uid != null && (
					   s.Item.SellerId == uid
					|| s.Invites.Any(i => i.InviteeUserId == uid && i.ExpiresAt > now)
				   ))
				|| isAdmin;
			var live = await _db.Sessions.AsNoTracking()
				.Where(visible)
				.Where(s => s.Status == AuctionSessionStatus.Live)
				.OrderBy(s => s.EndUtc)
				.Select(s => new AuctionCardVM
				{
					ItemId = s.ItemId,
					Title = s.Item.Title,
					ThumbnailUrl = s.Item.Images
						.OrderBy(i => i.SortOrder)
						.Select(i => i.FilePath)
						.FirstOrDefault() ?? "/images/placeholder.png",
					Status = "Live",
					EndUtc = s.EndUtc,
					CurrentPrice = (decimal)(
						s.Bids.Select(b => (double?)b.Amount).Max()
						?? (double)s.Item.StartingPrice
					),
					IsPrivate = s.IsPrivate
				})
				.Take(12)
				.ToListAsync();
			var upcoming = await _db.Sessions.AsNoTracking()
				.Where(visible)
				.Where(s => s.Status == AuctionSessionStatus.Scheduled && s.StartUtc > now)
				.OrderBy(s => s.StartUtc)
				.Select(s => new AuctionCardVM
				{
					ItemId = s.ItemId,
					Title = s.Item.Title,
					ThumbnailUrl = s.Item.Images
						.OrderBy(i => i.SortOrder)
						.Select(i => i.FilePath)
						.FirstOrDefault() ?? "/images/placeholder.png",
					Status = "Upcoming",
					EndUtc = s.EndUtc,
					CurrentPrice = s.Item.StartingPrice,
					IsPrivate = s.IsPrivate
				})
				.Take(8)
				.ToListAsync();
			var newest = await _db.Items.AsNoTracking()
				.OrderByDescending(i => i.CreatedAt)
				.Select(i => new AuctionCardVM
				{
					ItemId = i.Id,
					Title = i.Title,
					ThumbnailUrl = i.Images
						.OrderBy(im => im.SortOrder)
						.Select(im => im.FilePath)
						.FirstOrDefault() ?? "/images/placeholder.png",

					Status =
						i.Sessions.AsQueryable().Where(visible).Any(s => s.Status == AuctionSessionStatus.Live) ? "Live" :
						i.Sessions.AsQueryable().Where(visible).Any(s => s.StartUtc > now) ? "Upcoming" : "Ended",

					EndUtc = i.Sessions.AsQueryable()
						.Where(visible)
						.OrderByDescending(s => s.EndUtc)
						.Select(s => (DateTime?)s.EndUtc)
						.FirstOrDefault(),

					CurrentPrice = (decimal)(
						i.Sessions.AsQueryable()
							.Where(visible)
							.SelectMany(s => s.Bids)
							.Select(b => (double?)b.Amount)
							.Max()
						?? (double)i.StartingPrice
					),
					IsPrivate = i.Sessions.AsQueryable()
						.Where(visible)
						.OrderByDescending(s => s.EndUtc)
						.Select(s => s.IsPrivate)
						.FirstOrDefault()
				})
				.Take(12)
				.ToListAsync();

			var vm = new FeaturedAuctionsVM
			{
				Live = live,
				Upcoming = upcoming,
				Newest = newest
			};

			return View(vm);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
		[HttpGet]
		public IActionResult Contact() => View(new ContactVM());

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Contact(ContactVM vm)
		{
			if (!ModelState.IsValid) return View(vm);

			// TODO: send email or persist (for now just show success)
			TempData["ContactOk"] = "C?m ?n b?n! Chúng tôi ?ã nh?n ???c thông tin.";
			return RedirectToAction(nameof(Contact));
		}
		public IActionResult About() => View();
		// /Home/Article/5
		public async Task<IActionResult> Article(int id)
		{
			var n = await _db.News.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
			if (n == null) return NotFound();

			var vm = new NewsDetailVM
			{
				Id = n.Id,
				Title = n.Title,
				CreatedAt = n.CreatedAt,
				ContentHtml = n.Content ?? ""
			};

			ViewBag.Latest = await _db.News.AsNoTracking()
				.OrderByDescending(x => x.CreatedAt)
				.Take(6)
				.Select(x => new NewsListItemVM { Id = x.Id, Title = x.Title, CreatedAt = x.CreatedAt })
				.ToListAsync();

			return View(vm);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Subscribe(string email)
		{
			if (!string.IsNullOrWhiteSpace(email))
				TempData["Info"] = "C?m ?n b?n ?ã ??ng ký nh?n tin!";
			return RedirectToAction(nameof(Index));
		}
	}
}
