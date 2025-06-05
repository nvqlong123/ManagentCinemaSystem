using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public int TotalItemCount { get; private set; } // <<< THÊM THUỘC TÍNH NÀY

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalItemCount = count; // <<< GÁN GIÁ TRỊ Ở ĐÂY

        this.AddRange(items);
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        // Đảm bảo pageIndex không nhỏ hơn 1
        pageIndex = Math.Max(1, pageIndex);
        // Đảm bảo pageIndex không vượt quá TotalPages (nếu count > 0)
        if (count > 0)
        {
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            pageIndex = Math.Min(pageIndex, totalPages);
        }


        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}