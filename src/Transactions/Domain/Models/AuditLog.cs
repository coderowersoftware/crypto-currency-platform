namespace Transactions.Domain.Models
{
    public class AuditLog
    {
        public Guid TenantId { get; set; }
        public string? Reference { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? Values { get; set; }
        public string? UserId { get; set; }
    }

}