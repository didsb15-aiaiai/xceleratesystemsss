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
    public class CompaniesController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _context;

        public CompaniesController(xcleratesystemslinks_SampleDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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