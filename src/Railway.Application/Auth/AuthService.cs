using Railway.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Railway.Persistence;
using Railway.Application.Services;

namespace Railway.Application.Auth;

public sealed class AuthService(RailwayDbContext db, IOptions<JwtOptions> jwtOptions)
{
    /// <summary>Demo credential only — replace with hashed passwords in production.</summary>
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "Demo123!";

    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        if (!request.Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase) ||
            request.Password != DemoPassword)
        {
            return null;
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item =>
            item.Email == DemoEmail);
        if (user is null)
        {
            return null;
        }

        return CreateToken(user.Id, user.Email, user.FullName);
    }

    private TokenResponse CreateToken(Guid userId, string email, string name)
    {
        var options = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(options.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires);
    }
}
