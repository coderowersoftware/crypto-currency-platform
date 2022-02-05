using Transactions.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models.Reports
{
    public class Licenses
    {
        public int TotalLicenses { get; set; }

        public int UnutilizedLicenses { get; set; }

        public int LicensesUsed { get; set; }

        public int LicensesRemaining { get; set; }
    }
}