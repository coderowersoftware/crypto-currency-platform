using Newtonsoft.Json;

namespace CodeRower.CCP.Controllers.Models.Reports
{
    public class Miner
    {
        [JsonIgnore]
        public string? UserId { get; set; }
        public string? CustomerId{ get; set; }
        public string? Image { get; set; }
        public string? Name { get; set; }
        public string? ReferralCode { get; set; }
        public string? DisplayName { get; set; }
        public long LicensesCount { get; set; }
        public decimal Variance { get; set; }

        public decimal LockedAmount { get; set; }

        public decimal UnlockedAmount { get; set; }
    }
}