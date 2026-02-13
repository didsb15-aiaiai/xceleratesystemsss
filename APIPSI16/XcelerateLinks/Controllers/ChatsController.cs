using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;

namespace XcelerateLinks.Mvc.Controllers
{
    public class ChatsController : ApiControllerBase
    {
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IHttpClientFactory httpFactory, ILogger<ChatsController> logger)
            : base(httpFactory)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/chat");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load chats.";
                return View(Array.Empty<Chat>());
            }

            var chats = await resp.Content.ReadFromJsonAsync<IEnumerable<Chat>>();
            return View(chats ?? Array.Empty<Chat>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var chat = await resp.Content.ReadFromJsonAsync<Chat>();
            if (chat == null) return RedirectToAction(nameof(Index));
            return View(chat);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new Chat
            {
                CreatedByUserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chat model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                ModelState.AddModelError("", "Unable to identify current user.");
                return View(model);
            }

            model.CreatedByUserId = userId.Value;
            model.CreatedAt ??= DateTime.UtcNow;

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/chat", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create chat.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var chat = await resp.Content.ReadFromJsonAsync<Chat>();
            if (chat == null) return RedirectToAction(nameof(Index));
            return View(chat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
