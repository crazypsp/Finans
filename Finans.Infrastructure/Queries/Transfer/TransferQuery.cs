using System.Text;
using Dapper;
using Finans.Contracts.Transfer;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Transfer
{
    public sealed class TransferQuery : ITransferQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public TransferQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<TransferListItemDto>> ListAsync(TransferFilterDto filter, CancellationToken ct)
        {
            var sql = new StringBuilder(@"
SELECT TOP (1000)
    bt.Id AS BankTransactionId,
    bt.BankId,
    b.BankName,
    bt.AccountNumber,
    bt.Iban,
    bt.BranchNo,
    bt.CustomerNo,
    bt.TransactionDate,
    bt.Description,
    bt.Amount,
    bt.Currency,
    bt.DebitCredit,
    bt.ReferenceNumber,
    bt.IsTransferred,
    bt.TransferBatchNo,
    bt.IsMatched,
    bt.MatchedCurrentCode,
    bt.MatchedCurrentName,
    CASE
        WHEN bt.IsTransferred = 1 THEN 'Transferred'
        WHEN bt.TransferBatchNo IS NOT NULL THEN 'Pending'
        ELSE 'New'
    END AS TransferStatus
FROM BankTransactions bt
INNER JOIN Banks b ON b.Id = bt.BankId
WHERE bt.CompanyId = @CompanyId
  AND bt.IsDeleted = 0
");

            if (filter.BankId.HasValue)
                sql.AppendLine("AND bt.BankId = @BankId");

            if (!string.IsNullOrWhiteSpace(filter.AccountNumber))
                sql.AppendLine("AND bt.AccountNumber = @AccountNumber");

            if (!string.IsNullOrWhiteSpace(filter.Currency))
                sql.AppendLine("AND bt.Currency = @Currency");

            if (filter.StartDate.HasValue)
                sql.AppendLine("AND bt.TransactionDate >= @StartDate");

            if (filter.EndDate.HasValue)
                sql.AppendLine("AND bt.TransactionDate <= @EndDate");

            if (filter.MinAmount.HasValue)
                sql.AppendLine("AND bt.Amount >= @MinAmount");

            if (filter.MaxAmount.HasValue)
                sql.AppendLine("AND bt.Amount <= @MaxAmount");

            if (!string.IsNullOrWhiteSpace(filter.DescriptionContains))
                sql.AppendLine("AND bt.Description LIKE '%' + @DescriptionContains + '%'");

            if (filter.OnlyNotTransferred)
                sql.AppendLine("AND bt.IsTransferred = 0");

            if (filter.OnlyPendingTransfer)
                sql.AppendLine("AND bt.TransferBatchNo IS NOT NULL AND bt.IsTransferred = 0");

            sql.AppendLine("ORDER BY bt.TransactionDate DESC, bt.Id DESC;");

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<TransferListItemDto>(sql.ToString(), new
            {
                filter.CompanyId,
                filter.BankId,
                filter.AccountNumber,
                filter.Currency,
                filter.StartDate,
                filter.EndDate,
                filter.MinAmount,
                filter.MaxAmount,
                filter.DescriptionContains
            });

            return rows.ToList();
        }
    }
}