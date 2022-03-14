using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models
{
    public class VerifyOtpRequest
    {
        [Required]
        public string? Otp { get; set; }

        [Required]
        public string Service { get; set; }
    }
}