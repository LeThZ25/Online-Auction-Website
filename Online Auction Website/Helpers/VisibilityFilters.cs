using System;
using System.Linq.Expressions;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Helpers
{
	public static class VisibilityFilters
	{
		public static Expression<Func<AuctionSession, bool>> SessionVisibleTo(string? userId, bool isAdmin, DateTime nowUtc)
		{
			if (isAdmin)
				return s => true;
			if (string.IsNullOrEmpty(userId))
				return s => !s.IsPrivate;
			return s => !s.IsPrivate
						|| s.Item.SellerId == userId
						|| s.Invites.Any(iv => iv.InviteeUserId == userId && iv.ExpiresAt > nowUtc);
		}
	}
}