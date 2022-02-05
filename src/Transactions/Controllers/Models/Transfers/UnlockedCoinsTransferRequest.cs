using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class UnlockedCoinsTransferRequest
    {
        [Required]
        public string? ToCustomerId { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}