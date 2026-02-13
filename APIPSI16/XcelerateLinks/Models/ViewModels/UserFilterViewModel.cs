using APIPSI16.Models;

namespace XcelerateLinks.Models.ViewModels
{
    public class UserFilterViewModel
    {
        public int? JobPreference { get; set; }

        public int? Nationality { get; set; }

        public IEnumerable<User> Users { get; set; } = Array.Empty<User>();

        public string? ErrorMessage { get; set; }
    }
}
