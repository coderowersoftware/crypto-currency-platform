using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class UnlockedTransferRequest
    {
        [Required]
        [Range(double.Epsilon, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}