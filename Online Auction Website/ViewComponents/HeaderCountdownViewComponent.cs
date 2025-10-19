using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;

public class HeaderCountdownViewComponent : ViewComponent
{
	private readonly ApplicationDbContext _db;
	public HeaderCountdownViewComponent(ApplicationDbContext db) => _db = db;

	public async Task<IViewComponentResult> InvokeAsync()
	{
		var now = DateTime.UtcNow;

		// 1) nearest LIVE auction end
		var liveEnd = await _db.Sessions.AsNoTracking()
			.Where(s => s.Status == AuctionSessionStatus.Live && s.EndUtc > now)
			.OrderBy(s => s.EndUtc)
			.Select(s => (DateTime?)s.EndUtc)
			.FirstOrDefaultAsync();

		if (liveEnd.HasValue)
			return View(new HeaderCountdownVM { TargetUtc = liveEnd, Mode = "end" });

		// 2) or next SCHEDULED auction start
		var nextStart = await _db.Sessions.AsNoTracking()
			.Where(s => s.Status == AuctionSessionStatus.Scheduled && s.StartUtc > now)
			.OrderBy(s => s.StartUtc)
			.Select(s => (DateTime?)s.StartUtc)
			.FirstOrDefaultAsync();

		if (nextStart.HasValue)
			return View(new HeaderCountdownVM { TargetUtc = nextStart, Mode = "start" });

		// 3) fallback to normal clock
		return View(new HeaderCountdownVM { TargetUtc = null, Mode = "clock" });
	}
}