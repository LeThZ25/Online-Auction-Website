using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineAuctionWebsite.Models.Entities
{
	public enum AccountType { Individual = 1, Organization = 2 }
	public enum Gender { Unspecified = 0, Male = 1, Female = 2, Other = 3 }
	public class AppUser : IdentityUser
	{
		public string? DisplayName { get; set; }
		public bool IsVerified { get; set; }
		public AccountType AccountType { get; set; } = AccountType.Individual;

		[MaxLength(150)] public string? FirstName { get; set; }
		[MaxLength(150)] public string? MiddleName { get; set; }
		[MaxLength(150)] public string? LastName { get; set; }
		[MaxLength(200)] public string? OrganizationName { get; set; }

		public Gender Gender { get; set; } = Gender.Unspecified;
		public DateTime? BirthDate { get; set; }

		[MaxLength(200)] public string? Province { get; set; }
		[MaxLength(200)] public string? District { get; set; }
		[MaxLength(200)] public string? Ward { get; set; }
		[MaxLength(300)] public string? AddressLine { get; set; }

		[MaxLength(50)] public string? IdNumber { get; set; }
		public DateTime? IdIssueDate { get; set; }
		[MaxLength(200)] public string? IdIssuePlace { get; set; }
		[MaxLength(300)] public string? IdFrontPath { get; set; }
		[MaxLength(300)] public string? IdBackPath { get; set; }

		[MaxLength(200)] public string? BankName { get; set; }
		[MaxLength(200)] public string? BankBranch { get; set; }
		[MaxLength(50)] public string? BankAccountNumber { get; set; }
		[MaxLength(200)] public string? BankAccountHolder { get; set; }
		[Range(0, double.MaxValue)]
		public decimal? BudgetCeiling { get; set; }
		public bool ViewOnlyWhenOverBudget { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<AuctionRegistration> Registrations { get; set; } = new List<AuctionRegistration>();
		public ICollection<Bid> Bids { get; set; } = new List<Bid>();
		public ICollection<AuctionItem> Items { get; set; } = new List<AuctionItem>();
	}
}