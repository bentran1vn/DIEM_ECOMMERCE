using Carter;
using DiemEcommerce.API.DependencyInjection.Extensions;
using DiemEcommerce.API.Middlewares;
using DiemEcommerce.Application.DependencyInjection.Extensions;
using DiemEcommerce.Infrastructure.DependencyInjection.Extensions;
using DiemEcommerce.Infrastructure.DependencyInjection.Options;
using DiemEcommerce.Persistence.DependencyInjection.Extensions;
using DiemEcommerce.Persistence.DependencyInjection.Options;
using DotNetEnv;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Configuration
    .AddEnvironmentVariables(); // Pattern JwtOptions__SecretKey

Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging
    .ClearProviders()
    .AddSerilog();

builder.Host.UseSerilog();

// Add Carter module
builder.Services.AddCarter();
builder.Services.AddSignalR();
builder.Services
    .AddSwaggerGenNewtonsoftSupport()
    .AddFluentValidationRulesToSwagger()
    .AddEndpointsApiExplorer()
    .AddSwaggerApi();

builder.Services
    .AddApiVersioning(options => options.ReportApiVersions = true)
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.ConfigureCors();

builder.Services.AddMediatRApplication();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure Options and SQL => Remember mapcarter
builder.Services.AddInterceptorPersistence();
builder.Services.AddPostgreSqlPersistence();
builder.Services.AddRepositoryPersistence();

builder.Services.AddJwtAuthenticationApi(builder.Configuration);
builder.Services.AddServicesInfrastructure();
builder.Services.AddRedisInfrastructure(builder.Configuration);
builder.Services.ConfigureCloudinaryOptionsInfrastucture(builder.Configuration.GetSection(nameof(CloudinaryOptions)));
builder.Services.ConfigureMailOptionsInfrastucture(builder.Configuration.GetSection(nameof(MailOption)));
builder.Services.AddHttpContextAccessor();

// Add Middleware => Remember using middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

// Using middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// if (app.Environment.IsDevelopment())
    app.UseSwaggerApi();

app.UseCors("CorsPolicy");

app.UseRouting();
app.UseAuthentication(); // Need to be before app.UseAuthorization();
app.UseAuthorization();

// Add API Endpoint with carter module
app.MapCarter();

try
{
    await app.RunAsync();
    Log.Information("Stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
    Console.WriteLine(ex.Message);
    await app.StopAsync();
}
finally
{
    Log.CloseAndFlush();
    await app.DisposeAsync();
}

public partial class Program { }