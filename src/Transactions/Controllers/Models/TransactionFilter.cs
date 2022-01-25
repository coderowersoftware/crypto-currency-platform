namespace Transactions.Controllers.Models
{
    public class TransactionFilter
    {
        public string? TransactionType { get; set; }
        public List<string>? TransactionTypes { get; set; }
        public bool? IsCredit { get; set; }
        public string? Reference { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Remark { get; set; }
        public string? Description { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Sku { get; set; }
        public string? PayerId { get; set; }
        public string? PayerName { get; set; }
        public string? PayeeId { get; set; }
        public string? PayeeName { get; set; }
        public string? OnBehalfOfId { get; set; }
        public string? OnBehalfOfName { get; set; }
        public string? BaseTransaction { get; set; }
    }
}