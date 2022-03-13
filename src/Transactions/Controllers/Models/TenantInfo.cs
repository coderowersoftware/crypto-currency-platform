namespace CodeRower.CCP.Controllers.Models
{
    public class TenantInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public decimal? MintRewardsDailyPercent { get; set; }
        public decimal? FarmingDailyUnlockPercent { get; set; }
        public decimal? DailyCoinRewardForAirDropUser { get; internal set; }
        public decimal? OfferDailyCoinRewardForAirDropUserForDays { get; internal set; }
        public decimal? UnlockToWalletFeePct { get; internal set; }
        public decimal? WalletToWalletFeePct { get; internal set; }
        public decimal? CoinPaymentWalletWithdrawalFeePct { get; internal set; }
        public decimal? BankAccountWithdrawalFeePct { get; internal set; }
        public decimal? MinWithdrawalLimitInUSD { get; internal set; }
        public decimal LicenseCost { get; internal set; }
        public string? LicenseCostCurrency { get; internal set; }
        public decimal MonthlyMaintenancePct { get; internal set; }
        public decimal LicenseCommissionPct { get; internal set; }
    }
}