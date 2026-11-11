using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.Common;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int PageIndex,
    int PageSize,
    long TotalCount,
    int TotalPages
)
{
    public static PagedResult<T> Create(IEnumerable<T> items, int pageIndex, int pageSize, long totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, pageIndex, pageSize, totalCount, totalPages);
    }
}
