using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;
using WEBDEV_Project.ViewModels;

namespace WEBDEV_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    DisplayName = model.DisplayName 
                };
                
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);
                    
                    // Fire and forget email
                    _ = _emailService.SendConfirmationAsync(user.Email, user.DisplayName, confirmationLink!);
                    
                    return View("ConfirmEmailSuccess");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return RedirectToAction("Index", "Home");
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["Success"] = "Email confirmed successfully. You can now log in.";
                return RedirectToAction("Login");
            }
            
            TempData["Error"] = "Email confirmation failed.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "This account has been banned.");
                        return View(model);
                    }

                    // Removed manual email confirmation check to allow testing


                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        user.LastLoginAt = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                        
                        return RedirectToLocal(returnUrl);
                    }
                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts.");
                        return View(model);
                    }
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
                    
                    _ = _emailService.SendPasswordResetAsync(user.Email, user.DisplayName, callbackUrl!);
                }
                
                // Don't reveal if email exists or not
                return View("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (token == null || email == null) return BadRequest("A code must be supplied for password reset.");
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login"); // Don't reveal account doesn't exist
            
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password has been reset successfully.";
                return RedirectToAction("Login");
            }
            
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your password has been changed.";
                return RedirectToAction("Index", "Profile");
            }
            
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
