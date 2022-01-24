using Newtonsoft.Json;
namespace Transactions.Domain.Models
{
    public class Transaction
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("isCredit")]
        public bool IsCredit { get; set; }

        [JsonProperty("reference")]
        public string? Reference { get; set; }

        [JsonProperty("paymentMethod")]
        public string? PaymentMethod { get; set; }

        [JsonProperty("remark")]
        public string? Remark { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("productId")]
        public string? ProductId { get; set; }

        [JsonProperty("productName")]
        public string? ProductName { get; set; }

        [JsonProperty("sku")]
        public string? Sku { get; set; }

        [JsonProperty("payerId")]
        public string? PayerId { get; set; }

        [JsonProperty("payerName")]
        public string? PayerName { get; set; }

        [JsonProperty("payeeId")]
        public string? PayeeId { get; set; }

        [JsonProperty("payeeName")]
        public string? PayeeName { get; set; }

        [JsonProperty("onBehalfOfId")]
        public string? OnBehalfOfId { get; set; }

        [JsonProperty("onBehalfOfName")]
        public string? OnBehalfOfName { get; set; }

        [JsonProperty("additionalData")]
        public string? AdditionalData { get; set; }

        [JsonProperty("importHash")]
        public string? ImportHash { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("transactionTypeIdentifier")]
        public string? TransactionTypeIdentifier { get; set; }

        [JsonProperty("tenantId")]
        public string? TenantId { get; set; }

        [JsonProperty("createdById")]
        public string? CreatedById { get; set; }

        [JsonProperty("updatedById")]
        public string? UpdatedById { get; set; }

        //[JsonProperty("currencyISO")]
        public string? CurrencyISO { get; set; }

        [JsonProperty("currency")]
        public object? Currency { get; set; }

        [JsonProperty("transactionType")]
        public TransactionType? TransactionType { get; set; }

        [JsonProperty("virtualValue")]
        public decimal? VirtualValue { get; set; }

        [JsonProperty("amounts")]
        public object? Amounts { get; set; }
        [JsonProperty("amounts_total")]
        public object? AmountsTotal { get; set; }
    }

}