using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;

namespace XcelerateLinks.Mvc.Controllers
{
    public class CompaniesController : ApiControllerBase
    {
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(IHttpClientFactory httpFactory, ILogger<CompaniesController> logger)
            : base(httpFactory)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/companies");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load companies.";
                return View(Array.Empty<Company>());
            }

            var companies = await resp.Content.ReadFromJsonAsync<IEnumerable<Company>>();
            return View(companies ?? Array.Empty<Company>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));
            return View(company);
        }

        [HttpGet]
        public IActionResult Create() => View(new Company());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/companies", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create company.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company model)
        {
            if (id != model.CompanyId) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/companies/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update company.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
