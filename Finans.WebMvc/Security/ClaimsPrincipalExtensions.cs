using System.Security.Claims;
using Finans.Contracts.Security;

namespace Finans.WebMvc.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("UserId");
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static int GetCompanyId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("CompanyId");
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin);

        public static bool IsDealer(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Dealer);

        public static bool IsSubDealer(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.SubDealer);

        public static bool IsAccountant(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Accountant);

        public static bool IsCompany(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Company);

        public static bool IsCompanyUser(this ClaimsPrincipal user)
            => user.IsInRole(RoleNames.CompanyUser);

        public static bool IsCompanyScopeUser(this ClaimsPrincipal user)
            => user.IsCompany() || user.IsCompanyUser() || user.IsAccountant();
    }
}
