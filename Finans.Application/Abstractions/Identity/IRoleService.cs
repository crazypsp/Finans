using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Finans.Application.Dtos.Identity;

namespace Finans.Application.Abstractions.Identity
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleListItem>> GetRolesAsync(CancellationToken ct = default);
    }
}