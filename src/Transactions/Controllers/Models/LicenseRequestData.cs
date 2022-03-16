
namespace CodeRower.CCP.Controllers.Models
{
    public class LicenseRequestData
    {
        public LicenseRequest Data { get; set; }
    }

    public class LicenseRequest
    {
        public Guid LicenseNumber { get; set; }
    }

}