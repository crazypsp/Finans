using Finans.Application.Abstractions.Identity;
using Finans.Application.Abstractions.Security;
using Finans.Application.Dtos.Identity;
using Finans.Data.Context;
using Finans.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Finans.Application.Services.Identity
{
    public sealed class UserService : IUserService
    {
        private readonly FinansDbContext _db;
        private readonly IPasswordHasher _hasher;

        public UserService(FinansDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken ct = default)
        {
            var users = await _db.Users.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Select(x => new UserListItem
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    IsActive = x.IsActive,
                    IsSystemAdmin = x.IsSystemAdmin
                })
                .ToListAsync(ct);

            // Rolleri ayrı çek (EF projection içinde subquery sorunlu olabilir)
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await (from ur in _db.UserRoles.AsNoTracking()
                                   join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                   where userIds.Contains(ur.UserId)
                                   select new { ur.UserId, r.Code })
                                  .ToListAsync(ct);

            foreach (var u in users)
            {
                u.Roles = userRoles.Where(x => x.UserId == u.Id).Select(x => x.Code).ToList();
            }

            return users;
        }

        public async Task<int> CreateUserAsync(UserCreateRequest request, CancellationToken ct = default)
        {
            var exists = await _db.Users.AnyAsync(x => x.UserName == request.UserName || x.Email == request.Email, ct);
            if (exists)
                throw new InvalidOperationException("Kullanıcı adı veya e-posta zaten kayıtlı.");

            var (hash, salt) = _hasher.HashPassword(request.Password);

            var entity = new User
            {
                UserName = request.UserName.Trim(),
                Email = request.Email.Trim(),
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                IsEmailVerified = false,
                IsSystemAdmin = request.IsSystemAdmin,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Users.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default)
        {
            var exists = await _db.UserRoles.AnyAsync(x =>
                x.UserId == request.UserId &&
                x.RoleId == request.RoleId &&
                x.ScopeCompanyId == request.ScopeCompanyId, ct);

            if (exists) return;

            _db.UserRoles.Add(new UserRole
            {
                UserId = request.UserId,
                RoleId = request.RoleId,
                ScopeCompanyId = request.ScopeCompanyId,
                AssignedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}