// Controllers/NewsReadController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.ViewModels;

public class NewsReadController : Controller
{
	private readonly ApplicationDbContext _db;
	public NewsReadController(ApplicationDbContext db) => _db = db;

	public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 9)
	{
		var query = _db.News.AsNoTracking();
		if (!string.IsNullOrWhiteSpace(q))
			query = query.Where(n => EF.Functions.Like(n.Title, $"%{q}%"));

		var total = await query.CountAsync();

		var items = await query
			.OrderByDescending(n => n.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(n => new NewsListItemVM
			{
				Id = n.Id,
				Title = n.Title,
				Excerpt = n.Content.Length > 160 ? n.Content.Substring(0, 160) + "…" : n.Content,
				CreatedAt = n.CreatedAt
			})
			.ToListAsync();

		var vm = new NewsIndexVM
		{
			Items = items,
			Page = page,
			TotalPages = (int)Math.Ceiling(total / (double)pageSize),
			Q = q
		};
		return View(vm);
	}

	public async Task<IActionResult> Article(int id)
	{
		var n = await _db.News.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		if (n == null) return NotFound();

		var vm = new NewsDetailVM
		{
			Id = n.Id,
			Title = n.Title,
			ContentHtml = n.Content,
			CreatedAt = n.CreatedAt
		};

		ViewBag.Latest = await _db.News.AsNoTracking()
			.OrderByDescending(x => x.CreatedAt)
			.Take(5)
			.Select(x => new NewsListItemVM { Id = x.Id, Title = x.Title, CreatedAt = x.CreatedAt })
			.ToListAsync();

		return View(vm);
	}
}