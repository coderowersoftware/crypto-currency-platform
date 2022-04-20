using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models.Transfers
{
    public class CoinsTransferToCPRequest
    {
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public string Otp { get; set; }

        public string Message { get; set; }

        [JsonIgnore]
        public decimal FeeAmount { get; set; }
        [JsonIgnore]
        public string TransactionType { get; set; }
    }
}