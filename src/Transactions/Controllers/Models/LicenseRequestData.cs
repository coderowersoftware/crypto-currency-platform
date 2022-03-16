
using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models
{
    public class LicenseRequestData
    {
        [Required]
        public LicenseRequest Data { get; set; }
    }

    public class LicenseRequest
    {
        [Required]
        public string LicenseNumber { get; set; }
    }

}