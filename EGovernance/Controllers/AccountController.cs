
using EGOVERNANCE.Models;
using EGOVERNANCE.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EGOVERNANCE.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // =========================
        // CITIZEN LOGIN
        // =========================

        [HttpGet]
        public IActionResult CitizenLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CitizenLogin(CitizenLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "No account found with this email.");
                return View(model);
            }

            var isCitizen = await _userManager.IsInRoleAsync(user, "Citizen");

            if (!isCitizen)
            {
                ModelState.AddModelError("", "This account is not registered as a Citizen.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Citizen");

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Your account is temporarily locked. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError("", "Incorrect email or password.");
            return View(model);
        }

        // =========================
        // ADMIN LOGIN
        // =========================

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Administrator account not found.");
                return View(model);
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin)
            {
                ModelState.AddModelError("", "This account does not have administrator access.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Admin");

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Your account is temporarily locked. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError("", "Incorrect email or password.");
            return View(model);
        }

        // =========================
        // CITIZEN REGISTRATION
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Ensure Citizen role exists.
            if (!await _roleManager.RoleExistsAsync("Citizen"))
            {
                var createRoleResult =
                    await _roleManager.CreateAsync(new IdentityRole("Citizen"));

                if (!createRoleResult.Succeeded)
                {
                    foreach (var error in createRoleResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }
            }

            // Create Citizen account.
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Assign Citizen role.
            var roleResult = await _userManager.AddToRoleAsync(user, "Citizen");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError("", error.Description);

                await _userManager.DeleteAsync(user);
                return View(model);
            }

            // Sign in newly registered Citizen.
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Citizen");
        }

        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
