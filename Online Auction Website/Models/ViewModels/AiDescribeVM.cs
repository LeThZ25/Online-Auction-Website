using Microsoft.AspNetCore.Http;

namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class AiDescribeVM
	{
		public string? Title { get; set; }
		public string? CategoryName { get; set; }
		public decimal? StartingPrice { get; set; }

		// Thông tin tuỳ chọn – nếu form bạn có thì JS sẽ gửi kèm
		public string? Condition { get; set; }
		public string? Brand { get; set; }
		public int? Year { get; set; }
		public string? Material { get; set; }
		public string? Dimensions { get; set; }
		public string? Location { get; set; }
		public string? Notes { get; set; }

		// Ảnh chưa lưu – nhận từ input type="file"
		public List<IFormFile> Images { get; set; } = new();
	}
}