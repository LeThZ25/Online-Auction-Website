using System.Threading;
using System.Threading.Tasks;

namespace OnlineAuctionWebsite.Services
{
	public interface IBidEngine
	{
		Task<(bool ok, string? error)> PlaceBidAsync(int sessionId, string userId, decimal amount, CancellationToken ct = default);
		Task<(bool ok, string? error)> SetAutoBidAsync(int sessionId, string userId, decimal maxAmount, CancellationToken ct = default);
	}
}