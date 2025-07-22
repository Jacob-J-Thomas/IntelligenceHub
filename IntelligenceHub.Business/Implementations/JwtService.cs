using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Service that generates JWT tokens for the API.
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly AuthSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtService"/> class.
        /// </summary>
        public JwtService(AuthSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public Auth0Response GenerateToken(DbUser user, bool isAdmin)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Sub),
                new Claim("tenant_id", user.TenantId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim("roles", "admin"));
            }

            var token = new JwtSecurityToken(
                issuer: _settings.Domain,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);

            return new Auth0Response
            {
                AccessToken = jwt,
                ExpiresIn = (int)TimeSpan.FromHours(1).TotalSeconds,
                TokenType = "Bearer"
            };
        }
    }
}
