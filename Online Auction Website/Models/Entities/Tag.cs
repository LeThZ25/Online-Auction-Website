using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class Tag
	{
		public int Id { get; set; }

		[Required, MaxLength(60)]
		public string Name { get; set; } = "";

		[Required, MaxLength(80)]
		public string Slug { get; set; } = "";
		public ICollection<AuctionItemTag> ItemTags { get; set; }
	}

	public class ItemTag
	{
		public int ItemId { get; set; }
		public int TagId { get; set; }

		public AuctionItem Item { get; set; } = null!;
		public Tag Tag { get; set; } = null!;
	}
}