using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EMS.API.Authorization;

public class DynamicPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public DynamicPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy == null)
        {
            policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
        }
        return policy;
    }
}
