using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;
using KuveytTurkSrv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finans.Infrastructure.Banking.Managers.BankProviders.Infrastructure;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class KuveytTurkStatementProvider : IBankProvider
    {
        public int BankId => BankIds.KuveytTurk;
        public string BankCode => "KTB";
        public string ProviderCode => "KuveytTurkStatementProvider";

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            var list = new List<LegacyBankRow>();

            var parts = request.AccountNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int accountNumber = int.Parse(parts[0]);
            short accountSuffix = (short)(parts.Length > 1 ? short.Parse(parts[1]) : 0);

            AccountStatementServiceClient client = string.IsNullOrWhiteSpace(request.Link)
                ? new AccountStatementServiceClient(AccountStatementServiceClient.EndpointConfiguration.BasicHttpBinding_IAccountStatementService)
                : new AccountStatementServiceClient(AccountStatementServiceClient.EndpointConfiguration.BasicHttpBinding_IAccountStatementService, request.Link);

            try
            {
                var req = new AccountStatetmentRequest
                {
                    ExtUName = request.Username,
                    ExtUPassword = request.Password ?? "",
                    AccountNumber = accountNumber,
                    //AccountSuffix = accountSuffix.ToString(), // Reference: AccountSuffix string
                    BeginDate = request.StartDate,
                    EndDate = request.EndDate,
                };

                var resp = await client.GetAccountStatementAsync(req).ConfigureAwait(false);
                if (resp == null || resp.Value == null) return LegacyBankRowMapper.ToResult(list);

                foreach (var acc in resp.Value)
                {
                    foreach (var d in acc.Details ?? Array.Empty<TransactionDetailContract>())
                    {
                        // Kuveyt’te Debit/Credit ayrı: daha doğru borç/alacak bununla çıkar.
                        var isCredit = d.Credit > 0;
                        var amount = isCredit ? d.Credit : -d.Debit;

                        list.Add(new LegacyBankRow
                        {
                            BNKCODE = BankCode,
                            HESAPNO = request.AccountNumber,
                            FRMIBAN = acc.IBAN,
                            PROCESSVKN = acc.TCKNorTaxNumber,

                            PROCESSID = d.BusinessKey?.ToString(),
                            PROCESSTIME = d.TranDate,
                            PROCESSTIME2 = d.ValueDate,
                            PROCESSTIMESTR = d.TranDate.ToString("yyyyMMdd"),
                            PROCESSTIMESTR2 = d.ValueDate?.ToString("yyyyMMdd"),

                            PROCESSREFNO = d.TranRef,
                            PROCESSAMAOUNT = amount.ToString(),
                            PROCESSBALANCE = d.CurrentBalance.ToString(),
                            PROCESSDESC = d.Description,
                            PROCESSDESC2 = d.TranType,
                            PROCESSDEBORCRED = isCredit ? "A" : "B",
                            PROCESSTYPECODE = d.FECName,
                            PROCESSTYPECODEMT940 = d.SwiftTransactionCode,
                            Durum = 0
                        });
                    }
                }

                return LegacyBankRowMapper.ToResult(list);
            }
            finally
            {
                client.SafeClose();
            }
        }
    }
}
