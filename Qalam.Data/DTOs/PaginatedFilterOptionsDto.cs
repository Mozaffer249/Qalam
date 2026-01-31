namespace Qalam.Data.DTOs;

public class PaginatedFilterOptionsDto
{
    public List<FilterOptionDto> Options { get; set; } = new List<FilterOptionDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
