using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Contract.Abstractions.Shared;

public class ListResult<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }
    
    private ListResult(List<T> items, int totalCount)
    { 
        Items = items;
        TotalCount = totalCount;
    }
    
    public static async Task<ListResult<T>> CreateAsync(IQueryable<T> query)
    {
        var totalCount = await query.CountAsync();
        var items = await query.ToListAsync();
        return new (items, totalCount);
    }

    public static ListResult<T> Create(List<T> items, int totalCount)
        => new (items, totalCount);
}