using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace CodeRower.CCP.Controllers.Models
{
    /// <summary>
    /// Add Transaction Response
    /// </summary>
    [DataContract]
    public class IdentifierProfileBalance
    {

        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "transactionid", EmitDefaultValue = false)]
        public string TransactionId{ get; set; }

        /// <summary>
        /// Gets or Sets TransactionType
        /// </summary>
        [DataMember(Name = "currentAmount", EmitDefaultValue = false)]
        public decimal CurrentAmount { get; set; }

        /// <summary>
        /// Gets or Sets Amount
        /// </summary>
        [DataMember(Name = "currentVirtualValue", EmitDefaultValue = false)]
        public decimal CurrentVirtualValue { get; set; }

        /// <summary>
        /// Gets or Sets VirtualValue
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public string Currency { get; set; }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}