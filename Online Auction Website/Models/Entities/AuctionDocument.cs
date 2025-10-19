using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public class AuctionDocument
	{
		public int Id { get; set; }
		public int? ItemId { get; set; }
		public AuctionItem? Item { get; set; }
		public int? SessionId { get; set; }
		public AuctionSession? Session { get; set; }
		public string FileName { get; set; } = "";
		public string FilePath { get; set; } = "";
		public string? ContentType { get; set; }
		public long Size { get; set; }
		public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
	}
}