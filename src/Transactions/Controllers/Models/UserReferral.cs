using CodeRower.CCP.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models
{
    public class UserReferral
    {
        public string? ReferralCode { get; set; }

        public LicenseType LicenseType { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int NumberOfLicenses { get; set; }
    }
}