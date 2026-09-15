namespace Railway.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Railway.Api";
    public string Audience { get; set; } = "Railway.Web";
    public int ExpiresMinutes { get; set; } = 60;
}
