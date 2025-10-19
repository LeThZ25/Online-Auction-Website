using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class AuctionItemCreateVM
	{
		// ----- Item -----
		[Required, StringLength(200)]
		[Display(Name = "Tiêu đề")]
		public string Title { get; set; } = string.Empty;

		[Required(ErrorMessage = "Vui lòng nhập mô tả")]
		[Display(Name = "Mô tả chi tiết")]
		public string Description { get; set; } = string.Empty;

		[Required, StringLength(50)]
		[Display(Name = "Mã tài sản")]
		public string AssetCode { get; set; } = string.Empty;

		[Range(0, double.MaxValue)]
		[Display(Name = "Giá khởi điểm")]
		public decimal StartingPrice { get; set; }

		[Range(0, double.MaxValue)]
		[Display(Name = "Giá dự phòng")]
		public decimal? ReservePrice { get; set; }

		[Required]
		[Display(Name = "Danh mục")]
		public int CategoryId { get; set; }

		// ----- Session time -----
		[Required]
		[Display(Name = "Thời gian bắt đầu đấu giá (UTC)")]
		public DateTime AuctionStartUtc { get; set; }

		[Required]
		[Display(Name = "Thời gian kết thúc đấu giá (UTC)")]
		public DateTime AuctionEndUtc { get; set; }

		// ----- Bidding config -----
		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Bước giá phải lớn hơn 0")]
		[Display(Name = "Bước giá")]
		public decimal MinIncrement { get; set; } = 0m;

		[Range(0, double.MaxValue)]
		[Display(Name = "Tiền đặt trước")]
		public decimal DepositAmount { get; set; } = 0m;

		[Range(0, 600)]
		[Display(Name = "Độ trễ mỗi lần đặt giá (giây)")]
		public int BidCooldownSeconds { get; set; } = 3;

		// ----- Anti-sniping -----
		[Display(Name = "Bật chống phá giá giây cuối (anti-sniping)")]
		public bool EnableAntiSniping { get; set; } = false;

		[Range(0, 600)]
		[Display(Name = "Cửa sổ kiểm tra (giây)")]
		public int ExtendWindowSeconds { get; set; } = 30; // nếu còn <=30s thì gia hạn

		[Range(0, 600)]
		[Display(Name = "Gia hạn thêm (giây)")]
		public int ExtendBySeconds { get; set; } = 120;

		[Range(0, 20)]
		[Display(Name = "Số lần gia hạn tối đa")]
		public int ExtendMaxCount { get; set; } = 3;

		// ----- Tags + uploads -----
		[Display(Name = "Tags (phân tách bởi dấu phẩy)")]
		public string? Tags { get; set; }

		[Display(Name = "Tài liệu liên quan")]
		public List<IFormFile> Documents { get; set; } = new();

		[Required, StringLength(200)]
		[Display(Name = "Tổ chức đấu giá tài sản")]
		public string OrganizationName { get; set; } = string.Empty;

		[StringLength(200)]
		[Display(Name = "Đấu giá viên")]
		public string? AuctioneerName { get; set; }

		[Required, StringLength(300)]
		[Display(Name = "Địa chỉ")]
		public string OrganizationAddress { get; set; } = string.Empty;

		[Display(Name = "Hình ảnh sản phẩm")]
		public List<IFormFile> Images { get; set; } = new();
		public bool IsPrivate { get; set; } = false;
		public string? InviteUserEmails { get; set; }
	}
}