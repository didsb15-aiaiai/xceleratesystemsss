using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;                      // server-side JobApplications model
using XcelerateLinks.Models.ViewModels;     // your MVC view models (if applicable)

namespace XcelerateLinks.Mvc.Controllers
{
    public class ApplicationsController : ApiControllerBase
    {
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(IHttpClientFactory httpFactory, ILogger<ApplicationsController> logger)
            : base(httpFactory)
        {
            _logger = logger;
        }

        // GET: /Applications
        public async Task<IActionResult> Index()
        {
            var uid = GetCurrentUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/jobapplications/user/{uid}");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load job applications.";
                return View(Array.Empty<JobApplication>());
            }

            var applications = await resp.Content.ReadFromJsonAsync<IEnumerable<JobApplication>>();
            return View(applications ?? Array.Empty<JobApplication>());
        }

        // GET: /Applications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load job application {Id}: {Status}", id, resp.StatusCode);
                return RedirectToAction(nameof(Index));
            }

            var application = await resp.Content.ReadFromJsonAsync<JobApplication>();
            if (application == null) return RedirectToAction(nameof(Index));
            return View(application);
        }

        // GET: /Applications/Create?opportunityId=123
        [HttpGet]
        public async Task<IActionResult> Create(int? opportunityId = null)
        {
            var model = new JobApplicationCreateViewModel
            {
                Application = new JobApplication { OpportunityId = opportunityId ?? 0 }
            };

            model.Opportunities = await LoadOpportunitiesAsync();
            return View(model);
        }

        // POST: /Applications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobApplicationCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Opportunities = await LoadOpportunitiesAsync();
                return View(model);
            }

            // send OpportunityId and Name (API reads user from JWT)
            var payload = new { OpportunityId = model.Application.OpportunityId, Name = model.Application.Name };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/jobapplications/apply", payload);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create application.");
                model.Opportunities = await LoadOpportunitiesAsync();
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Applications/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load job application for delete {Id}: {Status}", id, resp.StatusCode);
                return RedirectToAction(nameof(Index));
            }

            var application = await resp.Content.ReadFromJsonAsync<JobApplication>();
            if (application == null) return RedirectToAction(nameof(Index));
            return View(application);
        }

        // POST: /Applications/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete job application {Id}: {Status}", id, resp.StatusCode);
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<Opportunity>> LoadOpportunitiesAsync()
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/opportunities");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unable to load opportunities for application form: {Status}", resp.StatusCode);
                return Array.Empty<Opportunity>();
            }

            return await resp.Content.ReadFromJsonAsync<IEnumerable<Opportunity>>() ?? Array.Empty<Opportunity>();
        }

        // View model: the JobApplications entity should include the new Name property.
        public class JobApplicationCreateViewModel
        {
            public JobApplication Application { get; set; } = new JobApplication();
            public IEnumerable<Opportunity> Opportunities { get; set; } = Array.Empty<Opportunity>();
        }
    }
}