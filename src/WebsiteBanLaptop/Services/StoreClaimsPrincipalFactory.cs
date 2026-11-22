using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Services;

public sealed class StoreClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<int>>
{
    public StoreClaimsPrincipalFactory(UserManager<User> users,
        RoleManager<IdentityRole<int>> roles, IOptions<IdentityOptions> options)
        : base(users, roles, options) { }

    public override async Task<ClaimsPrincipal> CreateAsync(User user)
    {
        var principal = await base.CreateAsync(user);
        if (principal.Identity is ClaimsIdentity identity)
        {
            var old = identity.FindFirst(Options.ClaimsIdentity.UserNameClaimType);
            if (old != null) identity.RemoveClaim(old);
            identity.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, user.FullName));
        }
        return principal;
    }
}
