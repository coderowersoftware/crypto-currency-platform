namespace CodeRower.CCP.Controllers.Models
{
    public class TransactionBook
    {
        public string TransactionBookId { get; set; }
        public decimal Amount { get; set; }
        public string GatewayTransactionId { get; set; }
        public string GatewayResponse { get; set; }
        public string CallbackStatus { get; set; }
        public string CallbackResponse { get; set; }
        public string Status { get; set; }
        public bool IsCredit { get; set; }
        public string WalletTransactionStatus { get; set; }
        public string WalletResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UserId { get; set; }
        public string CustomerId { get; set; }
    }
}
