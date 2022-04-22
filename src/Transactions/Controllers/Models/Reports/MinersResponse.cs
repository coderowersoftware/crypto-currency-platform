using CodeRower.CCP.Controllers.Models.Reports;

namespace Transactions.Controllers.Models.Reports
{
    public class MinersReponse
    {
        public IEnumerable<Miner> Miners { get; set; }
        public int Count { get; set; }
    }
}
