namespace APIPSI16.Models.DTOs
{
    public partial class UserDTO
    {
        // Standardize: Use UserId as primary identifier (matches User entity)
        public int UserId { get; set; }

        // Remove Id and UserID if not needed elsewhere
        // public int Id { get; set; }
        // public int UserID { get; set; }

        // Basic info
        public string? Name { get; set; }
        public string? Email { get; set; }

        // Domain fields
        public int? Nationality { get; set; }
        public int? JobPreference { get; set; }

        // Add Password property if needed for compatibility
        public string? Password { get; set; }

        // Other fields
        public int? OpportunityId { get; set; }
        public virtual Opportunity? Opportunity { get; set; }
        public virtual User? User { get; set; }
    }
}