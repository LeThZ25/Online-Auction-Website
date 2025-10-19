using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Data;
using OnlineAuctionWebsite.Hubs;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Services;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services
	.AddDefaultIdentity<AppUser>(opt =>
	{
		opt.SignIn.RequireConfirmedAccount = false;
		opt.Password.RequireNonAlphanumeric = false;
		opt.Password.RequireUppercase = false;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSignalR();
builder.Services.AddScoped<IBidEngine, BidEngine>();
builder.Services.AddHostedService<AuctionSessionWorker>();
builder.Services.AddHttpClient();
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables()
	.AddUserSecrets<Program>(optional: true);

var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var fbId = builder.Configuration["Authentication:Facebook:AppId"];
var fbSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var auth = builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
{
	auth.AddGoogle(o =>
	{
		o.ClientId = googleId!;
		o.ClientSecret = googleSecret!;
		o.CallbackPath = "/signin-google"; // default is fine
	});
}
if (!string.IsNullOrWhiteSpace(fbId) && !string.IsNullOrWhiteSpace(fbSecret))
{
	auth.AddFacebook(o =>
	{
		o.AppId = fbId!;
		o.AppSecret = fbSecret!;
		o.CallbackPath = "/signin-facebook"; // default is fine
	});
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<AuctionHub>("/hubs/auction");

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var ctx = services.GetRequiredService<ApplicationDbContext>();
	await ctx.Database.MigrateAsync();
	await SeedData.InitializeAsync(services);
	await SeedData.SeedCategoriesAsync(ctx);
}
app.Run();