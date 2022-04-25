
namespace CodeRower.CCP.Controllers.Domain
{
    public class CoinsTransferToCPRequestDTO
    {
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public decimal? FeeAmount { get; set; }
        public string? TransactionType { get; set; }
        public decimal? FeeAmountInCC { get; set; }
        public decimal? AmountInCC { get; set; }
        public string Currency { get; set; }
    }
}