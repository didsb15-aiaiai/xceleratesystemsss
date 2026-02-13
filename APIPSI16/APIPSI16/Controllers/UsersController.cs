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
    public class UsersController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _context;

        public UsersController(xcleratesystemslinks_SampleDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/Users
        // - If no filters supplied: Admin (0) only -> returns all users
        // - If filters supplied (jobPreference and/or nationality): Admin (0) and Employer (2) can query
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int? jobPreference, [FromQuery] int? nationality)
        {
            var userRole = GetCurrentUserRole();

            // If any filter present, allow Admin and Employer
            if (jobPreference.HasValue || nationality.HasValue)
            {
                if (userRole != "0" && userRole != "2")
                    return Forbid();

                IQueryable<User> q = _context.Users;

                if (jobPreference.HasValue)
                    q = q.Where(u => u.JobPreference == jobPreference.Value);

                if (nationality.HasValue)
                    q = q.Where(u => u.Nationality == nationality.Value);

                var filtered = await q.ToListAsync();
                return Ok(filtered);
            }

            // No filters: only admin can list everyone
            if (userRole != "0")
                return Forbid();

            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/5
        // Admin can view any user; regular users can view only their own record
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole != "0" && currentUserId != id)
                return Forbid();

            return Ok(user);
        }

        // POST: api/Users
        // Only admins can create users via this endpoint.
        // (Self-registration should be done through /api/Auth/register)
        [HttpPost]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        // PUT: api/Users/5
        // Admins can update any user; users can update only their own record.
        // Non-admins cannot change Role or PasswordHash through this endpoint.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updated)
        {
            if (id != updated.UserId) return BadRequest();

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Only admin or owner can update
            if (userRole != "0" && existing.UserId != currentUserId)
                return Forbid();

            // Non-admins: only allow a subset of fields to be changed
            if (userRole != "0")
            {
                existing.Name = updated.Name;
                existing.PhoneNumber = updated.PhoneNumber;
                existing.Nationality = updated.Nationality;
                existing.JobPreference = updated.JobPreference;
                existing.ProfileBio = updated.ProfileBio;
                existing.DoB = updated.DoB;
                // Do NOT allow non-admins to change Email, Role, PasswordHash
            }
            else
            {
                // Admin can update most fields; don't automatically accept a plaintext password here
                existing.Name = updated.Name;
                existing.Email = updated.Email;
                existing.PhoneNumber = updated.PhoneNumber;
                existing.Nationality = updated.Nationality;
                existing.JobPreference = updated.JobPreference;
                existing.ProfileBio = updated.ProfileBio;
                existing.DoB = updated.DoB;
                existing.Role = updated.Role;
                // If you want admins to reset passwords, provide a dedicated endpoint that accepts a hashed password
            }

            try
            {
                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Users/5
        // Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(u => u.UserId == id);
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