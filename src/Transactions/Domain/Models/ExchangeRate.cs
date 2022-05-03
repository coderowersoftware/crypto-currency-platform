namespace Transactions.Domain.Models
{
    public class ExchangeRate
    {
        public Guid? Id { get; set; }
        public decimal ValueInUSD { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedById { get; set; }
    }
}
