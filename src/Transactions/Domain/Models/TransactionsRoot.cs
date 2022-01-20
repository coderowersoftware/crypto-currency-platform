using Newtonsoft.Json;
namespace Transactions.Domain.Models
{
    public class TransactionsRoot
    {
        [JsonProperty("rows")]
        public List<Transaction>? Rows { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }
}