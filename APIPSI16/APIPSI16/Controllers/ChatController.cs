using APIPSI16.Data;
using APIPSI16.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIPSI16.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require JWT for all actions
    public class ChatController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _context;

        public ChatController(xcleratesystemslinks_SampleDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/Chat
        // Users see only their own chats; admins see all
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            IQueryable<Chat> query = _context.Chats.Include(c => c.ChatUsers);

            // Non-admins see only chats they're part of
            if (userRole != "0")
            {
                if (!currentUserId.HasValue) return Unauthorized();
                query = query.Where(c => c.ChatUsers.Any(cu => cu.UserId == currentUserId.Value));
            }

            var chats = await query.ToListAsync();
            return Ok(chats);
        }

        // GET: api/Chat/5
        // Users can only see chats they're part of; admins can see any
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChat(int id)
        {
            var chat = await _context.Chats
                .Include(c => c.ChatUsers)
                .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(c => c.ChatId == id);

            if (chat == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Enforce ownership: users can only see chats they're part of
            if (userRole != "0" && !chat.ChatUsers.Any(cu => cu.UserId == currentUserId))
                return Forbid();

            return Ok(chat);
        }

        // POST: api/Chat
        // All authenticated users can create chats
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] Chat chat)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChat), new { id = chat.ChatId }, chat);
        }

        // DELETE: api/Chat/5
        // Only admins can delete chats
        [HttpDelete("{id}")]
        [Authorize(Roles = "0")] // Admin only
        public async Task<IActionResult> DeleteChat(int id)
        {
            var chat = await _context.Chats.FindAsync(id);
            if (chat == null) return NotFound();

            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}