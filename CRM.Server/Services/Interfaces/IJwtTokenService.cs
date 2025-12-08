using CRM.Server.Models;

namespace CRM.Server.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles);
    }
}
