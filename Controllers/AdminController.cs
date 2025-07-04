using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System;
using Microsoft.ApplicationInsights;

namespace Blog.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminAuthSettings _authSettings;
        private readonly TelemetryClient _telemetryClient;

        public AdminController(IOptions<AdminAuthSettings> authOptions, TelemetryClient telemetryClient)
        {
            _authSettings = authOptions.Value;
            _telemetryClient = telemetryClient;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(_authSettings.Username))
            {
                Exception ex = new Exception("Admin username is missing from configuration.");
                _telemetryClient.TrackException(ex);
                throw ex;
            }
            if (string.IsNullOrEmpty(_authSettings.PasswordHash))
            {
                Exception ex = new Exception("Admin password hash is missing from configuration.");
                _telemetryClient.TrackException(ex);
                throw ex;
            }
            if (username == _authSettings.Username && BCrypt.Net.BCrypt.Verify(password, _authSettings.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
                    });

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid login";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
    }

}
