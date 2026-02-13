using System;
using System.Collections.Generic;

namespace APIPSI16.Models;

public partial class Opportunity
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public int? CreatorId { get; set; }

    public int? CompanyId { get; set; }

    public byte? EmploymentType { get; set; }

    public byte? SeniorityLevel { get; set; }

    public string? Location { get; set; }

    public byte? RemoteOption { get; set; }

    public string? Description { get; set; }

    public string? Duration { get; set; }

    public decimal? CompensationMin { get; set; }

    public decimal? CompensationMax { get; set; }

    public string? CompensationCurrency { get; set; }

    public virtual Company? Company { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<EmployerCandidateHistory> EmployerCandidateHistories { get; set; } = new List<EmployerCandidateHistory>();

    public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
