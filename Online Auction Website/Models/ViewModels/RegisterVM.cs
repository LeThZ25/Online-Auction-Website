using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Models.ViewModels
{
	public class RegisterVM
	{
		[Display(Name = "Loại tài khoản")]
		public AccountType AccountType { get; set; } = AccountType.Individual;

		// Cá nhân
		[Display(Name = "Họ")] public string? LastName { get; set; }
		[Display(Name = "Tên đệm")] public string? MiddleName { get; set; }
		[Display(Name = "Tên")] public string? FirstName { get; set; }

		// Tổ chức
		[Display(Name = "Tên tổ chức")] public string? OrganizationName { get; set; }

		// Đăng nhập
		[Required, EmailAddress] public string Email { get; set; } = string.Empty;
		[Phone, Display(Name = "Số điện thoại")] public string? PhoneNumber { get; set; }
		[Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = string.Empty;
		[Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Nhập lại mật khẩu")] public string ConfirmPassword { get; set; } = string.Empty;

		// Thông tin cá nhân
		[Display(Name = "Giới tính")] public Gender Gender { get; set; } = Gender.Unspecified;
		[DataType(DataType.Date), Display(Name = "Ngày sinh")] public DateTime? BirthDate { get; set; }
		[Display(Name = "Tỉnh/Thành phố")] public string? Province { get; set; }
		[Display(Name = "Quận/Huyện")] public string? District { get; set; }
		[Display(Name = "Xã/Phường")] public string? Ward { get; set; }
		[Display(Name = "Địa chỉ chi tiết")] public string? AddressLine { get; set; }

		// CCCD/CMND
		[Display(Name = "Số CCCD/CMND")] public string? IdNumber { get; set; }
		[DataType(DataType.Date), Display(Name = "Ngày cấp")] public DateTime? IdIssueDate { get; set; }
		[Display(Name = "Nơi cấp")] public string? IdIssuePlace { get; set; }
		[Display(Name = "Ảnh mặt trước CCCD")] public IFormFile? IdFront { get; set; }
		[Display(Name = "Ảnh mặt sau CCCD")] public IFormFile? IdBack { get; set; }

		// Ngân hàng
		[Display(Name = "Ngân hàng")] public string? BankName { get; set; }
		[Display(Name = "Chi nhánh ngân hàng")] public string? BankBranch { get; set; }
		[Display(Name = "Số tài khoản")] public string? BankAccountNumber { get; set; }
		[Display(Name = "Chủ tài khoản")] public string? BankAccountHolder { get; set; }

		[Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý điều khoản.")]
		public bool AcceptTerms { get; set; } = false;

		public string? ReturnUrl { get; set; }
	}
}