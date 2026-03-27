using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IAuthService authService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                AttachUserToContext(context, authService, token);
            }

            await _next(context);
        }

        private void AttachUserToContext(HttpContext context, IAuthService authService, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ??
                    throw new InvalidOperationException("JWT Key not configured"));

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var regNo = jwtToken.Claims.FirstOrDefault(x => x.Type == "RegNo")?.Value;
                var username = jwtToken.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value;

                if (!string.IsNullOrEmpty(regNo))
                {
                    // Attach user to context
                    context.Items["RegNo"] = regNo;
                    context.Items["Username"] = username ?? regNo;

                    _logger.LogDebug("User attached to context: {RegNo}", regNo);
                }
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token expired");
                // Token expired - don't attach user
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                // Token validation failed - do nothing
            }
        }
    }
}