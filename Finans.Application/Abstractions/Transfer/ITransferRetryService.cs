namespace Finans.Application.Abstractions.Transfer
{
    public interface ITransferRetryService
    {
        Task RetryItemAsync(int companyId, int itemId, CancellationToken ct = default);
        Task RetryItemsAsync(int companyId, IReadOnlyList<int> itemIds, CancellationToken ct = default);
    }
}