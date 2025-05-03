using DiemEcommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder builder)  {
        builder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
        

}