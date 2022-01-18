namespace Transactions.Controllers.Models.Common
{
    public class QueryOptions
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public string OrderBy { get; set; }
        //setting up default query options
        public QueryOptions()
        {
            this.Offset = 0;
            this.Limit = 10;
        }
        //validating query option and setting tomax page to 100
        public QueryOptions(int pageNumber, int pageSize, string orderBy, string defaultOrderBy)
        {
            this.Offset = pageNumber;
            this.Limit = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
            this.OrderBy = (string.IsNullOrEmpty(orderBy) || !orderBy.Contains("_")) ? defaultOrderBy : orderBy;
        }

        public QueryOptions(int pageNumber, int pageSize, string orderBy)
        {
            this.Offset = pageNumber;
            this.Limit = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
            this.OrderBy = orderBy;
        }
    }
}