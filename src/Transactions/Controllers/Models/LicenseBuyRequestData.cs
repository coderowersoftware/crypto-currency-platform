
namespace Transactions.Controllers.Models
{
    public class LicenseBuyRequestData
    {
        public LicenseBuyRequest Data { get; set; }
    }

    public class LicenseBuyRequest
    {
        public Guid TransactionId { get; set; }
    }

}