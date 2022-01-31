using System.Runtime.Serialization;

namespace Transactions.Controllers.Models
{
    /// <summary>
    /// Transaction Type Balance
    /// </summary>
    [DataContract]
    public class TransactionTypeBalance
    {
        /// <summary>
        /// Transaction Type
        /// </summary>
        [DataMember(Name = "transactionType", EmitDefaultValue = false)]
        public string TransactionType { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public string Currency { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Virtual Value
        /// </summary>
        [DataMember(Name = "virtualValue")]
        public decimal VirtualValue { get; set; }
        /// <summary>
        /// Total Virtual Value
        /// </summary>
        [DataMember(Name = "totalVirtualValue")]
        public decimal TotalVirtualValue { get; set; }

    }
}