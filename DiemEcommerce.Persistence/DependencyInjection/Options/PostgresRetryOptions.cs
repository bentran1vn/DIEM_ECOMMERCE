using System.ComponentModel.DataAnnotations;

namespace DiemEcommerce.Persistence.DependencyInjection.Options;

public record PostgresRetryOptions
{
    [Required, Range(5, 20)] public int MaxRetryCount { get; init; }
    [Required, Timestamp] public TimeSpan MaxRetryDelay { get; init; }
    public string[]? ErrorCodesToAdd { get; init; }
}