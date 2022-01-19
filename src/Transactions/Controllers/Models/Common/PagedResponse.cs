namespace Transactions.Controllers.Models.Common
{
    public class PagedResponse<T>
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        //public Uri FirstPage { get; set; }
        //public Uri LastPage { get; set; }
        //public int TotalPages { get; set; }
        public int Count { get; set; }
        //public Uri NextPage { get; set; }
        //public Uri PreviousPage { get; set; }
        public IEnumerable<T> Rows { get; set; }
        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, string orderBy, int totalRecords)
        {
            //string route = $"{request.Scheme}://{request.Host}{request.Path.Value}";
            this.Offset = pageNumber;
            this.Limit = pageSize;
            this.Rows = data;
            this.Count = totalRecords;
        }
    }
}