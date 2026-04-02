using Finans.Application.Abstractions.Security;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Security
{
    public sealed class CompanyAuthorizationService : ICompanyAuthorizationService
    {
        private readonly FinansDbContext _db;

        public CompanyAuthorizationService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CanAccessTransferItemAsync(int companyId, int transferItemId, CancellationToken ct = default)
        {
            return await _db.ErpTransferItems
                .AnyAsync(x =>
                    x.CompanyId == companyId &&
                    x.Id == transferItemId &&
                    !x.IsDeleted,
                    ct);
        }

        public async Task<bool> CanAccessBatchAsync(int companyId, int batchId, CancellationToken ct = default)
        {
            return await _db.ErpTransferBatches
                .AnyAsync(x =>
                    x.CompanyId == companyId &&
                    x.Id == batchId &&
                    !x.IsDeleted,
                    ct);
        }
    }
}
