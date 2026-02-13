using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XcelerateLinks.Mvc.Services;

namespace XcelerateLinks.Mvc.Controllers
{
    [Authorize]
    public abstract class ApiControllerBase : Controller
    {
        private readonly IHttpClientFactory _httpFactory;

        protected ApiControllerBase(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        protected HttpClient CreateAuthorizedClient()
        {
            var client = _httpFactory.CreateClient("Api");
            if (Request.Cookies.TryGetValue(TokenHandler.CookieName, out var token) && !string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        protected int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        protected static async Task<string?> SafeReadStringAsync(HttpResponseMessage resp)
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
    }
}
