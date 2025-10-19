using Microsoft.AspNetCore.SignalR;

namespace OnlineAuctionWebsite.Hubs
{
	public class AuctionHub : Hub
	{
		public override async Task OnConnectedAsync()
		{
			var http = Context.GetHttpContext();
			if (http!.Request.Query.TryGetValue("sessionId", out var sid))
				await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sid}");
			await base.OnConnectedAsync();
		}
		// Client gọi khi vào trang chi tiết
		public Task JoinSession(int sessionId) =>
			Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");

		// Tuỳ chọn: nếu muốn group theo item
		public Task JoinItem(int itemId) =>
			Groups.AddToGroupAsync(Context.ConnectionId, $"item-{itemId}");
	}
}