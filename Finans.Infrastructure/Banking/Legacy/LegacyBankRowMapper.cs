using System.Globalization;
using Finans.Application.Models.Banking;

namespace Finans.Infrastructure.Banking.Legacy
{
    public static class LegacyBankRowMapper
    {
        public static BankStatementResult ToResult(IEnumerable<LegacyBankRow> rows, string? rawResponse = null)
        {
            return new BankStatementResult
            {
                IsSuccess = true,
                RawResponse = rawResponse,
                Rows = rows.Select(ToRow).ToList()
            };
        }

        public static BankStatementResult Fail(string error, string? rawResponse = null)
        {
            return new BankStatementResult
            {
                IsSuccess = false,
                Error = error,
                RawResponse = rawResponse,
                Rows = new List<BankStatementRow>()
            };
        }

        public static BankStatementRow ToRow(LegacyBankRow x)
        {
            var txDate = x.PROCESSTIME
                         ?? x.PROCESSTIME2
                         ?? SafeParseDate(x.PROCESSTIMESTR)
                         ?? SafeParseDate(x.PROCESSTIMESTR2)
                         ?? DateTime.UtcNow;

            var amount = SafeParseDecimal(x.PROCESSAMAOUNT);
            var balance = SafeParseDecimalNullable(x.PROCESSBALANCE);

            var desc = string.Join(" | ", new[] { x.PROCESSDESC, x.PROCESSDESC2, x.PROCESSDESC3, x.PROCESSDESC4 }
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));

            return new BankStatementRow
            {
                ExternalTransactionId = FirstNonEmpty(x.PROCESSID),
                ExternalUniqueKey = FirstNonEmpty(x.PROCESSID, x.PROCESSREFNO)
                    ?? $"{x.HESAPNO}|{txDate:O}|{amount.ToString(CultureInfo.InvariantCulture)}|{desc}",
                TransactionDate = txDate,
                Description = desc,
                Amount = Math.Abs(amount),
                DebitCredit = MapDebitCredit(x.PROCESSDEBORCRED),
                Currency = FirstNonEmpty(x.CURRENCYCODE, "TRY"),
                ReferenceNumber = x.PROCESSREFNO,
                BalanceAfter = balance,
                AccountNumber = x.HESAPNO,
                BranchNo = x.SUBECODE,
                Iban = FirstNonEmpty(x.PROCESSIBAN, x.FRMIBAN),
                CustomerNo = x.URF
            };
        }

        private static string MapDebitCredit(string? value)
        {
            var v = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (v is "A" or "C" or "ALACAK" or "+" or "CR" or "CREDIT") return "C";
            if (v is "B" or "D" or "BORC" or "BORÇ" or "-" or "DR" or "DEBIT") return "D";
            return "C";
        }

        private static decimal SafeParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            var v = value!.Trim();
            if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out d)) return d;
            v = v.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return 0m;
        }

        private static decimal? SafeParseDecimalNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : SafeParseDecimal(value);
        private static DateTime? SafeParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateTime.TryParse(value, out var dt)) return dt;
            if (DateTime.TryParseExact(value, new[] { "yyyyMMdd", "yyyyMMddHHmmss", "yyyy-MM-dd", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ss", "O" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;
            return null;
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
    }
}
