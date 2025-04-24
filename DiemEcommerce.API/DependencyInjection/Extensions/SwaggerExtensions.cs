using DiemEcommerce.API.DependencyInjection.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DiemEcommerce.API.DependencyInjection.Extensions;

public static class SwaggerExtensions
{
    public static void AddSwaggerApi(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. 

                    Enter 'Bearer' [space] and then your token in the text input below.

                    Example: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        },
                        Scheme = "oauth2",
                        Name = JwtBearerDefaults.AuthenticationScheme,
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
            
            c.OperationFilter<SwaggerFileOperationFilter>();
            c.EnableAnnotations();
            
        });
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    }
    
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType.IsAssignableFrom(typeof(IFormFile)) ||
                            (p.ParameterType.IsGenericType && 
                             p.ParameterType.GetGenericArguments().Any(arg => arg.IsAssignableFrom(typeof(IFormFile)))))
                .ToList();

            if (fileParameters.Count > 0)
            {
                // Ensure consumes is set correctly for file upload
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = 
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(context.MethodInfo.GetParameters()
                                .First(p => p.ParameterType
                                    .GetProperties()
                                    .Any(prop => prop.PropertyType.IsAssignableFrom(typeof(IFormFile))))
                                .ParameterType, context.SchemaRepository)
                        }
                    }
                };
            }
        }
    }

    public static void UseSwaggerApi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var version in app.DescribeApiVersions().Select(version => version.GroupName))
                options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);

            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.DocExpansion(DocExpansion.List);
        });

        app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
            .WithTags(string.Empty);
    }
    
}