using Transactions.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models.Reports
{
    public class LicenseLog
    {
        public string? UserId { get; set; }
        public string? LicenseId { get; set; }

        public string? Title { get; set; }
        public DateTime? MiningStartedAt { get; set; }

        public MiningStatus MiningStatus { get; set; }

    }
}