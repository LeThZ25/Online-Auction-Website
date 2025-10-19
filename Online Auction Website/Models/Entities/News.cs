using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models
{
	public class News
	{
		public int Id { get; set; }

		[Required, StringLength(200)]
		public string Title { get; set; } = string.Empty;

		[Required]
		public string Content { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}