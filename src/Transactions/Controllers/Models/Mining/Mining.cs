using Transactions.Controllers.Models.Enums;

namespace Transactions.Controllers.Models.Mining
{
    public class Mining
    {
        public Guid UserId { get; set; }
        public Guid LicenseId { get; set; }

        public DateTime StartDate { get; set; }
        public MiningStatus MiningStatus { get; set; }
    }
}