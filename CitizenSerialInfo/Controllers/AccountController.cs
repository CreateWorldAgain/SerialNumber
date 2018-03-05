using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using CitizenSerialInfo.Models;
using CitizenSerialInfo.Models.ViewModels;
using CitizenSerialInfo.Services;

namespace CitizenSerialInfo.Controllers
{
    public class AccountController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private AppDbContext _db;
        private UserManager<ApplicationUser> _userManager;
        private IEmailSender _emailSender;
        private IConfiguration _configuration;
        private IViewRenderService _viewRenderService;
        private RoleManager<ApplicationRole> _roleManager;

        public AccountController(AppDbContext db,            
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailSender emailSender,
            IViewRenderService viewRenderService,
            SignInManager<ApplicationUser> signInManager)
        {
            _roleManager = roleManager;
            _configuration = configuration;
            _viewRenderService = viewRenderService;
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        async public Task<IActionResult> Register(RegisterViewModel model)
        {
            string error = "";

            var searchInfoCount = (await _roleManager.FindByNameAsync("default")).MoreInfoCount;

            var result = await _userManager.CreateAsync(new ApplicationUser {
                Email=model.Email,
                EmailConfirmed=false,
                UserName=model.Login,
                FirstName=model.FirstName,
                LastName=model.LastName,
                MoreInfoCount= searchInfoCount                              
            }, model.Password);

            if (!result.Succeeded)
            {
                error = String.Join("\r\n",result.Errors.Select(s => s.Description).ToArray());
            }
            else
            {
                var user = await _userManager.FindByNameAsync(model.Login);
                if (user != null)
                {
                    int countAddInfo = (await _roleManager.FindByNameAsync("default")).MoreInfoCount;
                    user.MoreInfoCount = countAddInfo;
                    _db.SaveChanges();

                    var res=await _userManager.AddToRoleAsync(user, "default");
                    if (!res.Succeeded)
                        error = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    else
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

                        var emailModel = new ConfirmEmailViewModel
                        {
                            ConfirmLink = callbackUrl,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        };

                        var htmlEmailText = await _viewRenderService.RenderToStringAsync("Emails/ConfirmationEmailTemplate", emailModel);

                        await _emailSender.SendEmailConfirmationAsync(model.Email, "Confirm your email", htmlEmailText);
                        
                    }
                }
                else error = "Can not find user: "+model.Login;
            }

            return Json(new { error = error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        async public Task<IActionResult> Login(LoginViewModel model)
        {
            string error = "";

            var user = _db.Users.FirstOrDefault(s => s.Email == model.Login || s.UserName == model.Login);

            if (user != null)
            {
                if (!user.IsApproved)
                    error = "Your account not yet approved by manager.";
                else
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (!result.Succeeded)
                    {
                        if (result.IsNotAllowed)
                            error = "Your email is not confirmed.";
                        else
                            error = "User name or password incorrect";
                    }
                }
            }
            else
                error = "User name or password incorrect";

            return Json(new { error=error });
        }

        [HttpGet]
        async public Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ApproveUserByManager(string code)
        {
            var model = new ApproveUserViewModel();
            model.Error = "";

            var user = _db.Users.FirstOrDefault(s => s.SecurityStamp == code);
            if (user==null)
                model.Error = "User is not found. Confirmation faild.";
            else
            {
                model.FirstName = user.FirstName;
                model.LastName = user.LastName;
                user.IsApproved = true;
                _db.SaveChanges();
              
                await _emailSender.SendEmailInfoAboutApproveAsync(user.Email, "You account is approved", "Our manager confirmed your registration. Now you can signIn. Use your login and password.");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            string error="";

            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                error=$"Unable to load user with ID '{userId}'.";
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            

            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                user.SecurityStamp = Guid.NewGuid().ToString().ToLower();
                _db.SaveChanges();


                var htmlEmailText = await _viewRenderService.RenderToStringAsync("Emails/ConfirmationUserEmailTemplate", new ConfirmEmailViewModel {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ConfirmLink = Url.EmailApproveUserLink(user.SecurityStamp, Request.Scheme)
                });

                var emailManager = _configuration.GetSection("EmailSettings").GetValue<String>("EmailForConfirmUsers");
                await _emailSender.SendEmailForConfirmationUserAsync(emailManager, "New registered user", htmlEmailText);

                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            else
            {
                error = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
            }

            return View("ConfirmEmail", error);
        }

       

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            string error = "";

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    error = "Email is not valid!!!";
                }
                else
                {

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    _emailSender.SendEmail(model.Email, "Reset Password",
                       $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");                    
                }
            }
            else
            {
                error = "Bad email";
            }

            return Json(new { error = error });
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                
                return RedirectToAction(nameof(ResetPasswordConfirmation));
               
            }
           
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


    }
}
