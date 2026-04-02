using Finans.Contracts.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Infrastructure.Queries.Transfer
{
    public interface ITransferQuery
    {
        Task<IReadOnlyList<TransferListItemDto>> ListAsync(TransferFilterDto filter, CancellationToken ct);
    }
}
