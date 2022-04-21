using System.Runtime.Serialization;

namespace CodeRower.CCP.Controllers.Models
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
        public decimal Amount
        {
            get => Amount;
            set => Amount = decimal.Round(Amount, 4);
        }

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