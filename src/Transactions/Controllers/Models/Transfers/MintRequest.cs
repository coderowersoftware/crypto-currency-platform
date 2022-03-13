using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class MintRequest
    {
        public string? Otp { get; set; }
        public string? AccountPin { get; set; }
        [Required]
        public decimal Amount { get; set; }
    }
}