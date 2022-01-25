using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Transactions.Controllers.Models.Enums;

namespace Transactions.Controllers.Models
{
    /// <summary>
    /// Transaction
    /// </summary>
    [DataContract]
    public partial class Transaction : GenericEntity, IEquatable<Transaction>, IValidatableObject
    {

        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public object? Currency { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction" /> class.
        /// </summary>
        /// <param name="transactionType">transactionType.</param>
        /// <param name="amount">amount.</param>
        /// <param name="currency">currency.</param>
        /// <param name="virtualValue">virtualValue.</param>
        /// <param name="isCredit">isCredit.</param>
        /// <param name="reference">reference.</param>
        /// <param name="paymentMethod">paymentMethod.</param>
        /// <param name="remark">remark.</param>
        /// <param name="description">description.</param>
        /// <param name="productId">productId.</param>
        /// <param name="productName">productName.</param>
        /// <param name="sku">sku.</param>
        /// <param name="payerId">payerId.</param>
        /// <param name="payerName">payerName.</param>
        /// <param name="payeeId">payeeId.</param>
        /// <param name="payeeName">payeeName.</param>
        /// <param name="onBehalfOfId">onBehalfOfId.</param>
        /// <param name="onBehalfOfName">onBehalfOfName.</param>
        /// <param name="additionalData">additionalData.</param>
        /// <param name="baseTransaction">baseTransaction.</param>
        /// <param name="transactionTypeIdentifier">transactionTypeIdentifier   .</param>
        public Transaction(TransactionType? transactionType = default(TransactionType), decimal? amount = default(decimal?), 
            object? currency = default(object?), string? currencyISO = default(string?), decimal? virtualValue = default(decimal?), 
            bool? isCredit = default(bool?), string? reference = default(string), string? paymentMethod = default(string), string? remark = default(string), string? description = default(string), string? productId = default(string), string? productName = default(string), 
            string? sku = default(string), string? payerId = default(string), string? payerName = default(string), 
            string? payeeId = default(string), string? payeeName = default(string), string? onBehalfOfId = default(string), 
            string? onBehalfOfName = default(string), string? additionalData = default(string), 
            string? baseTransaction = default(string), string? importHash = default(string), Guid? tenantId = default(Guid?), 
            string? transactionTypeIdentifier = default(string)) : base(importHash, tenantId)
        {
            this.TransactionType = transactionType;
            this.Amount = amount;
            this.Currency = currency;
            this.CurrencyISO = currencyISO;
            this.VirtualValue = virtualValue;
            this.IsCredit = isCredit;
            this.Reference = reference;
            this.PaymentMethod = paymentMethod;
            this.Remark = remark;
            this.Description = description;
            this.ProductId = productId;
            this.ProductName = productName;
            this.Sku = sku;
            this.PayerId = payerId;
            this.PayerName = payerName;
            this.PayeeId = payeeId;
            this.PayeeName = payeeName;
            this.OnBehalfOfId = onBehalfOfId;
            this.OnBehalfOfName = onBehalfOfName;
            this.AdditionalData = additionalData;
            this.BaseTransaction = baseTransaction;
            this.TransactionTypeIdentifier = transactionTypeIdentifier;
        }

        /// <summary>
        /// Gets or Sets TransactionType
        /// </summary>
        [DataMember(Name = "transactionType", EmitDefaultValue = false)]
        public TransactionType? TransactionType { get; set; }

        /// <summary>
        /// Gets or Sets Amount
        /// </summary>
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public decimal? Amount { get; set; }


        /// <summary>
        /// Gets or Sets VirtualValue
        /// </summary>
        [DataMember(Name = "virtualValue", EmitDefaultValue = false)]
        public decimal? VirtualValue { get; set; }

        /// <summary>
        /// Gets or Sets IsCredit
        /// </summary>
        [DataMember(Name = "isCredit", EmitDefaultValue = false)]
        public bool? IsCredit { get; set; }

        /// <summary>
        /// Gets or Sets Reference
        /// </summary>
        [DataMember(Name = "reference", EmitDefaultValue = false)]
        public string? Reference { get; set; }

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
        public string? PayerId { get; set; }

        /// <summary>
        /// Gets or Sets PayerName
        /// </summary>
        [DataMember(Name = "payerName", EmitDefaultValue = false)]
        public string? PayerName { get; set; }

        /// <summary>
        /// Gets or Sets PayeeId
        /// </summary>
        [DataMember(Name = "payeeId", EmitDefaultValue = false)]
        public string? PayeeId { get; set; }

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
        /// Gets or Sets Currency ISO
        /// </summary>
        [DataMember(Name = "currencyISO", EmitDefaultValue = false)]
        public string? CurrencyISO { get; set; }

        /// <summary>
        /// Gets or Sets BaseTransaction
        /// </summary>
        [DataMember(Name = "transactionTypeIdentifier", EmitDefaultValue = false)]
        public string? TransactionTypeIdentifier { get; set; }

        /// <summary>
        /// Gets or Sets BaseTransaction
        /// </summary>
        [DataMember(Name = "amounts", EmitDefaultValue = false)]
        public object? Amounts { get; set; }

        /// <summary>
        /// Gets or Sets AmountsTotal
        /// </summary>
        [DataMember(Name = "amounts_total", EmitDefaultValue = false)]
        public object? AmountsTotal { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Transaction {\n");
            sb.Append("  ").Append(base.ToString().Replace("\n", "\n  ")).Append("\n");
            sb.Append("  TransactionType: ").Append(TransactionType).Append("\n");
            sb.Append("  Amount: ").Append(Amount).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  VirtualValue: ").Append(VirtualValue).Append("\n");
            sb.Append("  IsCredit: ").Append(IsCredit).Append("\n");
            sb.Append("  Reference: ").Append(Reference).Append("\n");
            sb.Append("  PaymentMethod: ").Append(PaymentMethod).Append("\n");
            sb.Append("  Remark: ").Append(Remark).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  ProductId: ").Append(ProductId).Append("\n");
            sb.Append("  ProductName: ").Append(ProductName).Append("\n");
            sb.Append("  Sku: ").Append(Sku).Append("\n");
            sb.Append("  PayerId: ").Append(PayerId).Append("\n");
            sb.Append("  PayerName: ").Append(PayerName).Append("\n");
            sb.Append("  PayeeId: ").Append(PayeeId).Append("\n");
            sb.Append("  PayeeName: ").Append(PayeeName).Append("\n");
            sb.Append("  OnBehalfOfId: ").Append(OnBehalfOfId).Append("\n");
            sb.Append("  OnBehalfOfName: ").Append(OnBehalfOfName).Append("\n");
            sb.Append("  AdditionalData: ").Append(AdditionalData).Append("\n");
            sb.Append("  BaseTransaction: ").Append(BaseTransaction).Append("\n");
            sb.Append("  TransactionTypeIdentifier: ").Append(TransactionTypeIdentifier).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object? input)
        {
            return this.Equals(input as Transaction);
        }

        /// <summary>
        /// Returns true if Transaction instances are equal
        /// </summary>
        /// <param name="input">Instance of Transaction to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Transaction? input)
        {
            if (input == null)
                return false;

            return base.Equals(input) &&
                (
                    this.TransactionType == input.TransactionType ||
                    (this.TransactionType != null &&
                    this.TransactionType.Equals(input.TransactionType))
                ) && base.Equals(input) &&
                (
                    this.Amount == input.Amount ||
                    (this.Amount != null &&
                    this.Amount.Equals(input.Amount))
                ) && base.Equals(input) &&
                (
                    this.Currency == input.Currency ||
                    (this.Currency != null &&
                    this.Currency.Equals(input.Currency))
                ) && base.Equals(input) &&
                (
                    this.VirtualValue == input.VirtualValue ||
                    (this.VirtualValue != null &&
                    this.VirtualValue.Equals(input.VirtualValue))
                ) && base.Equals(input) &&
                (
                    this.IsCredit == input.IsCredit ||
                    (this.IsCredit != null &&
                    this.IsCredit.Equals(input.IsCredit))
                ) && base.Equals(input) &&
                (
                    this.Reference == input.Reference ||
                    (this.Reference != null &&
                    this.Reference.Equals(input.Reference))
                ) && base.Equals(input) &&
                (
                    this.PaymentMethod == input.PaymentMethod ||
                    (this.PaymentMethod != null &&
                    this.PaymentMethod.Equals(input.PaymentMethod))
                ) && base.Equals(input) &&
                (
                    this.Remark == input.Remark ||
                    (this.Remark != null &&
                    this.Remark.Equals(input.Remark))
                ) && base.Equals(input) &&
                (
                    this.Description == input.Description ||
                    (this.Description != null &&
                    this.Description.Equals(input.Description))
                ) && base.Equals(input) &&
                (
                    this.ProductId == input.ProductId ||
                    (this.ProductId != null &&
                    this.ProductId.Equals(input.ProductId))
                ) && base.Equals(input) &&
                (
                    this.ProductName == input.ProductName ||
                    (this.ProductName != null &&
                    this.ProductName.Equals(input.ProductName))
                ) && base.Equals(input) &&
                (
                    this.Sku == input.Sku ||
                    (this.Sku != null &&
                    this.Sku.Equals(input.Sku))
                ) && base.Equals(input) &&
                (
                    this.PayerId == input.PayerId ||
                    (this.PayerId != null &&
                    this.PayerId.Equals(input.PayerId))
                ) && base.Equals(input) &&
                (
                    this.PayerName == input.PayerName ||
                    (this.PayerName != null &&
                    this.PayerName.Equals(input.PayerName))
                ) && base.Equals(input) &&
                (
                    this.PayeeId == input.PayeeId ||
                    (this.PayeeId != null &&
                    this.PayeeId.Equals(input.PayeeId))
                ) && base.Equals(input) &&
                (
                    this.PayeeName == input.PayeeName ||
                    (this.PayeeName != null &&
                    this.PayeeName.Equals(input.PayeeName))
                ) && base.Equals(input) &&
                (
                    this.OnBehalfOfId == input.OnBehalfOfId ||
                    (this.OnBehalfOfId != null &&
                    this.OnBehalfOfId.Equals(input.OnBehalfOfId))
                ) && base.Equals(input) &&
                (
                    this.OnBehalfOfName == input.OnBehalfOfName ||
                    (this.OnBehalfOfName != null &&
                    this.OnBehalfOfName.Equals(input.OnBehalfOfName))
                ) && base.Equals(input) &&
                (
                    this.AdditionalData == input.AdditionalData ||
                    (this.AdditionalData != null &&
                    this.AdditionalData.Equals(input.AdditionalData))
                ) && base.Equals(input) &&
                (
                    this.BaseTransaction == input.BaseTransaction ||
                    (this.BaseTransaction != null &&
                    this.BaseTransaction.Equals(input.BaseTransaction))
                ) && base.Equals(input) &&
                (
                    this.TransactionTypeIdentifier == input.TransactionTypeIdentifier ||
                    (this.TransactionTypeIdentifier != null &&
                    this.TransactionTypeIdentifier.Equals(input.TransactionTypeIdentifier))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = base.GetHashCode();
                if (this.TransactionType != null)
                    hashCode = hashCode * 59 + this.TransactionType.GetHashCode();
                if (this.Amount != null)
                    hashCode = hashCode * 59 + this.Amount.GetHashCode();
                if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.VirtualValue != null)
                    hashCode = hashCode * 59 + this.VirtualValue.GetHashCode();
                if (this.IsCredit != null)
                    hashCode = hashCode * 59 + this.IsCredit.GetHashCode();
                if (this.Reference != null)
                    hashCode = hashCode * 59 + this.Reference.GetHashCode();
                if (this.PaymentMethod != null)
                    hashCode = hashCode * 59 + this.PaymentMethod.GetHashCode();
                if (this.Remark != null)
                    hashCode = hashCode * 59 + this.Remark.GetHashCode();
                if (this.Description != null)
                    hashCode = hashCode * 59 + this.Description.GetHashCode();
                if (this.ProductId != null)
                    hashCode = hashCode * 59 + this.ProductId.GetHashCode();
                if (this.ProductName != null)
                    hashCode = hashCode * 59 + this.ProductName.GetHashCode();
                if (this.Sku != null)
                    hashCode = hashCode * 59 + this.Sku.GetHashCode();
                if (this.PayerId != null)
                    hashCode = hashCode * 59 + this.PayerId.GetHashCode();
                if (this.PayerName != null)
                    hashCode = hashCode * 59 + this.PayerName.GetHashCode();
                if (this.PayeeId != null)
                    hashCode = hashCode * 59 + this.PayeeId.GetHashCode();
                if (this.PayeeName != null)
                    hashCode = hashCode * 59 + this.PayeeName.GetHashCode();
                if (this.OnBehalfOfId != null)
                    hashCode = hashCode * 59 + this.OnBehalfOfId.GetHashCode();
                if (this.OnBehalfOfName != null)
                    hashCode = hashCode * 59 + this.OnBehalfOfName.GetHashCode();
                if (this.AdditionalData != null)
                    hashCode = hashCode * 59 + this.AdditionalData.GetHashCode();
                if (this.BaseTransaction != null)
                    hashCode = hashCode * 59 + this.BaseTransaction.GetHashCode();
                if (this.TransactionTypeIdentifier != null)
                    hashCode = hashCode * 59 + this.TransactionTypeIdentifier.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}