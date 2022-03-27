namespace CodeRower.CCP.Controllers.Models.Reports
{
    public class OverallLicenseDetails
    {
        public int Total { get; set; }

        public int Unutilized { get; set; }

        public int Used { get; set; }

        public int Remaining { get; set; }

        public int Purchased { get; set; }
        public decimal CoinsInFarming { get; set; }
        public decimal CoinsInMinting { get; set; }
        public decimal CoinsInWallet { get; set; }
        public int PoolLicenseMiners { get; internal set; }
        public int ActiveLicenseMiners { get; internal set; }
        public int TotalUsers { get; internal set; }
        public int TotalAirdropUsers{ get; internal set; }
    }
}