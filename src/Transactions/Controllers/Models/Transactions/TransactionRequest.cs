using Newtonsoft.Json;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using CodeRower.CCP.Controllers.Models.Enums;

namespace CodeRower.CCP.Controllers.Models
{
    /// <summary>
    /// AddTransactionRequest
    /// </summary>
    public class TransactionRequest
    {

        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public Currency? Currency { get; set; }

        /// <summary>
        /// Gets or Sets TransactionType
        /// </summary>
        [DataMember(Name = "transactionType", EmitDefaultValue = false)]
        public string TransactionType { get; set; }

        /// <summary>
        /// Gets or Sets Amount
        /// </summary>
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public decimal Amount { get; set; }


        /// <summary>
        /// Gets or Sets VirtualValue
        /// </summary>
        [DataMember(Name = "virtualValue", EmitDefaultValue = false)]
        public decimal? VirtualValue { get; set; }

        /// <summary>
        /// Gets or Sets IsCredit
        /// </summary>
        [DataMember(Name = "isCredit", EmitDefaultValue = false)]
        public bool IsCredit { get; set; }

        /// <summary>
        /// Gets or Sets Reference
        /// </summary>
        [DataMember(Name = "reference", EmitDefaultValue = false)]
        public string Reference { get; set; }

        /// <summary>
        /// Gets or Sets PaymentMethod
        /// </summary>
        [DataMember(Name = "paymentMethod", EmitDefaultValue = false)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Gets or Sets Remark
        /// </summary>
        [DataMember(Name = "remark", EmitDefaultValue = false)]
        public string? Remark { get; set; }

        /// <summary>
        /// Gets or Sets Description
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or Sets ProductId
        /// </summary>
        [DataMember(Name = "productId", EmitDefaultValue = false)]
        public string? ProductId { get; set; }

        /// <summary>
        /// Gets or Sets ProductName
        /// </summary>
        [DataMember(Name = "productName", EmitDefaultValue = false)]
        public string? ProductName { get; set; }

        /// <summary>
        /// Gets or Sets Sku
        /// </summary>
        [DataMember(Name = "sku", EmitDefaultValue = false)]
        public string? Sku { get; set; }

        /// <summary>
        /// Gets or Sets PayerId
        /// </summary>
        [DataMember(Name = "payerId", EmitDefaultValue = false)]
        [JsonIgnore]
        public string PayerId { get; set; }

        /// <summary>
        /// Gets or Sets PayerName
        /// </summary>
        [DataMember(Name = "payerName", EmitDefaultValue = false)]
        public string? PayerName { get; set; }

        /// <summary>
        /// Gets or Sets PayeeId
        /// </summary>
        [DataMember(Name = "payeeId", EmitDefaultValue = false)]
        public string PayeeId { get; set; }

        /// <summary>
        /// Gets or Sets PayeeName
        /// </summary>
        [DataMember(Name = "payeeName", EmitDefaultValue = false)]
        public string? PayeeName { get; set; }

        /// <summary>
        /// Gets or Sets OnBehalfOfId
        /// </summary>
        [DataMember(Name = "onBehalfOfId", EmitDefaultValue = false)]
        public string? OnBehalfOfId { get; set; }

        /// <summary>
        /// Gets or Sets OnBehalfOfName
        /// </summary>
        [DataMember(Name = "onBehalfOfName", EmitDefaultValue = false)]
        public string? OnBehalfOfName { get; set; }

        /// <summary>
        /// Gets or Sets AdditionalData
        /// </summary>
        [DataMember(Name = "additionalData", EmitDefaultValue = false)]
        public string? AdditionalData { get; set; }

        /// <summary>
        /// Gets or Sets BaseTransaction
        /// </summary>
        [DataMember(Name = "baseTransaction", EmitDefaultValue = false)]
        public string? BaseTransaction { get; set; }

        /// <summary>
        /// Gets or Sets BaseTransaction
        /// </summary>
        [DataMember(Name = "currentBalanceFor", EmitDefaultValue = false)]
        public string CurrentBalanceFor { get; set; }

        /// <summary>
        /// Gets or Sets Service
        /// </summary>
        [DataMember(Name = "service", EmitDefaultValue = false)]
        public string Service { get; set; }

        /// <summary>
        /// Gets or Sets Provider
        /// </summary>
        [DataMember(Name = "provider", EmitDefaultValue = false)]
        public string Provider { get; set; }

        /// <summary>
        /// Gets or Sets Vendor
        /// </summary>
        [DataMember(Name = "vendor", EmitDefaultValue = false)]
        public string Vendor { get; set; }

        /// <summary>
        /// Gets or Sets ExecuteCommissionAmount
        /// </summary>
        [DataMember(Name = "executeCommissionFor", EmitDefaultValue = false)]
        public string ExecuteCommissionFor { get; set; }

        /// <summary>
        /// Gets or Sets ExecuteCommissionAmount
        /// </summary>
        [DataMember(Name = "executeCommissionAmount", EmitDefaultValue = false)]
        public decimal? ExecuteCommissionAmount { get; set; }

    }
}