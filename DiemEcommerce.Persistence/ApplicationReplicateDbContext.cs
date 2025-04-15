using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Persistence;

public class ApplicationReplicateDbContext : DbContext
{
    public ApplicationReplicateDbContext(DbContextOptions<ApplicationReplicateDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);

}