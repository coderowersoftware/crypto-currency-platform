
using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models
{
    public class LicenseBuyRequestData
    {
        [Required]
        public LicenseBuyRequest Data { get; set; }
    }

    public class LicenseBuyRequest
    {
        [Required]
        public string TransactionId { get; set; }
        public string LicenseType { get; set; } = "POOL";
    }

}