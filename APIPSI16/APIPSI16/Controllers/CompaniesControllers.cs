using APIPSI16.Data;
using APIPSI16.Models;
using APIPSI16.Models.DTOs;
using APIPSI16.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIPSI16.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require JWT for all actions
    public class CompaniesController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _context;
        private readonly IFileStorageService _fileStorage;

        public CompaniesController(xcleratesystemslinks_SampleDBContext context, IFileStorageService fileStorage)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        // GET: api/Companies
        // All authenticated users can view companies
        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            return Ok(await _context.Companies.ToListAsync());
        }

        // GET: api/Companies/5
        // All authenticated users can view company details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return Ok(company);
        }

        // GET: api/Companies/5/profile
        // Get complete company profile with members and opportunities
        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetCompanyProfile(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            var members = await _context.CompanyMembers
                .Where(cm => cm.CompanyId == id)
                .Include(cm => cm.User)
                .Select(cm => new CompanyMemberDTO
                {
                    UserId = cm.UserId,
                    UserName = cm.User.Name,
                    Role = cm.Role.ToString(),
                    JoinedAt = cm.StartDate.HasValue ? new DateTime(cm.StartDate.Value.Year, cm.StartDate.Value.Month, cm.StartDate.Value.Day) : (DateTime?)null
                })
                .ToListAsync();

            var opportunities = await _context.Opportunities
                .Where(o => o.CompanyId == id)
                .Select(o => new OpportunityDTO
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Location = o.Location,
                    EmploymentType = o.EmploymentType,
                    SeniorityLevel = o.SeniorityLevel,
                    RemoteOption = o.RemoteOption,
                    Duration = o.Duration,
                    CompensationMin = o.CompensationMin,
                    CompensationMax = o.CompensationMax,
                    CompensationCurrency = o.CompensationCurrency
                })
                .ToListAsync();

            var profileDto = new CompanyProfileDTO
            {
                CompanyId = company.CompanyId,
                Name = company.Name,
                Industry = company.Industry,
                Location = company.Location,
                CompanyLogoUrl = company.CompanyLogoUrl,
                Description = company.Description,
                CreatedAt = company.CreatedAt,
                Members = members,
                Opportunities = opportunities
            };

            return Ok(profileDto);
        }

        // POST: api/Companies/5/upload-logo
        // Upload company logo
        [HttpPost("{id}/upload-logo")]
        [Authorize(Roles = "0,2")]
        public async Task<IActionResult> UploadCompanyLogo(int id, [FromForm] IFormFile file)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            // Check if user is member (for non-admins)
            if (userRole == "2")
            {
                if (!currentUserId.HasValue) return Unauthorized();
                var isMember = await _context.CompanyMembers
                    .AnyAsync(cm => cm.CompanyId == id && cm.UserId == currentUserId.Value);
                if (!isMember) return Forbid();
            }

            if (!_fileStorage.ValidateImageFile(file, out var errorMessage))
                return BadRequest(new { message = errorMessage });

            try
            {
                if (!string.IsNullOrEmpty(company.CompanyLogoUrl))
                {
                    await _fileStorage.DeleteFileAsync(company.CompanyLogoUrl);
                }

                var fileUrl = await _fileStorage.SaveFileAsync(file, "companies");
                company.CompanyLogoUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, fileUrl = fileUrl, message = "Company logo uploaded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error uploading file: {ex.Message}" });
            }
        }

        // POST: api/Companies/5/members
        // Invite a member to the company
        [HttpPost("{id}/members")]
        [Authorize(Roles = "0,2")]
        public async Task<IActionResult> InviteMember(int id, [FromBody] CompanyMember member)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole == "2")
            {
                if (!currentUserId.HasValue) return Unauthorized();
                var isMember = await _context.CompanyMembers
                    .AnyAsync(cm => cm.CompanyId == id && cm.UserId == currentUserId.Value);
                if (!isMember) return Forbid();
            }

            member.CompanyId = id;
            _context.CompanyMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(member);
        }

        // DELETE: api/Companies/5/members/userId
        // Remove a member from the company
        [HttpDelete("{id}/members/{userId}")]
        [Authorize(Roles = "0,2")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole == "2")
            {
                if (!currentUserId.HasValue) return Unauthorized();
                var isMember = await _context.CompanyMembers
                    .AnyAsync(cm => cm.CompanyId == id && cm.UserId == currentUserId.Value);
                if (!isMember) return Forbid();
            }

            var member = await _context.CompanyMembers
                .FirstOrDefaultAsync(cm => cm.CompanyId == id && cm.UserId == userId);

            if (member == null) return NotFound();

            _context.CompanyMembers.Remove(member);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Companies
        // Only admins and employers can create companies
        [HttpPost]
        [Authorize(Roles = "0,2")] // Admin or Employer
        public async Task<IActionResult> CreateCompany([FromBody] Company company)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompany), new { id = company.CompanyId }, company);
        }

        // PUT: api/Companies/5
        // Admins can update any company; employers can update their own company
        [HttpPut("{id}")]
        [Authorize(Roles = "0,2")] // Admin or Employer
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] Company company)
        {
            if (id != company.CompanyId) return BadRequest();

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Employers can only update companies they're members of
            if (userRole == "2")
            {
                if (!currentUserId.HasValue) return Unauthorized();

                var isMember = await _context.CompanyMembers
                    .AnyAsync(cm => cm.CompanyId == id && cm.UserId == currentUserId.Value);

                if (!isMember)
                    return Forbid("You can only update companies you're a member of.");
            }

            _context.Entry(company).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Companies/5
        // Only admins can delete companies
        [HttpDelete("{id}")]
        [Authorize(Roles = "0")] // Admin only
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(c => c.CompanyId == id);
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