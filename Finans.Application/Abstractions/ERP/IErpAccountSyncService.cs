using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Abstractions.ERP
{
    public interface IErpAccountSyncService
    {
        Task SyncGlAccountsAsync(int companyId, CancellationToken ct = default);
        Task SyncCurrentAccountsAsync(int companyId, CancellationToken ct = default);
        Task SyncBankAccountsAsync(int companyId, CancellationToken ct = default);
    }
}
