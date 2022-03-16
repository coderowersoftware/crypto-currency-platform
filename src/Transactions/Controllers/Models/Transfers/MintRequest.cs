using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class MintRequest
    {
        [Required]
        public string? Otp { get; set; }

        [Required]
        [Range(double.Epsilon, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}