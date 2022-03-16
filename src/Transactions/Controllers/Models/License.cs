using CodeRower.CCP.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models
{
    public class License
    {
        public Guid? CustomerId { get; set; }
        public Guid LicenseId { get; set; }
        public string? LicenseNumber { get; set; }
        public decimal? Mined { get; set; }
        public string? Title { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public MiningStatus? MiningStatus { get; set; }
        public string? LicenseType { get; set; }
    }
}