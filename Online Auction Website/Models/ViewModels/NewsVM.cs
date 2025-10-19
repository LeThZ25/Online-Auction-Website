// Models/ViewModels/NewsListItemVM.cs
namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class NewsListItemVM
	{
		public int Id { get; set; }
		public string Title { get; set; } = "";
		public string Excerpt { get; set; } = "";
		public DateTime CreatedAt { get; set; }
	}

	public class NewsIndexVM
	{
		public IEnumerable<NewsListItemVM> Items { get; set; }
			= Enumerable.Empty<NewsListItemVM>();
		public int Page { get; set; }
		public int TotalPages { get; set; }
		public string? Q { get; set; }
	}

	public class NewsDetailVM
	{
		public int Id { get; set; }
		public string Title { get; set; } = "";
		public string ContentHtml { get; set; } = "";
		public DateTime CreatedAt { get; set; }
	}
}
