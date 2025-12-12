using CRM.Server.Models;
using CRM.Server.Security;                  // ✅ Your custom config class
using CRM.Server.Services.Interfaces;
using Microsoft.Extensions.Options;        // ✅ Built-in for IOptions<T>
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRM.Server.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _jwtSettings;

        // ✅ Built-in framework interface: IOptions<JwtSettings>
        // ✅ This is how your JwtSettings class is now properly used
        public JwtTokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles, string? sessionId = null)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

            // add roles
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            // add session id if provided
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                claims.Add(new Claim("session_id", sessionId));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
