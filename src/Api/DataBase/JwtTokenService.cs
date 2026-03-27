using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Modules.Login.Application;
using Modules.Login.Domain;

namespace Api.DataBase;

public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateToken(User user)
    {
        var key = _configuration["JWT:KEY"];
        // TODO: SECURITY: JWT signing material is loaded from configuration that is currently checked into the repo in development settings. Move secrets to user secrets or environment variables and rotate any exposed keys.
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("JWT__KEY missing.");

        var issuer = _configuration["JWT:ISSUER"];
        if (string.IsNullOrEmpty(issuer))
            throw new InvalidOperationException("JWT__ISSUER missing.");

        var audience = _configuration["JWT:AUDIENCE"];
        if (string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT__AUDIENCE missing.");

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Name, user.Username)
    };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(3),
            signingCredentials: credentials
        );
            
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
