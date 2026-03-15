namespace BakeryFlow.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BakeryFlow";

    public string Audience { get; init; } = "BakeryFlow";

    public string Key { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 480;
}
