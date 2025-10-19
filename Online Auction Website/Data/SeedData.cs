using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Data
{
	public static class SeedData
	{
		public static async Task InitializeAsync(IServiceProvider services)
		{
			// Safety: OK if already migrated
			var db = services.GetRequiredService<ApplicationDbContext>();
			await db.Database.MigrateAsync();

			var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
			var userManager = services.GetRequiredService<UserManager<AppUser>>();

			const string adminRole = "Admin";
			const string adminEmail = "admin@auctionsite.local";
			const string adminPass = "Admin@123"; // change in production

			// 1) Role
			if (!await roleManager.RoleExistsAsync(adminRole))
			{
				await roleManager.CreateAsync(new IdentityRole(adminRole));
			}

			// 2) Admin user
			var admin = await userManager.FindByEmailAsync(adminEmail);
			if (admin == null)
			{
				admin = new AppUser
				{
					UserName = adminEmail,
					Email = adminEmail,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(admin, adminPass);
				if (result.Succeeded)
				{
					await userManager.AddToRoleAsync(admin, adminRole);
				}
				else
				{
					throw new Exception("Failed to create admin user: " +
						string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
				}
			}
			else
			{
				// ensure in role if user already existed
				if (!await userManager.IsInRoleAsync(admin, adminRole))
					await userManager.AddToRoleAsync(admin, adminRole);
			}
		}
		public static async Task SeedCategoriesAsync(ApplicationDbContext _db)
		{
			if (!_db.Categories.Any())
			{
				var categories = new List<AuctionCategory>
				{
					new AuctionCategory { Name = "Tài sản nhà nước", Slug = "tai-san-nha-nuoc" },
					new AuctionCategory { Name = "Bất động sản", Slug = "bat-dong-san" },
					new AuctionCategory { Name = "Phương tiện - xe cộ", Slug = "phuong-tien-xe-co" },
					new AuctionCategory { Name = "Sưu tầm - nghệ thuật", Slug = "suu-tam-nghe-thuat" },
					new AuctionCategory { Name = "Hàng hiệu xa xỉ", Slug = "hang-hieu-xa-xi" },
					new AuctionCategory { Name = "Tang vật bị tịch thu", Slug = "tang-vat-bi-tich-thu" },
					new AuctionCategory { Name = "Tài sản khác", Slug = "tai-san-khac" }
				};

				_db.Categories.AddRange(categories);
				await _db.SaveChangesAsync();
			}
		}

	}
}