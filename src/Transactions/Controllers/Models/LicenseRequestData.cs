
namespace Transactions.Controllers.Models
{
    public class LicenseRequestData
    {
        public LicenseRequest Data { get; set; }
    }

    public class LicenseRequest
    {
        public Guid LicenseId { get; set; }
    }

}