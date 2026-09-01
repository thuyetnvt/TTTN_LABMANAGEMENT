using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Dtos;

public sealed class PageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [MaxLength(200)]
    public string? Search { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public int? CategoryId { get; set; }
    public int? LocationNodeId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public string NormalizedSearch => Search?.Trim() ?? string.Empty;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageQuery paging,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, paging.Page);
        var pageSize = Math.Clamp(paging.PageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<T>(items, total, page, pageSize, totalPages);
    }

    public static PagedResult<TResult> Map<TSource, TResult>(
        this PagedResult<TSource> source,
        Func<TSource, TResult> mapper)
    {
        return new PagedResult<TResult>(
            source.Items.Select(mapper).ToList(),
            source.Total,
            source.Page,
            source.PageSize,
            source.TotalPages);
    }
}
