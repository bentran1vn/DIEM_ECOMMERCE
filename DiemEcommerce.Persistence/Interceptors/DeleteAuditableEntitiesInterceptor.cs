using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DiemEcommerce.Persistence.Interceptors;

public sealed class DeleteAuditableEntitiesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DbContext? dbContext = eventData.Context;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        IEnumerable<EntityEntry> entries =
            dbContext
                .ChangeTracker
                .Entries();
        
        foreach (EntityEntry entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Deleted)
            {
                var entity = entityEntry.Entity;
                var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
                var updateProperty = entity.GetType().GetProperty("ModifiedOnUtc");
                if (isDeletedProperty != null && isDeletedProperty.CanWrite && updateProperty != null && updateProperty.CanWrite)
                {
                    entityEntry.State = EntityState.Modified;
                    isDeletedProperty.SetValue(entity, true);
                    updateProperty.SetValue(entity, DateTimeOffset.UtcNow);
                }
            }
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}