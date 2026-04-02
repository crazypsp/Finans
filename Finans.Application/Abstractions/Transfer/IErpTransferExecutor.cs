namespace Finans.Application.Abstractions.Transfer
{
    public interface IErpTransferExecutor
    {
        Task ExecutePendingAsync(CancellationToken ct = default);
    }
}