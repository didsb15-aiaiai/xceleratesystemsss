using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Claims;
using XcelerateLinks.Models.ViewModels;

namespace XcelerateLinks.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _env;
        private const string CookieName = "ApiAccessToken";

        public AccountController(IHttpClientFactory httpFactory, ILogger<AccountController> logger, IWebHostEnvironment env)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // If already authenticated, redirect based on role claim (use role from cookie principal)
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrWhiteSpace(roleClaim) && roleClaim == "0")
                    return RedirectToAction("AdminIndex", "Home");

                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpFactory.CreateClient("Api");
            var payload = new { username = model.Username?.Trim(), password = model.Password };

            try
            {
                var resp = await client.PostAsJsonAsync("api/auth/login", payload);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await SafeReadStringAsync(resp);
                    ModelState.AddModelError("", !string.IsNullOrWhiteSpace(body) ? $"Login failed: {body}" : $"Login failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    return View(model);
                }

                var auth = await resp.Content.ReadFromJsonAsync<AuthResponse?>();
                if (auth == null || string.IsNullOrWhiteSpace(auth.Token))
                {
                    ModelState.AddModelError("", "Login failed: invalid response from authentication service.");
                    return View(model);
                }

                // store API JWT in HttpOnly cookie for API calls
                Response.Cookies.Append(CookieName, auth.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = auth.ExpiresAt.ToUniversalTime()
                });

                // clear any existing MVC cookie principal
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // parse JWT to obtain user info including role (role should come from User.Role column via API)
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(auth.Token);

                var name = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name" || c.Type == "name")?.Value
                           ?? model.Username
                           ?? string.Empty;
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
                var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? "1";

                // Build MVC claims principal and include the role claim so User.IsInRole / [Authorize(Roles="0")] work
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, name) };
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                }

                // include role from JWT (matches Role column in DB)
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                // Redirect admin users to AdminIndex
                if (role == "0")
                {
                    return RedirectToAction("AdminIndex", "Home");
                }

                // Respect return URL if present and safe
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login exception");
                ModelState.AddModelError("", "Unable to contact authentication service.");
                if (_env.IsDevelopment()) ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete(CookieName);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpFactory.CreateClient("Api");
            var payload = new
            {
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Nationality = model.Nationality,
                JobPreference = model.JobPreference,
                Profile_Bio = model.Profile_Bio,
                DoB = model.DoB,
                Role = model.Role,
                Password = model.Password
            };

            try
            {
                var resp = await client.PostAsJsonAsync("api/auth/register", payload);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await SafeReadStringAsync(resp);
                    ModelState.AddModelError("", !string.IsNullOrWhiteSpace(body) ? $"Registration failed: {body}" : $"Registration failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    return View(model);
                }

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register exception");
                ModelState.AddModelError("", "Unable to contact authentication service.");
                return View(model);
            }
        }

        // helper to read response body without throwing
        private static async Task<string?> SafeReadStringAsync(HttpResponseMessage resp)
        {
            try
            {
                return resp.Content == null ? null : await resp.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        // DTO for auth response
        private class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }
    }
}