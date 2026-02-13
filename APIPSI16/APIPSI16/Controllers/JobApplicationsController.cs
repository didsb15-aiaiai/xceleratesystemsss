using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIPSI16.Data;
using APIPSI16.Models;
using APIPSI16.Models.DTOs;

namespace APIPSI16.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobApplicationsController : ControllerBase
    {
        private readonly xcleratesystemslinks_SampleDBContext _db;
        public JobApplicationsController(xcleratesystemslinks_SampleDBContext db) => _db = db;

        // POST api/jobapplications/apply
        // Payload: { OpportunityId: int, Name: "optional applicant-provided name (varchar(50))" }
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyDto dto)
        {
            var uid = GetUserId();
            if (uid == null) return Unauthorized();

            if (dto.OpportunityId <= 0) return BadRequest("OpportunityId is required.");

            // optional: validate Name length
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Length > 50)
                return BadRequest("Name must be 50 characters or fewer.");

            if (await _db.JobApplications.AnyAsync(a => a.OpportunityId == dto.OpportunityId && a.UserId == uid))
                return Conflict("Already applied");

            var app = new JobApplication
            {
                OpportunityId = dto.OpportunityId,
                UserId = uid.Value,
                Status = 0,
                AppliedAt = DateTime.UtcNow,
                Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name // assumes JobApplications has Name property
            };

            await _db.JobApplications.AddAsync(app);
            await _db.SaveChangesAsync();

            var opportunity = await _db.Opportunities.FindAsync(dto.OpportunityId);
            if (opportunity != null)
            {
                // best-effort notify: if the opportunity has a CreatorId or CompanyId property, adjust accordingly
                var notifyUserId = opportunity.CreatorId ?? 0;
                if (notifyUserId != 0)
                {
                    await _db.Notifications.AddAsync(new Notification
                    {
                        UserId = notifyUserId,
                        ActorUserId = uid.Value,
                        Type = "JobApplied",
                        Payload = $"{{\"applicationId\":{app.JobApplicationId}}}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
            }

            return CreatedAtAction(nameof(Get), new { id = app.JobApplicationId }, app);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var app = await _db.JobApplications.FindAsync(id);
            if (app == null) return NotFound();
            return Ok(app);
        }

        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var actorId = GetUserId();
            if (actorId == null) return Unauthorized();

            var app = await _db.JobApplications.FindAsync(id);
            if (app == null) return NotFound();

            app.Status = dto.NewStatus;
            app.UpdatedAt = DateTime.UtcNow;
            _db.JobApplications.Update(app);

            await _db.AuditLogs.AddAsync(new AuditLog { UserId = actorId.Value, Action = "UpdateApplicationStatus", TargetType = "JobApplication", TargetId = id, CreatedAt = DateTime.UtcNow });
            await _db.Notifications.AddAsync(new Notification
            {
                UserId = app.UserId,
                ActorUserId = actorId.Value,
                Type = "ApplicationStatusChanged",
                Payload = $"{{\"applicationId\":{id},\"newStatus\":{dto.NewStatus}}}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(app);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> ForUser(int userId)
        {
            var uid = GetUserId();
            if (uid == null) return Unauthorized();

            if (uid != userId && !User.IsInRole("0")) return Forbid();

            var list = await _db.JobApplications.Where(a => a.UserId == userId).ToListAsync();
            return Ok(list);
        }

        private int? GetUserId()
        {
            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(sid, out var id) ? id : (int?)null;
        }

        // DTO used by this controller
        public class ApplyDto
        {
            public int OpportunityId { get; set; }
            public string? Name { get; set; } // optional applicant-provided name (varchar(50))
        }

    }
}