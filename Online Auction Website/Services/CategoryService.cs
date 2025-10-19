namespace OnlineAuctionWebsite.Services
{
	using Microsoft.EntityFrameworkCore;
	using Microsoft.Extensions.Caching.Memory;
	using OnlineAuctionWebsite.Models;
	using OnlineAuctionWebsite.Models.Entities;

	public class CategoryService : ICategoryService
	{
		private readonly ApplicationDbContext _db;
		private readonly IMemoryCache _cache;
		public CategoryService(ApplicationDbContext db, IMemoryCache cache)
		{ _db = db; _cache = cache; }

		public Task<List<AuctionCategory>> GetAllAsync() =>
			_cache.GetOrCreateAsync("cats", async e =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
				return await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
			})!;
	}
}
