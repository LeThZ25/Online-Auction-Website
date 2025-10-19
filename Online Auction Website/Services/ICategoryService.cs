namespace OnlineAuctionWebsite.Services
{
	using OnlineAuctionWebsite.Models.Entities;
	public interface ICategoryService
	{
		Task<List<AuctionCategory>> GetAllAsync();
	}
}
