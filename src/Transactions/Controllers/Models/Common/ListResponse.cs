namespace CodeRower.CCP.Controllers.Models.Common
{
    public class ListResponse<T>
    {
        public IEnumerable<T> Rows { get; set; }
    }
}