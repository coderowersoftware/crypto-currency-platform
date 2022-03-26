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
        public decimal LatestRateInUSD { get; internal set; }
        public string? WalletTenantId { get; internal set; }
        public string? WalletClientId { get; internal set; }
        public string? WalletSecret { get; internal set; }
        public string? TwilioAccountSID { get; internal set; }
        public string? TwilioAuthToken { get; internal set; }
        public string? TwilioMessageServiceId { get; internal set; }
        public string? NodeHost { get; internal set; }
        public string? WalletHost { get; internal set; }
        public string? AuthJWTSecret { get; internal set; }
        public string? SendGridApiKey{ get; internal set; }
        public string? SendGridOtpTemplateId { get; internal set; }
        public string? SendGridEmailFrom { get; internal set; }

    }
}