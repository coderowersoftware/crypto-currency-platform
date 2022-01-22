using System.Runtime.Serialization;

namespace Transactions.Controllers.Models
{
    /// <summary>
    /// Add Transaction Response
    /// </summary>
    [DataContract]
    public class TransactionRequestData
    {
        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public TransactionRequest Data{ get; set; }

    }
}