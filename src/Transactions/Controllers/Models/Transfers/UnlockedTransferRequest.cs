using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class UnlockedTransferRequest
    {
        [Required]
        public decimal Amount { get; set; }
    }
}