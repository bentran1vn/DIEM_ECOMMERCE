using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Persistence.DependencyInjection.Options;
using DiemEcommerce.Persistence.Interceptors;
using DiemEcommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DiemEcommerce.Persistence.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSqlServerPersistence(this IServiceCollection services)
    {
        services.AddDbContextPool<ApplicationDbContext>((provider, builder) =>
        {
            var auditableInterceptor = provider.GetService<UpdateAuditableEntitiesInterceptor>()!;
            var deletableInterceptor = provider.GetService<DeleteAuditableEntitiesInterceptor>()!;
            var configuration = provider.GetRequiredService<IConfiguration>();
            var options = provider.GetRequiredService<IOptionsMonitor<SqlServerRetryOptions>>();

            builder
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .UseLazyLoadingProxies(true) // UseLazyLoadingProxies should be enabled if navigation properties are virtual
                .UseSqlServer(
                    connectionString: configuration.GetConnectionString("WriteConnectionString"),
                    sqlServerOptionsAction: optionsBuilder =>
                        optionsBuilder.ExecutionStrategy(dependencies => new SqlServerRetryingExecutionStrategy(
                            dependencies: dependencies,
                            maxRetryCount: options.CurrentValue.MaxRetryCount,
                            maxRetryDelay: options.CurrentValue.MaxRetryDelay,
                            errorNumbersToAdd: options.CurrentValue.ErrorNumbersToAdd))
                            .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)
                            .EnableRetryOnFailure()
                            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                )
                .AddInterceptors(auditableInterceptor, deletableInterceptor);
        });
        
        services.AddDbContextPool<ApplicationReplicateDbContext>((provider, builder) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var options = provider.GetRequiredService<IOptionsMonitor<SqlServerRetryOptions>>();

            builder
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .UseLazyLoadingProxies(true) // UseLazyLoadingProxies should be enabled if navigation properties are virtual
                .UseSqlServer(
                    connectionString: configuration.GetConnectionString("ReadConnectionString"),
                    sqlServerOptionsAction: optionsBuilder =>
                        optionsBuilder.ExecutionStrategy(dependencies => new SqlServerRetryingExecutionStrategy(
                            dependencies: dependencies,
                            maxRetryCount: options.CurrentValue.MaxRetryCount,
                            maxRetryDelay: options.CurrentValue.MaxRetryDelay,
                            errorNumbersToAdd: options.CurrentValue.ErrorNumbersToAdd))
                            .MigrationsAssembly(typeof(ApplicationReplicateDbContext).Assembly.GetName().Name)
                            .EnableRetryOnFailure()
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
    
    public static OptionsBuilder<SqlServerRetryOptions> ConfigureSqlServerRetryOptionsPersistence(this IServiceCollection services, IConfigurationSection section)
        => services
            .AddOptions<SqlServerRetryOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}