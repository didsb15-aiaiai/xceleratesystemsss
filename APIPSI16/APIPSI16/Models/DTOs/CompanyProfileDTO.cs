using System;
using System.Collections.Generic;

namespace APIPSI16.Models.DTOs
{
    public class CompanyProfileDTO
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        public List<CompanyMemberDTO>? Members { get; set; }
        public List<OpportunityDTO>? Opportunities { get; set; }
    }

    public class CompanyMemberDTO
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class OpportunityDTO
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public byte? EmploymentType { get; set; }
        public byte? SeniorityLevel { get; set; }
        public byte? RemoteOption { get; set; }
        public string? Duration { get; set; }
        public decimal? CompensationMin { get; set; }
        public decimal? CompensationMax { get; set; }
        public string? CompensationCurrency { get; set; }
    }
}
