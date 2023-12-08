using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Engineer_MVC.Areas.Identity.Pages.Account.Manage
{
    public class DeleteAccountModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly IUserService _userService;
        public DeleteAccountModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IStringLocalizer<SharedResource> sharedResource,
            IUserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sharedResource = sharedResource;
            _userService = userService;
        }
        public string Username { get; set; }
        public string ImagePath { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel
        {
            [Phone]
            public string PhoneNumber { get; set; }
            public string FirstName { get; set; }

            public string LastName { get; set; }
            public string ImagePath { get; set; }
        }
        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            ImagePath = user.ImagePath;
            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName
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
            var result = await _userManager.DeleteAsync(user);
            if (user.ImagePath != null && user.ImagePath!= "default.jpg") 
            {
                _userService.DeleteImage(user.ImagePath);
            }
            
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                StatusMessage = "Your account has been deleted.";
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page(); 
            }

        }

    }
}
