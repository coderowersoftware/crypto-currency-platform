using CodeRower.CCP.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models
{
    public class UserCommission
    {
        public string? partnerName { get; set; }

        public string? partnerLevel { get; set; }

        public int? childrenCount { get; set; }

        public LicenseType? licenseType { get; set; }

        public DateTime? partnerCreatedAt { get; set; }

        public int numberoflicenses { get; set; }

        public decimal commissionEarned { get; set; }

    }
}