using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;

namespace OnlineAuctionWebsite.Helpers
{
	public static class CardQuery
	{
		public static IQueryable<AuctionCardVM> ToAuctionCards(this IQueryable<AuctionItem> q)
		{
			var now = DateTime.UtcNow;

			return q.AsNoTracking()
				.Select(i => new AuctionCardVM
				{
					ItemId = i.Id,
					Title = i.Title,

					ThumbnailUrl = i.Images
						.OrderBy(im => im.SortOrder)
						.Select(im => im.FilePath)
						.FirstOrDefault() ?? "/images/placeholder.png",
					Status = i.Sessions.Any(s => s.Status == AuctionSessionStatus.Live) ? "Live"
						   : i.Sessions.Any(s => s.StartUtc > now) ? "Upcoming"
						   : "Ended",
					EndUtc = i.Sessions
						.Select(s => (DateTime?)s.EndUtc)
						.OrderByDescending(x => x)
						.FirstOrDefault(),
					CurrentPrice =
						i.Sessions
						 .Select(s => (decimal?)s.CurrentPrice)
						 .Where(p => p != null)
						 .OrderByDescending(p => p)
						 .FirstOrDefault()
					  ?? i.Sessions
						 .SelectMany(s => s.Bids)
						 .Select(b => (decimal?)b.Amount)
						 .OrderByDescending(a => a)
						 .FirstOrDefault()
					  ?? i.StartingPrice,

					Tags = i.ItemTags.Select(it => it.Tag.Name).ToList()
				});
		}
	}
}