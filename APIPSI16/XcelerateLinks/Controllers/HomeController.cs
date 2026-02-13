using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using XcelerateLinks.Mvc.Models.ViewModels; // ensure this matches your actual namespace

namespace XcelerateLinks.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;

        public HomeController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Admin-only view - verifies role from the users table (via the API).
        // Requires that your API exposes an endpoint that returns user details by id,
        // e.g. GET /api/users/{id} which returns JSON containing at least a "role" property.
        [Authorize]
        public async Task<IActionResult> AdminIndex()
        {
            // 1) try to get role from the JWT Claim first (fast path)
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(roleClaim) && int.TryParse(roleClaim, out var roleFromClaim))
            {
                if (roleFromClaim == 0) // 0 = Admin in your schema
                    return View("AdminIndex");

                return Forbid();
            }

            // 2) fallback: fetch the user's record from the API to read the Role column
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim))
            {
                // no user id claim -> require authentication/login
                return Challenge();
            }

            try
            {
                var client = _httpFactory.CreateClient("Api");

                // Attach bearer token from cookie if present (so API will authenticate)
                var token = Request.Cookies["ApiAccessToken"];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // Adjust the endpoint below to match your API route that returns a user by id
                var resp = await client.GetAsync($"api/users/{idClaim}");
                if (!resp.IsSuccessStatusCode)
                {
                    // If the API call failed, deny access (or redirect) to be safe.
                    return Forbid();
                }

                var userDto = await resp.Content.ReadFromJsonAsync<UserDto?>();
                if (userDto == null)
                    return Forbid();

                if (userDto.Role == 0) // admin
                    return View("AdminIndex");

                return Forbid();
            }
            catch
            {
                // On error, deny access (do not expose details)
                return Forbid();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var vm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(vm);
        }

        // Simple DTO for user API response. If you already have a shared model, use that instead.
        private class UserDto
        {
            public int Role { get; set; }
        }
    }
}