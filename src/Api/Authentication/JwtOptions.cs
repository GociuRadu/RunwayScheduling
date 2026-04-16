using System.ComponentModel.DataAnnotations;

namespace Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "JWT";

    [Required]
    [MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 180;
}
