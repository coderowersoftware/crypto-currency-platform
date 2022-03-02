namespace CodeRower.CCP.Controllers.Models
{
    public class LicenseLog
    {
        public Guid LicenseId { get; set; }
        public string Title { get; set; }
        public DateTime ActivatedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public string Status { get; set; }
        public string LicenseType { get; set; }
        public DateTime MiningStartedAt { get; set; }
        public DateTime? MiningEndedAt { get; set; }
    }
}