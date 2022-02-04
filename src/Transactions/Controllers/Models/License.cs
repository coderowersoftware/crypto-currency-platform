using Transactions.Controllers.Models.Enums;

namespace Transactions.Controllers.Models
{
    public class License
    {
        public Guid CustomerId { get; set; }
        public Guid LicenseId { get; set; }
        public int Mined { get; set; }
        public string? Title { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public MiningStatus? MiningStatus { get; set; }
    }
}