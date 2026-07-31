namespace CodeForge.Application.Common.Models
{
    /// <summary>
    /// Standard envelope for paginated list endpoints. See API_CONVENTIONS.md §6.
    /// </summary>
    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
}
