namespace Finans.Application.Abstractions.Banking
{
    /// <summary>
    /// Neden var?
    /// - Worker sadece periyodik tetiklesin; iş akışı Application'da kalsın.
    /// - Yarın API'den manuel import da aynı servisi çağırabilsin.
    /// </summary>
    public interface IBankImportService
    {
        Task RunImportAsync(CancellationToken ct = default);
    }
}