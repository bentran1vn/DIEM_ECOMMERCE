using System.Security.Claims;

namespace DiemEcommerce.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    (ClaimsPrincipal, bool) GetPrincipalFromExpiredToken(string token);
}