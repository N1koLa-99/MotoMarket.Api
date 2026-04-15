namespace MotoMarket.Api.Models.Responses;



public class PagedResultResponse<T>

{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = new();
}