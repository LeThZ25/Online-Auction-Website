# Online Auction Website

ASP.NET Core MVC auction site with:
- Realtime bidding & private sessions (whitelist/invite)
- AI assistant (Gemini 2.5 Flash) for chat + description suggestions
- EF Core + Identity

## Prereqs
- .NET 8 SDK
- SQL Server LocalDB (or your DB)
- (Optional) ImageMagick/Ghostscript if you process PDFs/images

## Quick start
```bash
# restore & build
dotnet restore
dotnet build

# set connection string (or use appsettings.Development.json locally)
# example for LocalDB in appsettings.Development.json:
# "ConnectionStrings": { "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AuctionDb;Trusted_Connection=True;MultipleActiveResultSets=true" }

# apply EF Core migrations
dotnet ef database update --project "Online Auction Website"

# run
dotnet run --project "Online Auction Website"
