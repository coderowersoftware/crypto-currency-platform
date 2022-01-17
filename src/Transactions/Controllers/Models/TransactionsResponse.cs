using System.Runtime.Serialization;

namespace Transactions.Controllers.Models
{
    [DataContract]
    public class TransactionsResponse
    {
        [DataMember(Name = "rows", EmitDefaultValue = false)]
        public List<TransactionResponse>? Rows { get; set; }

        [DataMember(Name = "count", EmitDefaultValue = false)]
        public int Count { get; set; }
    }
}