using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;

[Authorize(Roles = "Admin")]
public class NewsController : Controller
{
	private readonly ApplicationDbContext _db;
	public NewsController(ApplicationDbContext db) => _db = db;

	// ADMIN list
	public async Task<IActionResult> Index()
	{
		var items = await _db.News
			.OrderByDescending(n => n.CreatedAt)
			.ToListAsync();
		return View(items);             // <-- IEnumerable<News>
	}

	public IActionResult Create() => View();

	[HttpPost, ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(News model)
	{
		if (!ModelState.IsValid) return View(model);
		_db.News.Add(model);
		await _db.SaveChangesAsync();
		TempData["Success"] = "Đã tạo tin tức.";
		return RedirectToAction(nameof(Index));
	}

	public async Task<IActionResult> Edit(int id)
	{
		var news = await _db.News.FindAsync(id);
		return news == null ? NotFound() : View(news);
	}

	[HttpPost, ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, News model)
	{
		if (id != model.Id) return BadRequest();
		if (!ModelState.IsValid) return View(model);
		_db.Update(model);
		await _db.SaveChangesAsync();
		TempData["Success"] = "Đã cập nhật.";
		return RedirectToAction(nameof(Index));
	}

	public async Task<IActionResult> Delete(int id)
	{
		var news = await _db.News.FindAsync(id);
		return news == null ? NotFound() : View(news);
	}

	[HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteConfirmed(int id)
	{
		var news = await _db.News.FindAsync(id);
		if (news != null)
		{
			_db.News.Remove(news);
			await _db.SaveChangesAsync();
		}
		TempData["Success"] = "Đã xoá.";
		return RedirectToAction(nameof(Index));
	}
}