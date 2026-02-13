using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIPSI16.Data;
using APIPSI16.Models;

namespace APIPSI16.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostCommentsController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _db;
        public PostCommentsController(xcleratesystemslinks_SampleDBContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] PostComment dto)
        {
            var uid = GetUserId();
            if (uid == null) return Unauthorized();

            if (dto.ParentCommentId.HasValue)
            {
                var parent = await _db.PostComments.FindAsync(dto.ParentCommentId.Value);
                if (parent == null) return BadRequest("Parent comment not found.");
                if (parent.PostId != dto.PostId) return BadRequest("Parent comment belongs to different post.");
            }

            dto.UserId = uid.Value;
            dto.CreatedAt = DateTime.UtcNow;
            await _db.PostComments.AddAsync(dto);
            await _db.SaveChangesAsync();

            var post = await _db.Posts.FindAsync(dto.PostId);
            if (post != null && post.UserId != uid)
            {
                await _db.Notifications.AddAsync(new Notification
                {
                    UserId = post.UserId,
                    ActorUserId = uid.Value,
                    Type = "PostComment",
                    Payload = $"{{\"postId\":{post.PostId},\"commentId\":{dto.CommentId}}}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(Get), new { id = dto.CommentId }, dto);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var c = await _db.PostComments.FindAsync(id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = GetUserId();
            if (uid == null) return Unauthorized();

            var c = await _db.PostComments.FindAsync(id);
            if (c == null) return NotFound();
            if (c.UserId != uid && !User.IsInRole("0")) return Forbid();
            _db.PostComments.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int? GetUserId()
        {
            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(sid, out var id) ? id : (int?)null;
        }
    }
}