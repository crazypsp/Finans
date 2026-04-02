using Finans.Application.Models.Banking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Abstractions.Banking
{
    public interface IBankStatementManager
    {
        Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default);
    }
}
