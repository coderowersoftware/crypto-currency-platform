using Newtonsoft.Json;
namespace Transactions.Domain.Models
{
    public class TransactionType
    {
        [JsonProperty("identifier")]
        public string? Identifier { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }
}