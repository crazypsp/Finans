using Finans.Contracts.Transfer;

namespace Finans.Application.Abstractions.Transfer
{
    public interface ITransferBatchService
    {
        Task<CreateTransferBatchResultDto> CreateBatchAsync(CreateTransferBatchRequestDto request, CancellationToken ct = default);
    }
}