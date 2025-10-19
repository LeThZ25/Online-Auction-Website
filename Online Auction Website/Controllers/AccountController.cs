using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineAuctionWebsite.Models.Entities;
using OnlineAuctionWebsite.Models.ViewModels;

namespace OnlineAuctionWebsite.Controllers
{
	[AllowAnonymous]
	public class AccountController : Controller
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signInManager;
		private readonly IWebHostEnvironment _env;

		public AccountController(
			UserManager<AppUser> userManager,
			SignInManager<AppUser> signInManager,
			IWebHostEnvironment env)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_env = env;
		}

		// --- Register ----------------------------------------------------------
		[HttpGet]
		public IActionResult Register(string? returnUrl = null)
			=> View(new RegisterVM { ReturnUrl = returnUrl });

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterVM vm)
		{
			if (!ModelState.IsValid) return View(vm);

			// simple required check depending on type
			if (vm.AccountType == AccountType.Organization && string.IsNullOrWhiteSpace(vm.OrganizationName))
				ModelState.AddModelError(nameof(vm.OrganizationName), "Vui lòng nhập tên tổ chức.");

			if (!ModelState.IsValid) return View(vm);

			// Save uploads
			async Task<string?> SaveAsync(IFormFile? f, string folder)
			{
				if (f == null || f.Length == 0) return null;
				var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
				Directory.CreateDirectory(dir);
				var fileName = $"{Guid.NewGuid()}{Path.GetExtension(f.FileName)}";
				var path = Path.Combine(dir, fileName);
				using (var stream = System.IO.File.Create(path))
					await f.CopyToAsync(stream);
				return $"/uploads/{folder}/{fileName}";
			}

			var idFront = await SaveAsync(vm.IdFront, "ids");
			var idBack = await SaveAsync(vm.IdBack, "ids");

			var user = new AppUser
			{
				UserName = vm.Email,
				Email = vm.Email,
				PhoneNumber = vm.PhoneNumber,

				AccountType = vm.AccountType,
				FirstName = vm.FirstName,
				MiddleName = vm.MiddleName,
				LastName = vm.LastName,
				OrganizationName = vm.OrganizationName,

				Gender = vm.Gender,
				BirthDate = vm.BirthDate,
				Province = vm.Province,
				District = vm.District,
				Ward = vm.Ward,
				AddressLine = vm.AddressLine,

				IdNumber = vm.IdNumber,
				IdIssueDate = vm.IdIssueDate,
				IdIssuePlace = vm.IdIssuePlace,
				IdFrontPath = idFront,
				IdBackPath = idBack,

				BankName = vm.BankName,
				BankBranch = vm.BankBranch,
				BankAccountNumber = vm.BankAccountNumber,
				BankAccountHolder = vm.BankAccountHolder
			};

			var result = await _userManager.CreateAsync(user, vm.Password);
			if (!result.Succeeded)
			{
				foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
				return View(vm);
			}

			await _signInManager.SignInAsync(user, isPersistent: false);
			return LocalRedirect(vm.ReturnUrl ?? Url.Content("~/"));
		}

		// --- External login (Google / Facebook) -------------------------------
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ExternalLogin(string provider, string? returnUrl = null)
		{
			var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
			var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!);
			return Challenge(props, provider);
		}

		[HttpGet]
		public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
		{
			returnUrl ??= Url.Content("~/");
			if (remoteError != null)
			{
				TempData["Error"] = $"Đăng nhập ngoài thất bại: {remoteError}";
				return RedirectToAction(nameof(Login), new { returnUrl });
			}

			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
				return RedirectToAction(nameof(Login), new { returnUrl });

			// Sign-in if user already linked
			var signIn = await _signInManager.ExternalLoginSignInAsync(
				info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
			if (signIn.Succeeded) return LocalRedirect(returnUrl);

			// Create a new user from external info
			var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
			var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

			var newUser = new AppUser
			{
				UserName = email ?? $"{info.LoginProvider}_{info.ProviderKey}",
				Email = email,
				FirstName = name,
				AccountType = AccountType.Individual,
				EmailConfirmed = true
			};

			var create = await _userManager.CreateAsync(newUser);
			if (!create.Succeeded)
			{
				foreach (var e in create.Errors) TempData["Error"] = e.Description;
				return RedirectToAction(nameof(Login));
			}

			var addLogin = await _userManager.AddLoginAsync(newUser, info);
			if (!addLogin.Succeeded)
			{
				foreach (var e in addLogin.Errors) TempData["Error"] = e.Description;
				return RedirectToAction(nameof(Login));
			}

			await _signInManager.SignInAsync(newUser, isPersistent: false);
			return LocalRedirect(returnUrl);
		}

		// keep your existing Login/Logout actions...
		public IActionResult Login(string? returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}
	}
}