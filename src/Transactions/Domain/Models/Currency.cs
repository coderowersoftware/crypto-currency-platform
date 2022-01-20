using Newtonsoft.Json;
namespace Transactions.Domain.Models
{
    public class Currency
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("iso")]
        public string? Iso { get; set; }
    }
}