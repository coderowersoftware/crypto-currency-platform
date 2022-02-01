using Transactions.Controllers.Models.Enums;

namespace Transactions.Controllers.Models
{
    public class LicenseLog
    {
        public Guid CustomerId { get; set; }
        public Guid LicenseId { get; set; }

        public string? Title { get; set; }
        public DateTime? MiningStartedAt { get; set; }

        public MiningStatus MiningStatus { get; set; }

        public string? MinedBy { get; set; }
    }
}