using System.Security.Claims;
using Finans.Contracts.Security;

namespace Finans.WebMvc.Security
{
    public interface IMenuPermissionService
    {
        bool CanSeeAdminMenu(ClaimsPrincipal user);
        bool CanSeeTransferMenu(ClaimsPrincipal user);
        bool CanSeeConnectorMenu(ClaimsPrincipal user);
        bool CanSeeReportsMenu(ClaimsPrincipal user);
        bool CanSeeUserManagementMenu(ClaimsPrincipal user);
        bool CanSeeRuleAndMappingMenus(ClaimsPrincipal user);
    }

    public sealed class MenuPermissionService : IMenuPermissionService
    {
        public bool CanSeeAdminMenu(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin);

        public bool CanSeeTransferMenu(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin)
            || user.IsInRole(RoleNames.Company)
            || user.IsInRole(RoleNames.CompanyUser)
            || user.IsInRole(RoleNames.Accountant);

        public bool CanSeeConnectorMenu(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin)
            || user.IsInRole(RoleNames.Dealer)
            || user.IsInRole(RoleNames.SubDealer);

        public bool CanSeeReportsMenu(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin)
            || user.IsInRole(RoleNames.Dealer)
            || user.IsInRole(RoleNames.SubDealer)
            || user.IsInRole(RoleNames.Company)
            || user.IsInRole(RoleNames.CompanyUser)
            || user.IsInRole(RoleNames.Accountant);

        public bool CanSeeUserManagementMenu(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin);

        public bool CanSeeRuleAndMappingMenus(ClaimsPrincipal user)
            => user.IsInRole(RoleNames.Admin)
            || user.IsInRole(RoleNames.Accountant);
    }
}
