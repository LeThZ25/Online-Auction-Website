using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public enum AuctionItemStatus { Draft, Published, Closed }

	public class AuctionItem
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
		[StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
		[Display(Name = "Tiêu đề")]
		public string Title { get; set; } = string.Empty;

		[Display(Name = "Mô tả ngắn")]
		public string? Description { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập giá khởi điểm")]
		[Range(1, double.MaxValue, ErrorMessage = "Giá khởi điểm phải lớn hơn 0")]
		[Display(Name = "Giá khởi điểm")]
		public decimal StartingPrice { get; set; }

		[Range(0, double.MaxValue, ErrorMessage = "Giá dự phòng không hợp lệ")]
		[Display(Name = "Giá dự phòng")]
		public decimal? ReservePrice { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn danh mục")]
		[Display(Name = "Danh mục")]
		public int CategoryId { get; set; }
		public AuctionCategory Category { get; set; } = default!;

		[Required(ErrorMessage = "Người bán là bắt buộc")]
		[Display(Name = "Mã người bán")]
		public string SellerId { get; set; } = string.Empty;

		public AppUser Seller { get; set; } = default!;

		[Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
		[Display(Name = "Thời gian bắt đầu đấu giá (UTC)")]
		public DateTime AuctionStartUtc { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
		[Display(Name = "Thời gian kết thúc đấu giá (UTC)")]
		public DateTime AuctionEndUtc { get; set; }

		[Display(Name = "Mô tả chi tiết")]
		public string? DescriptionHtml { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập mã tài sản")]
		[StringLength(50, ErrorMessage = "Mã tài sản tối đa 50 ký tự")]
		[Display(Name = "Mã tài sản")]
		public string AssetCode { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public AuctionItemStatus Status { get; set; } = AuctionItemStatus.Published;

		public ICollection<AuctionSession> Sessions { get; set; } = new List<AuctionSession>();
		public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();
		public ICollection<AuctionItemTag> ItemTags { get; set; } = new List<AuctionItemTag>();
		public ICollection<AuctionDocument> Documents { get; set; }
	}
}