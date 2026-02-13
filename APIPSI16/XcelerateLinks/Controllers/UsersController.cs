using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;
using XcelerateLinks.Models.ViewModels;

namespace XcelerateLinks.Mvc.Controllers
{
    public class UsersController : ApiControllerBase
    {
        private readonly ILogger<UsersController> _logger;

        public UsersController(IHttpClientFactory httpFactory, ILogger<UsersController> logger)
            : base(httpFactory)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? jobPreference = null, int? nationality = null)
        {
            var model = new UserFilterViewModel
            {
                JobPreference = jobPreference,
                Nationality = nationality
            };

            var client = CreateAuthorizedClient();
            var query = new List<string>();
            if (jobPreference.HasValue) query.Add($"jobPreference={jobPreference.Value}");
            if (nationality.HasValue) query.Add($"nationality={nationality.Value}");
            var url = query.Count == 0 ? "api/users" : $"api/users?{string.Join("&", query)}";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                model.ErrorMessage = await SafeReadStringAsync(resp) ?? "Unable to load users with the selected filters.";
                model.Users = Array.Empty<User>();
                return View(model);
            }

            model.Users = await resp.Content.ReadFromJsonAsync<IEnumerable<User>>() ?? Array.Empty<User>();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await resp.Content.ReadFromJsonAsync<User>();
            if (user == null) return RedirectToAction(nameof(Index));
            return View(user);
        }

        public IActionResult Profile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Edit), new { id = userId.Value });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await resp.Content.ReadFromJsonAsync<User>();
            if (user == null) return RedirectToAction(nameof(Index));
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.UserId) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/users/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update user.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
