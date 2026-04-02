using Finans.Contracts.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Abstractions.Transfer
{
    public interface ITransferCommandService
    {
        /// <summary>
        /// Neden var?
        /// - UI "DB'ye hangi tablolara ne yazılacak?" bilmesin.
        /// - Aktarım emri oluşturma iş kuralı tek yerde dursun.
        /// </summary>
        Task<int> CreateTransferRequestAsync(CreateTransferRequestDto request, CancellationToken ct);
    }
}
