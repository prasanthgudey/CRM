using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CRM.Client.Security
{
    // ✅ FRAMEWORK INTERFACE: IAuthorizationPolicyProvider
    // ✅ USER-DEFINED ROLE POLICY PROVIDER
    public class RolePolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public RolePolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallbackPolicyProvider.GetFallbackPolicyAsync();

        // ✅ Dynamically builds policy like:
        // [Authorize(Roles = "Admin")]
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireRole(policyName)
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
