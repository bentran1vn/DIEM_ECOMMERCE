using CloudinaryDotNet;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Infrastructure.DependencyInjection.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DiemEcommerce.Infrastructure.Authentication;
using DiemEcommerce.Infrastructure.PasswordHasher;
using DiemEcommerce.Infrastructure.Caching;
using DiemEcommerce.Infrastructure.Mail;
using DiemEcommerce.Infrastructure.Media;

namespace DiemEcommerce.Infrastructure.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesInfrastructure(this IServiceCollection services)
        => services
            .AddTransient<IJwtTokenService, JwtTokenService>()
            .AddTransient<IPasswordHasherService, PasswordHasherService>()
            .AddTransient<ICacheService, CacheService>()
            .AddTransient<IMediaService, CloudinaryService>()
            .AddSingleton<IMailService, MailService>()
            .AddSingleton<Cloudinary>((provider) =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<CloudinaryOptions>>();
                return new Cloudinary(new Account(
                    options.CurrentValue.CloudName,
                    options.CurrentValue.ApiKey,
                    options.CurrentValue.ApiSecret));
            });
    
    public static void AddRedisInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            var connectionString = configuration.GetConnectionString("Redis");
            redisOptions.Configuration = connectionString;
        });
    }
    
    public static OptionsBuilder<CloudinaryOptions> ConfigureCloudinaryOptionsInfrastucture(this IServiceCollection services, IConfigurationSection section)
        => services
            .AddOptions<CloudinaryOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    
    public static OptionsBuilder<VnPayOption> ConfigureVnPayOptionsInfrastucture(this IServiceCollection services, IConfigurationSection section)
        => services
            .AddOptions<VnPayOption>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    
    public static OptionsBuilder<MailOption> ConfigureMailOptionsInfrastucture(this IServiceCollection services, IConfigurationSection section)
        => services
            .AddOptions<MailOption>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}