using Microsoft.AspNetCore.Mvc;
namespace OnlineAuctionWebsite.Helpers
{
	public static class TimeHelper
	{
		// Cross-platform “ICT” (UTC+7)
		public static TimeZoneInfo ICT =>
			TimeZoneInfo.FindSystemTimeZoneById(
				OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Bangkok");

		public static DateTime ToUtcFromIctLocal(DateTime local)
		{
			// <input type="datetime-local"> arrives with Kind=Unspecified → tell .NET it’s local ICT then convert
			var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
			return TimeZoneInfo.ConvertTimeToUtc(unspecified, ICT);
		}

		public static DateTime ToIctFromUtc(DateTime utc)
			=> TimeZoneInfo.ConvertTimeFromUtc(utc, ICT);

		public static string ToJsIsoUtc(DateTime utc)
			=> utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
	}
}