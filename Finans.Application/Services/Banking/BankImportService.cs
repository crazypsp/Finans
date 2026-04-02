using Finans.Application.Abstractions.Banking;
using Finans.Application.Abstractions.Logging;
using Finans.Application.Models.Banking;
using Finans.Data.Context;
using Finans.Entities.Banking;
using Finans.Entities.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Finans.Application.Services.Banking
{
    /// <summary>
    /// Gerçek banka import akışını yöneten servis.
    /// - Bank + hesap + credential eşleştirir
    /// - Provider manager ile statement çeker
    /// - Payload / log / transaction kayıtlarını oluşturur
    /// </summary>
    public sealed class BankImportService : IBankImportService
    {
        private readonly FinansDbContext _db;
        private readonly IBankStatementManager _bankStatementManager;
        private readonly IBankImportValidationService _validationService;
        private readonly INotificationService _notificationService;
        private readonly Abstractions.ERP.IBankTransactionMatchingService _matchingService;

        public BankImportService(
            FinansDbContext db,
            IBankStatementManager bankStatementManager,
            IBankImportValidationService validationService,
            INotificationService notificationService,
            Abstractions.ERP.IBankTransactionMatchingService matchingService)
        {
            _db = db;
            _bankStatementManager = bankStatementManager;
            _validationService = validationService;
            _notificationService = notificationService;
            _matchingService = matchingService;
        }

        /// <summary>
        /// Import sırasında bank, hesap ve credential üçlüsünü typed olarak taşımak için.
        /// dynamic kullanmıyoruz; compile-time güvenli olsun.
        /// </summary>
        private sealed class ImportTarget
        {
            public Bank Bank { get; set; } = null!;
            public BankAccount Account { get; set; } = null!;
            public BankCredential Credential { get; set; } = null!;
        }

        public async Task RunImportAsync(CancellationToken ct = default)
        {
            // Not:
            // join ... equals ... yapısında tip uyuşmazlığı yaşanabildiği için
            // daha stabil olan from-from-where yaklaşımını kullanıyoruz.
            var importTargets = await (
                from bank in _db.Banks.AsNoTracking()
                from account in _db.BankAccounts.AsNoTracking()
                from credential in _db.BankCredentials.AsNoTracking()
                where bank.IsActive && !bank.IsDeleted
                   && account.IsActive && !account.IsDeleted
                   && credential.IsActive && !credential.IsDeleted
                   && bank.CompanyId == account.CompanyId
                   && bank.Id == account.BankId
                   && bank.CompanyId == credential.CompanyId
                   && bank.Id == credential.BankId
                select new ImportTarget
                {
                    Bank = bank,
                    Account = account,
                    Credential = credential
                }
            )
            .OrderBy(x => x.Bank.CompanyId)
            .ThenBy(x => x.Bank.Id)
            .ThenBy(x => x.Account.Id)
            .ToListAsync(ct);

            foreach (var target in importTargets)
            {
                ct.ThrowIfCancellationRequested();

                await ImportSingleAccountAsync(
                    target.Bank,
                    target.Account,
                    target.Credential,
                    ct);
            }

            // Import sonrası otomatik eşleştirme
            // Her firma için eşleşmemiş işlemleri Logo Tiger hesap planı ile eşleştir
            var companyIds = importTargets.Select(x => x.Bank.CompanyId).Distinct();
            foreach (var companyId in companyIds)
            {
                try
                {
                    await _matchingService.MatchUnmatchedTransactionsAsync(companyId, ct);
                }
                catch
                {
                    // Eşleştirme hatası import'u engellemesin
                }
            }
        }

        private async Task<int> UpsertTransactionWithoutSaveAsync(
    int companyId,
    int bankId,
    string accountNumber,
    string? iban,
    string? branchNo,
    string? customerNo,
    string? currency,
    BankStatementRow row,
    CancellationToken ct)
        {
            var existing = await _db.BankTransactions
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.ExternalUniqueKey == row.ExternalUniqueKey,
                    ct);

            if (existing == null)
            {
                _db.BankTransactions.Add(new BankTransaction
                {
                    CompanyId = companyId,
                    BankId = bankId,
                    AccountNumber = accountNumber,
                    Iban = iban,
                    BranchNo = branchNo,
                    CustomerNo = customerNo,
                    TransactionDate = row.TransactionDate,
                    Description = row.Description,
                    Amount = row.Amount,
                    Currency = string.IsNullOrWhiteSpace(row.Currency) ? (currency ?? "TRY") : row.Currency,
                    DebitCredit = row.DebitCredit,
                    ReferenceNumber = row.ReferenceNumber,
                    BalanceAfterTransaction = row.BalanceAfter,
                    ExternalUniqueKey = row.ExternalUniqueKey,
                    IsMatched = false,
                    IsTransferred = false,                   
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                });

                return 1;
            }

            existing.Description = row.Description ?? existing.Description;
            existing.ReferenceNumber = row.ReferenceNumber ?? existing.ReferenceNumber;
            existing.BalanceAfterTransaction = row.BalanceAfter ?? existing.BalanceAfterTransaction;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            return 2;
        }

        private static BankStatementRequest BuildRequest(
            Bank bank,
            BankAccount account,
            BankCredential credential)
        {
            var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Credential.ExtrasJson varsa parse et
            if (!string.IsNullOrWhiteSpace(credential.ExtrasJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(credential.ExtrasJson);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                            extras[kv.Key] = kv.Value;
                    }
                }
                catch
                {
                    // invalid json ise import patlamasın
                }
            }

            // BankAccount alanlarını extras'a koyuyoruz.
            if (!string.IsNullOrWhiteSpace(account.Iban))
                extras["iban"] = account.Iban;

            if (!string.IsNullOrWhiteSpace(account.BranchNo))
                extras["branchNo"] = account.BranchNo;

            if (!string.IsNullOrWhiteSpace(account.CustomerNo))
                extras["customerNo"] = account.CustomerNo;

            return new BankStatementRequest
            {
                BankId = bank.Id,
                ProviderCode = bank.ProviderCode,
                Username = credential.Username,
                Password = credential.Password,
                AccountNumber = account.AccountNumber,
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today,
                Link = bank.DefaultLink,
                TLink = bank.DefaultTLink,
                Extras = extras
            };
        }

        private async Task<int> UpsertTransactionAsync(
            int companyId,
            int bankId,
            string accountNumber,
            string? iban,
            string? branchNo,
            string? customerNo,
            string? currency,
            BankStatementRow row,
            CancellationToken ct)
        {
            var existing = await _db.BankTransactions
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.ExternalUniqueKey == row.ExternalUniqueKey,
                    ct);

            if (existing == null)
            {
                var entity = new BankTransaction
                {
                    CompanyId = companyId,
                    BankId = bankId,
                    AccountNumber = accountNumber,
                    Iban = iban,
                    BranchNo = branchNo,
                    CustomerNo = customerNo,
                    TransactionDate = row.TransactionDate,
                    Description = row.Description,
                    Amount = row.Amount,
                    Currency = string.IsNullOrWhiteSpace(row.Currency) ? (currency ?? "TRY") : row.Currency,
                    DebitCredit = row.DebitCredit,
                    ReferenceNumber = row.ReferenceNumber,
                    BalanceAfterTransaction = row.BalanceAfter,
                    ExternalUniqueKey = row.ExternalUniqueKey,
                    IsMatched = false,
                    IsTransferred = false,
                    IsDeleted = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.BankTransactions.Add(entity);
                await _db.SaveChangesAsync(ct);
                return 1; // inserted
            }

            existing.Description = row.Description ?? existing.Description;
            existing.ReferenceNumber = row.ReferenceNumber ?? existing.ReferenceNumber;
            existing.BalanceAfterTransaction = row.BalanceAfter ?? existing.BalanceAfterTransaction;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return 2; // updated
        }

        private async Task WritePayloadAsync(
            int companyId,
            int bankId,
            string requestJson,
            string? responseText,
            bool isSuccess,
            string? errorMessage,
            DateTime executedAtUtc,
            CancellationToken ct)
        {
            var entity = new BankApiPayload
            {
                CompanyId = companyId,
                BankId = bankId,
                Operation = "StatementImport",
                RequestText = requestJson,
                ResponseText = responseText,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                ExecutedAtUtc = executedAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.BankApiPayloads.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        private async Task WriteIntegrationLogAsync(
            int companyId,
            int bankId,
            string status,
            Finans.Entities.Common.LogLevel level,
            string operation,
            string message,
            CancellationToken ct)
        {
            var entity = new BankIntegrationLog
            {
                CompanyId = companyId,
                BankId = bankId,
                Status = status,
                Level = level,
                Operation = operation,
                ErrorMessage = message,
                OccurredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.BankIntegrationLogs.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        private async Task ImportSingleAccountAsync(
    Bank bank,
    BankAccount account,
    BankCredential credential,
    CancellationToken ct)
        {
            var validationErrors = _validationService.Validate(bank, account, credential);
            if (validationErrors.Count > 0)
            {
                _db.BankIntegrationLogs.Add(new BankIntegrationLog
                {
                    CompanyId = bank.CompanyId,
                    BankId = bank.Id,
                    Status = "ValidationFailed",
                    Level = Finans.Entities.Common.LogLevel.Error,
                    Operation = "StatementImport",
                    ErrorMessage = string.Join(" | ", validationErrors),
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                });

                await _db.SaveChangesAsync(ct);
                return;
            }

            var request = BuildRequest(bank, account, credential);
            BankStatementResult result;

            try
            {
                result = await _bankStatementManager.GetStatementAsync(request, ct);              

            }
            catch (Exception ex)
            {
                _db.BankApiPayloads.Add(new BankApiPayload
                {
                    CompanyId = bank.CompanyId,
                    BankId = bank.Id,
                    Operation = "StatementImport",
                    RequestText = SerializeRequest(request),
                    ResponseText = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ExecutedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                });

                _db.BankIntegrationLogs.Add(new BankIntegrationLog
                {
                    CompanyId = bank.CompanyId,
                    BankId = bank.Id,
                    Status = "Exception",
                    Level = Finans.Entities.Common.LogLevel.Error,
                    Operation = "StatementImport",
                    ErrorMessage = ex.Message,
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                });

                await _db.SaveChangesAsync(ct);
                return;
            }

            _db.BankApiPayloads.Add(new BankApiPayload
            {
                CompanyId = bank.CompanyId,
                BankId = bank.Id,
                Operation = "StatementImport",
                RequestText = SerializeRequest(request),
                ResponseText = result.RawResponse,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.Error,
                ExecutedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            });

            if (!result.IsSuccess)
            {
                _db.BankIntegrationLogs.Add(new BankIntegrationLog
                {
                    CompanyId = bank.CompanyId,
                    BankId = bank.Id,
                    Status = "Failed",
                    Level = Finans.Entities.Common.LogLevel.Error,
                    Operation = "StatementImport",
                    ErrorMessage = result.Error ?? "Bilinmeyen provider hatası",
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                });

                await _db.SaveChangesAsync(ct);
                return;
            }

            var insertedCount = 0;
            var updatedCount = 0;

            foreach (var row in result.Rows)
            {
                ct.ThrowIfCancellationRequested();

                var upsertResult = await UpsertTransactionWithoutSaveAsync(
                    companyId: bank.CompanyId,
                    bankId: bank.Id,
                    accountNumber: account.AccountNumber,
                    iban: account.Iban,
                    branchNo: account.BranchNo,
                    customerNo: account.CustomerNo,
                    currency: account.Currency,
                    row: row,
                    ct: ct);

                if (upsertResult == 1) insertedCount++;
                else if (upsertResult == 2) updatedCount++;
            }

            _db.BankIntegrationLogs.Add(new BankIntegrationLog
            {
                CompanyId = bank.CompanyId,
                BankId = bank.Id,
                Status = "Success",
                Level = Finans.Entities.Common.LogLevel.Info,
                Operation = "StatementImport",
                ErrorMessage = $"Inserted={insertedCount}, Updated={updatedCount}, TotalRows={result.Rows.Count}",
                OccurredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            });

            await _db.SaveChangesAsync(ct);
        }

        private static string SerializeRequest(BankStatementRequest request)
        {
            return JsonSerializer.Serialize(new
            {
                request.BankId,
                request.ProviderCode,
                request.Username,
                request.AccountNumber,
                request.StartDate,
                request.EndDate,
                request.Link,
                request.TLink,
                request.Extras
            });
        }
    }
}