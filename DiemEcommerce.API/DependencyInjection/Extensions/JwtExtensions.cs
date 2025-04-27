using System.Text;
using DiemEcommerce.Contract.Constant.SystemRoles;
using DiemEcommerce.Infrastructure.DependencyInjection.Options;
using DiemEcommerce.Persistence.Constrants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DiemEcommerce.API.DependencyInjection.Extensions;

public static class JwtExtensions
{
    public static void AddJwtAuthenticationApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            JwtOption jwtOption = new JwtOption();
            configuration.GetSection(nameof(JwtOption)).Bind(jwtOption);
            
            o.SaveToken = true; // Save token into AuthenticationProperties

            var key = Encoding.UTF8.GetBytes(jwtOption.SecretKey);

            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, // on production make it true
                ValidateAudience = true, // on production make it true
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOption.Issuer,
                ValidAudience = jwtOption.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5), 
            };

            o.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("IS-TOKEN-EXPIRED", "true");
                    }

                    return Task.CompletedTask;
                }
            };

            // o.EventsType = typeof(CustomJwtBearerEvents);
        });

        services.AddAuthorization(
            opts =>
         {
             opts.AddPolicy(RoleNames.Customer, policy => policy.RequireRole(RoleNames.Customer));
             opts.AddPolicy(RoleNames.Factory, policy => policy.RequireRole(RoleNames.Factory));
             opts.AddPolicy(RoleNames.Admin, policy => policy.RequireRole(RoleNames.Admin));
             opts.AddPolicy(RoleNames.CustomerFactory, policy => policy.RequireRole(RoleNames.Customer, RoleNames.Factory));
             opts.AddPolicy(RoleNames.CustomerAdmin, policy => policy.RequireRole(RoleNames.Customer, RoleNames.Admin));
             opts.AddPolicy(RoleNames.FactoryAdmin, policy => policy.RequireRole(RoleNames.Factory, RoleNames.Admin));
             opts.AddPolicy(RoleNames.CustomerFactoryAdmin, policy => policy.RequireRole(RoleNames.Customer, RoleNames.Factory, RoleNames.Admin));
         });
        
         // services.AddScoped<CustomJwtBearerEvents>();
    }
}