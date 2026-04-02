using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Finans.Application.Abstractions.Security;
using Finans.Application.Dtos.Auth;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Security
{
    public sealed class AuthService : IAuthService
    {
        private readonly FinansDbContext _db;
        private readonly IPasswordHasher _hasher;

        public AuthService(FinansDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var key = request.UserNameOrEmail.Trim();

            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && (x.UserName == key || x.Email == key), ct);

            if (user == null)
                return new LoginResult { IsSuccess = false, Error = "Kullanıcı bulunamadı." };

            var companyId = await _db.CompanyUsers.AsNoTracking()
    .Where(x => x.UserId == user.Id && x.IsActive)
    .Select(x => (int?)x.CompanyId)
    .FirstOrDefaultAsync(ct);

            if (!_hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt ?? ""))
                return new LoginResult { IsSuccess = false, Error = "Şifre hatalı." };

            var roleCodes = await (from ur in _db.UserRoles.AsNoTracking()
                                   join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                   where ur.UserId == user.Id && r.IsActive
                                   select r.Code)
                                  .Distinct()
                                  .ToArrayAsync(ct);

            return new LoginResult
            {
                IsSuccess = true,
                UserId = user.Id,
                UserName = user.UserName,
                CompanyId = companyId ?? 1,
                RoleCodes = roleCodes
            };
        }
    }
}