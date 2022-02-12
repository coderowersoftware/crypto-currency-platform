namespace CodeRower.CCP.Controllers.Models
{
    public class TenantInfo
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public decimal? MintRewardsDailyPercent { get; set; }
        public decimal? FarmingDailyUnlockPercent { get; set; }
    }
}