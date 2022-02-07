namespace CodeRower.CCP.Controllers.Models.Reports
{
    public class OverallLicenseDetails
    {
        public int? LicenseUsers { get; set; }
        public int? PoolLicenseMiners { get; set; }
        public int? ActiveLicenseMiners { get; set; }
        public decimal CoinsInFarming { get; set; }
        public decimal CoinsInMinting { get; set; }
        public decimal CoinsInWallet { get; set; }
    }
}