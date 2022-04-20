using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class CoinsTransferToCPSettleRequestData
    {
        [Required]
        public CoinsTransferToCPSettleRequest Data { get; set; }
    }

    public class CoinsTransferToCPSettleRequest
    {
        [Required]
        public string TransactionId { get; set; }
        [Required]
        public string AuthKey { get; set; }

    }
}