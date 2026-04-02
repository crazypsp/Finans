using System.Security.Claims;
using Finans.Application.Abstractions.Security;
using Finans.Application.Dtos.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Finans.WebMvc.Security;

namespace Finans.WebMvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(request, ct);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Giriş başarısız.");
                return View(request);
            }

            // Neden claim?
            // - Cookie auth bu şekilde rol bazlı yetkilendirmeyi (Authorize(Roles="ADMIN")) sağlar.
            var claims = new List<Claim>
            {
                new Claim("UserId", result.UserId!.Value.ToString()),
                new Claim(ClaimTypes.Name, result.UserName ?? "user")
            };

            foreach (var role in result.RoleCodes)
                claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("CompanyId", (result.CompanyId ?? 1).ToString()));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => Content("Erişim reddedildi.");
    }
}