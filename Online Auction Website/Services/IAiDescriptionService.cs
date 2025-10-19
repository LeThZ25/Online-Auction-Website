using System.Threading;
using System.Threading.Tasks;
using OnlineAuctionWebsite.Models.ViewModels;

namespace OnlineAuctionWebsite.Services
{
	public class AiDescriptionResult
	{
		public string DescriptionHtml { get; set; } = "";
		public List<string> Tags { get; set; } = new();
	}

	public interface IAiDescriptionService
	{
		Task<AiDescriptionResult> GenerateAsync(AiDescribeVM vm, CancellationToken ct = default);
	}
}