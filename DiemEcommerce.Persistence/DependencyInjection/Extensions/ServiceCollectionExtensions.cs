using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Persistence.Interceptors;
using DiemEcommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiemEcommerce.Persistence.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPostgreSqlPersistence(this IServiceCollection services)
    {
        services.AddDbContextPool<ApplicationDbContext>((provider, builder) =>
        {
            var auditableInterceptor = provider.GetService<UpdateAuditableEntitiesInterceptor>()!;
            var deletableInterceptor = provider.GetService<DeleteAuditableEntitiesInterceptor>()!;
            var configuration = provider.GetRequiredService<IConfiguration>();

            builder
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .UseLazyLoadingProxies(true)
                .UseNpgsql(
                    connectionString: configuration.GetConnectionString("MasterConnection"),
                    npgsqlOptionsAction: optionsBuilder =>
                        optionsBuilder
                            .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)
                            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                )
                .AddInterceptors(auditableInterceptor, deletableInterceptor);
        });
        
        services.AddDbContextPool<ApplicationReplicateDbContext>((provider, builder) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();

            builder
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .UseLazyLoadingProxies(true)
                .UseNpgsql(
                    connectionString: configuration.GetConnectionString("SlaveConnection"),
                    npgsqlOptionsAction: optionsBuilder =>
                        optionsBuilder
                            .MigrationsAssembly(typeof(ApplicationReplicateDbContext).Assembly.GetName().Name)
                            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                );
        });
    }

    public static void AddInterceptorPersistence(this IServiceCollection services)
    {
        services.AddSingleton<UpdateAuditableEntitiesInterceptor>();
        services.AddSingleton<DeleteAuditableEntitiesInterceptor>();
    }

    public static void AddRepositoryPersistence(this IServiceCollection services)
    {
        services.AddTransient(typeof(IRepositoryBase<,,>), typeof(RepositoryBase<,,>));
    }
}