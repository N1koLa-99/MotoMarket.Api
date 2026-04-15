namespace MotoMarket.Api.Models.Responses;

public class LookupItemResponse
{
    public int Id { get; set; }
    public string GroupName { get; set; } = default!;
    public int? ParentId { get; set; }
    public string Code { get; set; } = default!;
    public string NameBg { get; set; } = default!;
    public int SortOrder { get; set; }
}