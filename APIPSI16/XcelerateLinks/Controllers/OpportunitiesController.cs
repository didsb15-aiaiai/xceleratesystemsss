using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;

namespace XcelerateLinks.Mvc.Controllers
{
    public class OpportunitiesController : ApiControllerBase
    {
        private readonly ILogger<OpportunitiesController> _logger;

        public OpportunitiesController(IHttpClientFactory httpFactory, ILogger<OpportunitiesController> logger)
            : base(httpFactory)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/opportunities");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load opportunities.";
                return View(Array.Empty<Opportunity>());
            }

            var opportunities = await resp.Content.ReadFromJsonAsync<IEnumerable<Opportunity>>();
            return View(opportunities ?? Array.Empty<Opportunity>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new Opportunity();
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                model.CreatorId = userId.Value;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity model)
        {
            if (!ModelState.IsValid) return View(model);

            model.CreatorId ??= GetCurrentUserId();

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/opportunities", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create opportunity.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity model)
        {
            if (id != model.Id) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/opportunities/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update opportunity.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
