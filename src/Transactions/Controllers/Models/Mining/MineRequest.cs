using System.ComponentModel.DataAnnotations;

namespace Transactions.Controllers.Models.Mining
{
    public class MineRequest
    {
        [Required]
        public Guid? LicenseId { get; set; }
    }

    public class MineRequestData
    {
        [Required]
        public MineRequest Data{ get; set; }
    }
}