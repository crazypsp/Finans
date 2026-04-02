using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Finans.Application.Abstractions.Identity;
using Finans.Application.Dtos.Identity;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Identity
{
    public sealed class RoleService : IRoleService
    {
        private readonly FinansDbContext _db;
        public RoleService(FinansDbContext db) => _db = db;

        public async Task<IReadOnlyList<RoleListItem>> GetRolesAsync(CancellationToken ct = default)
        {
            return await _db.Roles.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new RoleListItem { Id = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync(ct);
        }
    }
}