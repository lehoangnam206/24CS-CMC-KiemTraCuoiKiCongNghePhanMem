using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using TechStoreWeb.Data;
using TechStoreWeb.Models;
using TechStoreWeb.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace TechStoreWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user, string ConfirmPassword, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            ModelState.Remove("Role");
            ModelState.Remove("LoginProvider");
            ModelState.Remove("ProviderKey");
            ModelState.Remove("Username");

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("", "Vui lòng nhập mật khẩu.");
            }
            else if (user.Password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (user.Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
            }

            if (ModelState.IsValid)
            {
                var exists = _context.Users.Any(u => u.Email == user.Email || 
                    (user.Username != null && u.Username == user.Username));
                if (exists)
                {
                    ModelState.AddModelError("", "Email hoặc tên đăng nhập đã tồn tại.");
                    return View(user);
                }

                user.Role = "Customer";
                user.IsLocked = false;
                user.LoginProvider = "Local";
                user.Password = _passwordHasher.Hash(user.Password!);
                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", new { returnUrl });
            }
            return View(user);
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin đăng nhập.");
                return View();
            }

            var user = _context.Users.FirstOrDefault(u =>
                (u.Username == username || u.Email == username) &&
                u.LoginProvider == "Local");

            var verification = user == null
                ? PasswordVerificationResult.Failed
                : _passwordHasher.Verify(user.Password, password);

            if (user != null && verification != PasswordVerificationResult.Failed)
            {
                if (user.IsLocked)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa.");
                    return View();
                }

                if (verification == PasswordVerificationResult.SuccessNeedsUpgrade)
                {
                    user.Password = _passwordHasher.Hash(password);
                    _context.SaveChanges();
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (user.Role == "Admin")
                    return RedirectToAction("Index", "Products", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email/tên đăng nhập hoặc mật khẩu không chính xác.");
            return View();
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Đăng nhập thất bại. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
            if (claims == null)
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin tài khoản.";
                return RedirectToAction("Login");
            }

            var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            var issuer = result.Principal?.Identities.FirstOrDefault()?.AuthenticationType;
            var provider = issuer switch
            {
                "Facebook" => "Facebook",
                "Google" => "Google",
                _ => "External"
            };

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["ErrorMessage"] = $"Không thể lấy ID từ {provider}.";
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u =>
                u.LoginProvider == provider && u.ProviderKey == providerId);

            if (user == null && !string.IsNullOrEmpty(email))
            {
                user = _context.Users.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    user.LoginProvider = provider;
                    user.ProviderKey = providerId;
                    _context.SaveChanges();
                }
            }

            if (user == null)
            {
                user = new User
                {
                    FullName = name ?? $"{provider} User",
                    Email = email ?? $"{provider.ToLower()}_{providerId}@external.com",
                    Username = null,
                    Password = null,
                    PhoneNumber = null,
                    Role = "Customer",
                    IsLocked = false,
                    LoginProvider = provider,
                    ProviderKey = providerId
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            if (user.IsLocked)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa.";
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Products", new { area = "Admin" });

            return Redirect(returnUrl ?? "/");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });

            var user = _context.Users.Find(userId);
            if (user == null) return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(User updatedUser)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });

            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    user.FullName = updatedUser.FullName;
                    user.Email = updatedUser.Email;
                    user.PhoneNumber = updatedUser.PhoneNumber;

                    if (!string.IsNullOrEmpty(updatedUser.Password))
                    {
                        if (updatedUser.Password.Length < 6)
                        {
                            ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự.");
                            return View(updatedUser);
                        }

                        user.Password = _passwordHasher.Hash(updatedUser.Password);
                    }

                    _context.SaveChanges();

                    HttpContext.Session.SetString("Username", user.FullName);
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
            }
            return View(updatedUser);
        }

        public IActionResult Orders()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", new { returnUrl = "/Account/Orders" });

            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }
    }
}