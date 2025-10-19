using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Services
{
	// Services/AuctionSessionWorker.cs
	public class AuctionSessionWorker : BackgroundService
	{
		private readonly IServiceProvider _sp;
		private readonly ILogger<AuctionSessionWorker> _logger;
		public AuctionSessionWorker(IServiceProvider sp, ILogger<AuctionSessionWorker> logger)
		{ _sp = sp; _logger = logger; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _sp.CreateScope();
					var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
					var now = DateTime.UtcNow;

					var scheduled = await db.Sessions
						.Where(s => s.Status == AuctionSessionStatus.Scheduled && s.StartUtc <= now)
						.ToListAsync(stoppingToken);
					foreach (var s in scheduled) s.Status = AuctionSessionStatus.Live;

					var live = await db.Sessions
						.Where(s => s.Status == AuctionSessionStatus.Live && s.EndUtc <= now)
						.ToListAsync(stoppingToken);
					foreach (var s in live) s.Status = AuctionSessionStatus.Ended;

					if (scheduled.Any() || live.Any())
						await db.SaveChangesAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "AuctionSessionWorker tick failed");
				}

				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
			}
		}
	}
}