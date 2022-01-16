namespace Transactions.Controllers.Models
{
    public class TransactionsResponse
    {
        public List<TransactionResponse>? Transactions { get; set; }
        public int Count { get; set; }
    }
}