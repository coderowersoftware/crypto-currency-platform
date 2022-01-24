using System.ComponentModel.DataAnnotations;

namespace Transactions.Controllers.Models.Mining
{
    public class MineRequest
    {
        [Required]
        public Guid? LicenseId { get; set; }
    }
}