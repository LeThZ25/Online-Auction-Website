using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Models
{
	public class ApplicationDbContext : IdentityDbContext<AppUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		public DbSet<AuctionCategory> Categories => Set<AuctionCategory>();
		public DbSet<AuctionItem> Items => Set<AuctionItem>();
		public DbSet<AuctionSession> Sessions => Set<AuctionSession>();
		public DbSet<Bid> Bids => Set<Bid>();
		public DbSet<AuctionImage> Images => Set<AuctionImage>();
		public DbSet<News> News => Set<News>();
		public DbSet<AuctionDocument> Documents => Set<AuctionDocument>();
		public DbSet<AuctionRegistration> Registrations => Set<AuctionRegistration>();
		public DbSet<Payment> Payments => Set<Payment>();
		public DbSet<Tag> Tags => Set<Tag>();
		public DbSet<AuctionItemTag> ItemTags => Set<AuctionItemTag>();
		public DbSet<ProxyBid> ProxyBids => Set<ProxyBid>();
		public DbSet<Watchlist> Watchlists => Set<Watchlist>();
		public DbSet<SessionInvite> SessionInvites => Set<SessionInvite>();
		public DbSet<AuctionSessionWhitelist> SessionWhitelists { get; set; } = default!;

		protected override void OnModelCreating(ModelBuilder b)
		{
			base.OnModelCreating(b);

			// Store all DateTime as UTC
			var utc = new ValueConverter<DateTime, DateTime>(
				v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
				v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

			var utcNullable = new ValueConverter<DateTime?, DateTime?>(
				v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
				v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

			foreach (var et in b.Model.GetEntityTypes())
			{
				foreach (var p in et.GetProperties())
				{
					if (p.ClrType == typeof(DateTime)) p.SetValueConverter(utc);
					else if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcNullable);
				}
			}

			// ===== AuctionItem =====
			b.Entity<AuctionItem>(e =>
			{
				e.HasIndex(x => new { x.CategoryId, x.CreatedAt });
				// Keep if AuctionItem has AssetCode; remove if not present.
				e.HasIndex(x => x.AssetCode).IsUnique();

				e.HasOne(i => i.Category)
				 .WithMany(c => c.Items)
				 .HasForeignKey(i => i.CategoryId)
				 .OnDelete(DeleteBehavior.Restrict);

				e.HasOne(i => i.Seller)
				 .WithMany(u => u.Items)
				 .HasForeignKey(i => i.SellerId)
				 .OnDelete(DeleteBehavior.Restrict);

				e.HasMany(i => i.Sessions)
				 .WithOne(s => s.Item)
				 .HasForeignKey(s => s.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasMany(i => i.Images)
				 .WithOne(im => im.Item)
				 .HasForeignKey(im => im.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);

				// NOTE: Documents are mapped under AuctionSession (Option A), so no i.Documents mapping here.
			});

			// ===== AuctionSession =====
			b.Entity<AuctionSession>(e =>
			{
				e.HasIndex(x => new { x.Status, x.StartUtc, x.EndUtc });

				e.HasOne(s => s.Item)
				 .WithMany(i => i.Sessions)
				 .HasForeignKey(s => s.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasMany(s => s.Bids)
				 .WithOne(bid => bid.Session)
				 .HasForeignKey(bid => bid.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasMany(s => s.Registrations)
				 .WithOne(r => r.Session)
				 .HasForeignKey(r => r.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasMany(s => s.Documents)
				 .WithOne(d => d.Session)
				 .HasForeignKey(d => d.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);
			});

			// ===== Bid =====
			b.Entity<Bid>(e =>
			{
				e.HasIndex(x => new { x.SessionId, x.CreatedAt });

				e.HasOne(bid => bid.Session)
				 .WithMany(s => s.Bids)
				 .HasForeignKey(bid => bid.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(bid => bid.Bidder)
				 .WithMany(u => u.Bids)
				 .HasForeignKey(bid => bid.BidderId)
				 .OnDelete(DeleteBehavior.Cascade);
			});

			// ===== AuctionImage =====
			b.Entity<AuctionImage>(e =>
			{
				e.Property(x => x.SortOrder).HasDefaultValue(0);
				e.HasOne(im => im.Item)
				 .WithMany(i => i.Images)
				 .HasForeignKey(im => im.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);
			});

			// ===== AuctionDocument (Option A: belongs to Session only) =====
			b.Entity<AuctionDocument>(e =>
			{
				e.HasOne(d => d.Session)
				 .WithMany(s => s.Documents)
				 .HasForeignKey(d => d.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);
			});

			// ===== News =====
			b.Entity<News>(e =>
			{
				e.HasIndex(x => x.CreatedAt);
			});

			// ===== Registration =====
			b.Entity<AuctionRegistration>(e =>
			{
				e.HasIndex(r => new { r.SessionId, r.UserId }).IsUnique();

				e.HasOne(r => r.Session)
				 .WithMany(s => s.Registrations)
				 .HasForeignKey(r => r.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(r => r.User)
				 .WithMany(u => u.Registrations)
				 .HasForeignKey(r => r.UserId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(r => r.Payment)
				 .WithMany()
				 .HasForeignKey(r => r.PaymentId)
				 .OnDelete(DeleteBehavior.SetNull);
			});

			// ===== Payment =====
			b.Entity<Payment>(e =>
			{
				e.HasIndex(p => new { p.SessionId, p.UserId, p.Status });

				e.HasOne(p => p.Session)
				 .WithMany()
				 .HasForeignKey(p => p.SessionId)
				 .OnDelete(DeleteBehavior.SetNull);

				e.HasOne(p => p.User)
				 .WithMany()
				 .HasForeignKey(p => p.UserId)
				 .OnDelete(DeleteBehavior.SetNull);
			});

			// ===== Tags & join =====
			b.Entity<Tag>(e =>
			{
				e.HasIndex(t => t.Slug).IsUnique();
			});

			b.Entity<AuctionItemTag>(e =>
			{
				e.HasKey(x => new { x.ItemId, x.TagId });

				e.HasOne(x => x.Item)
				 .WithMany(i => i.ItemTags)
				 .HasForeignKey(x => x.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(x => x.Tag)
				 .WithMany(t => t.ItemTags)
				 .HasForeignKey(x => x.TagId)
				 .OnDelete(DeleteBehavior.Cascade);
			});
			// WATCHLIST
			b.Entity<Watchlist>(e =>
			{
				// Khóa tổng hợp để tránh trùng một user “theo dõi” cùng item 2 lần
				e.HasKey(w => new { w.ItemId, w.UserId });

				e.HasOne(w => w.Item)
				 .WithMany()                            // không cần nav ngược
				 .HasForeignKey(w => w.ItemId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(w => w.User)
				 .WithMany()
				 .HasForeignKey(w => w.UserId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasIndex(w => new { w.UserId, w.ItemId });        // truy vấn nhanh
			});

			// PROXY BID
			b.Entity<ProxyBid>(e =>
			{
				// Mỗi user chỉ 1 proxy cho 1 phiên
				e.HasIndex(p => new { p.SessionId, p.UserId }).IsUnique();

				e.HasOne(p => p.Session)
				 .WithMany()                          // không cần nav ngược
				 .HasForeignKey(p => p.SessionId)
				 .OnDelete(DeleteBehavior.Cascade);

				e.HasOne(p => p.User)
				 .WithMany()
				 .HasForeignKey(p => p.UserId)
				 .OnDelete(DeleteBehavior.Cascade);
			});

			b.Entity<SessionInvite>()
				.HasIndex(i => i.Token)
				.IsUnique();

			b.Entity<SessionInvite>()
				.HasOne(i => i.Session)
				.WithMany(s => s.Invites)
				.HasForeignKey(i => i.SessionId)
				.OnDelete(DeleteBehavior.Cascade);

			b.Entity<SessionInvite>()
				.HasOne(i => i.Inviter)
				.WithMany()
				.HasForeignKey(i => i.InviterUserId)
				.OnDelete(DeleteBehavior.Restrict);

			b.Entity<SessionInvite>()
				.HasOne(i => i.Invitee)
				.WithMany()
				.HasForeignKey(i => i.InviteeUserId)
				.OnDelete(DeleteBehavior.Restrict);

			b.Entity<AuctionSessionWhitelist>()
				.HasIndex(w => new { w.SessionId, w.UserId })
				.IsUnique();

			b.Entity<AuctionSessionWhitelist>()
				.HasOne(w => w.Session)
				.WithMany(s => s.Whitelist)
				.HasForeignKey(w => w.SessionId)
				.OnDelete(DeleteBehavior.Cascade);

			b.Entity<AuctionSessionWhitelist>()
				.HasOne(w => w.User)
				.WithMany() // nếu AppUser chưa có collection
				.HasForeignKey(w => w.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			b.Entity<AuctionSessionWhitelist>()
				.HasOne(w => w.AddedBy)
				.WithMany()
				.HasForeignKey(w => w.AddedById)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}