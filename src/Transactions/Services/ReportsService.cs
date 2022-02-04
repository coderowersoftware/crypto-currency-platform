using System.Data;
using CodeRower.CCP.Controllers.Models.Reports;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<Miner>> GetTopMiners();
    }

    public class ReportsService : IReportsService
    {
        private readonly IConfiguration _configuration;

        public ReportsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<Miner>> GetTopMiners()
        {
            var query = "get_top_miners";

            List<Miner> topMiners = new List<Miner>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
                    
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        Miner result = new Miner();
                        result.Name = Convert.ToString(reader["first_name"]);
                        result.DisplayName = Convert.ToString(reader["full_name"]);
                        result.Image = Convert.ToString(reader["image_url"]);
                        result.LicensesCount = Convert.ToInt32(reader["licenses_count"]);
                        topMiners.Add(result);
                    }
                }
            }
            return topMiners;
        }
    }
}