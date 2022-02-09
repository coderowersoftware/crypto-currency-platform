using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class CoinsTransferRequest
    {
        [Required]
        public string? ToCustomerId { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}