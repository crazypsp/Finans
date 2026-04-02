using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Finans.Application.Dtos.Identity;

namespace Finans.Application.Abstractions.Identity
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken ct = default);
        Task<int> CreateUserAsync(UserCreateRequest request, CancellationToken ct = default);
        Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default);
    }
}