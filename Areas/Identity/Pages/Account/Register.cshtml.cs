// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Data;
using Microsoft.Extensions.Localization;
using Engineer_MVC.Models.ViewModels;
using RazorLight;
using Engineer_MVC.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Engineer_MVC.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IUserStore<User> _userStore;
		private readonly IUserEmailStore<User> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly IEmailSender _emailSender;
		private readonly IUserService _userService;
		private readonly IStringLocalizer<SharedResource> _sharedResource;
        private readonly EngineerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        public RegisterModel(
			UserManager<User> userManager,
			IUserStore<User> userStore,
			SignInManager<User> signInManager,
			ILogger<RegisterModel> logger,
			IEmailSender emailSender,
			IStringLocalizer<SharedResource> sharedResource,
			IUserService userService,
            EngineerContext context,
			IWebHostEnvironment hostEnvironment)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
			_sharedResource = sharedResource;
			_userService = userService;
			_context = context;
			_hostEnvironment = hostEnvironment;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }
		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{

			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }
			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }
			[Required]
			[Display(Name = "First Name")]
			public string FirstName { get; set; }

			[Required]
			[Display(Name = "Last Name")]
			public string LastName { get; set; }
			[Phone]
			[Display(Name = "Phone number")]
			public string PhoneNumber { get; set; }
		}


		public async Task OnGetAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl;
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
			if (ModelState.IsValid)
			{

				var user = CreateUser();
				user.FirstName = Input.FirstName;
				user.LastName = Input.LastName;
				user.ImagePath = "default.jpg";

				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
				var result = await _userManager.CreateAsync(user, Input.Password);
				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(user, "User");
					_logger.LogInformation("User created a new account with password.");

					var userId = await _userManager.GetUserIdAsync(user);
					var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
					code = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));
					var callbackUrl = Url.Page(
						"/Account/ConfirmEmail",
						pageHandler: null,
						values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
						protocol: Request.Scheme);

                    var templatePath = "Views/Templates/ConfirmEmailTemplate.cshtml";
                    var template = System.IO.File.ReadAllText(templatePath).ToString();
                    var translationTemplate = new TranslationTemplate(_context, _hostEnvironment, _sharedResource);
                    var translatedTemplateResult = translationTemplate.Translate(template);

                    var translatedTemplate = ((ContentResult)translatedTemplateResult).Content;

                    var engine = new RazorLightEngineBuilder()
						.UseMemoryCachingProvider()
						.Build();
                    var model = new EmailConfirmationModel
                    {
                        CallbackUrl = callbackUrl
                    };
                    var resultTemplate = await engine.CompileRenderStringAsync("templateKey", translatedTemplate, model, null);

                    var message = new Message(new string[] { Input.Email }, _sharedResource["ConfirmYourEmail"], resultTemplate);
                    _emailSender.SendEmailAsync(message);

					return RedirectToAction("Index","Home");
					
				}
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}
			return Page();
		}

		private User CreateUser()
		{
			try
			{
				var user = Activator.CreateInstance<User>();
				user.FirstName = Input.FirstName;
				user.LastName = Input.LastName;
				user.PhoneNumber = Input.PhoneNumber;
				return user;
			}
			catch
			{
				throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
					$"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
					$"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
			}
		}

		private IUserEmailStore<User> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<User>)_userStore;
		}
	}
}
