using Finans.Entities.Banking;

namespace Finans.Application.Abstractions.ERP
{
    /// <summary>
    /// Bankadan çekilen hesap hareketlerini Logo Tiger hesap planı ile otomatik eşleştirir.
    /// Import sonrası çağrılır.
    /// </summary>
    public interface IBankTransactionMatchingService
    {
        /// <summary>
        /// Eşleşmemiş tüm banka hareketlerini tarayarak kural ve mapping'lere göre otomatik eşleştirir.
        /// </summary>
        Task<MatchingSummary> MatchUnmatchedTransactionsAsync(int companyId, CancellationToken ct = default);

        /// <summary>
        /// Tek bir işlemi eşleştirir ve sonucu döner.
        /// </summary>
        Task<MatchingResult> MatchSingleTransactionAsync(int companyId, int bankTransactionId, CancellationToken ct = default);
    }

    public sealed class MatchingSummary
    {
        public int TotalProcessed { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
    }

    public sealed class MatchingResult
    {
        public bool IsMatched { get; set; }
        public string? CurrentCode { get; set; }
        public string? CurrentName { get; set; }
        public string? GlCode { get; set; }
        public string? GlName { get; set; }
        public string? BankAccountCode { get; set; }
        public string? BankAccountName { get; set; }
        public string? MatchSource { get; set; } // "Rule", "Mapping", "Manual"
    }
}
