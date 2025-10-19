// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OnlineAuctionWebsite.Models.Entities;

namespace Online_Auction_Website.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public IndexModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }
		public string Username { get; set; } = "";

		[BindProperty]
		public InputModel Input { get; set; } = new();

		public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

			[Display(Name = "Ngân sách trần (đ)")]
			[Range(0, double.MaxValue, ErrorMessage = "Giá trị không hợp lệ.")]
			public decimal? BudgetCeiling { get; set; }

			[Display(Name = "Chỉ xem khi vượt ngân sách (không cho đặt giá)")]
			public bool ViewOnlyWhenOverBudget { get; set; }
		}

        private async Task LoadAsync(AppUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
				BudgetCeiling = user.BudgetCeiling,
				ViewOnlyWhenOverBudget = user.ViewOnlyWhenOverBudget
			};
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

			user.BudgetCeiling = Input.BudgetCeiling;
			user.ViewOnlyWhenOverBudget = Input.ViewOnlyWhenOverBudget;

			var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }
			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
			{
				foreach (var e in result.Errors)
					ModelState.AddModelError(string.Empty, e.Description);

				await LoadAsync(user);
				return Page();
			}
			await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
