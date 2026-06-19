using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Web.Auth;

public class JwtTokenService(IConfiguration config)
{
    private string Secret => config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
    private string Issuer => config["Jwt:Issuer"] ?? "stuffinabox";
    private string Audience => config["Jwt:Audience"] ?? "stuffinabox";

    public TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(double.TryParse(config["Jwt:RefreshDays"], out var d) ? d : 7);

    public string GenerateAccessToken(UserId userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Cryptographically-random opaque refresh token (returned to the client, never stored raw).</summary>
    public static string GenerateRefreshTokenRaw() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>SHA-256 hex of the raw token — only this is persisted.</summary>
    public static string HashRefreshToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
