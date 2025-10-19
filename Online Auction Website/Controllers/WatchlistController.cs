using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class WatchlistController : Controller
	{
		private readonly ApplicationDbContext _db;
		public WatchlistController(ApplicationDbContext db) { _db = db; }

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Toggle(int itemId)
		{
			var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var row = await _db.Watchlists.FirstOrDefaultAsync(w => w.ItemId == itemId && w.UserId == uid);
			if (row == null)
			{
				_db.Watchlists.Add(new Watchlist { ItemId = itemId, UserId = uid });
				await _db.SaveChangesAsync();
				return Json(new { ok = true, watching = true });
			}
			else
			{
				_db.Watchlists.Remove(row);
				await _db.SaveChangesAsync();
				return Json(new { ok = true, watching = false });
			}
		}
	}
}