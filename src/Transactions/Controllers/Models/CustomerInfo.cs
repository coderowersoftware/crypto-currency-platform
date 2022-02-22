namespace CodeRower.CCP.Controllers.Models
{
    public class CustomerInfo
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        //public bool? IsBlocked { get; set; }
        public string? AutoKYCStatus { get; set; }
        public string? ManualKYCStatus { get; set; }
        public string? UserId { get; set; }
        public string? UserStatus { get; set; }
        public string? WalletAddress { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? Bank { get; set; }
        public string? IFSC { get; set; }
        public string? Swift { get; set; }
        public string? CoinPaymentsAddress { get; set; }
    }
}