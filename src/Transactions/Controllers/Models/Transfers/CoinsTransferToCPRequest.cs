using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class CoinsTransferToCPRequest
    {
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public string Otp { get; set; }
    }
}