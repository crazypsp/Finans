using System.Globalization;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml.Linq;
using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class EmlakbankStatementProvider : IBankProvider
    {
        private const string DefaultEndpoint =
            "https://boa.emlakbank.com.tr/BOA.Integration.WCFService/BOA.Integration.AccountStatement/AccountStatementService.svc/Basic";

        private readonly IHttpClientFactory _httpClientFactory;

        public EmlakbankStatementProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public int BankId => BankIds.Emlakbank;
        public string BankCode => "EML";
        public string ProviderCode => "EmlakbankStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "EML", "EMLAK", "EMLAK_KATILIM" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Emlak Katilim icin Username zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Emlak Katilim icin Password zorunlu.");

            var endpoint = ResolveEndpoint(request.Link);
            var requestXml = BuildSoapEnvelope(request.Username, request.Password, request.StartDate, request.EndDate);

            var http = _httpClientFactory.CreateClient(nameof(EmlakbankStatementProvider));
            http.Timeout = TimeSpan.FromSeconds(45);

            using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);
            msg.Headers.TryAddWithoutValidation(
                "SOAPAction",
                "\"http://boa.net/BOA.Integration.CoreBanking.Teller/Service/IAccountStatementService/GetAccountStatement\"");
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            msg.Content = new StringContent(requestXml, Encoding.UTF8, "text/xml");

            using var resp = await http.SendAsync(msg, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            var responseText = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Emlak Katilim servis HTTP {(int)resp.StatusCode}: {SafePreview(responseText)}");

            return Parse(responseText, request);
        }

        private static Uri ResolveEndpoint(string? endpoint)
        {
            var value = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
            var wsdlIndex = value.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase);
            if (wsdlIndex >= 0)
                value = value[..wsdlIndex];

            return new Uri(value);
        }

        private static string BuildSoapEnvelope(string userName, string password, DateTime startDate, DateTime endDate)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://boa.net/BOA.Integration.CoreBanking.Teller/Service"" xmlns:boa=""http://schemas.datacontract.org/2004/07/BOA.Integration.Base"" xmlns:boa1=""http://schemas.datacontract.org/2004/07/BOA.Integration.Model.CoreBanking.Teller"">
  <soapenv:Header/>
  <soapenv:Body>
    <ser:GetAccountStatement>
      <ser:request>
        <boa:ExtUName>{XmlEscape(userName)}</boa:ExtUName>
        <boa:ExtUPassword>{XmlEscape(password)}</boa:ExtUPassword>
        <boa1:BeginDate>{startDate:yyyy-MM-dd}</boa1:BeginDate>
        <boa1:EndDate>{endDate:yyyy-MM-dd}</boa1:EndDate>
      </ser:request>
    </ser:GetAccountStatement>
  </soapenv:Body>
</soapenv:Envelope>";
        }

        private static BankStatementResult Parse(string xml, BankStatementRequest request)
        {
            var doc = XDocument.Parse(xml);
            var fault = doc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
            if (fault != null)
            {
                var faultText = GetDescendantValue(fault, "faultstring") ?? fault.Value;
                throw new InvalidOperationException($"Emlak Katilim SOAP Fault: {SafePreview(faultText)}");
            }

            var errorMessage = doc.Descendants()
                .Where(x => x.Name.LocalName.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value?.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            var hasAccount = doc.Descendants().Any(x => x.Name.LocalName.Equals("AccountContract", StringComparison.OrdinalIgnoreCase));
            if (!hasAccount && !string.IsNullOrWhiteSpace(errorMessage))
                return LegacyBankRowMapper.Fail(errorMessage!, xml);

            var requested = ParseRequestedAccount(request.AccountNumber);
            var customerNo = request.GetExtra("customerNo") ?? requested.accountNo;
            var list = new List<LegacyBankRow>();

            foreach (var account in doc.Descendants().Where(x => x.Name.LocalName.Equals("AccountContract", StringComparison.OrdinalIgnoreCase)))
            {
                var accountNo = GetChildValue(account, "AccountNumber");
                var suffix = GetChildValue(account, "AccountSuffix");

                if (!MatchesRequestedAccount(accountNo, suffix, requested))
                    continue;

                foreach (var detail in account.Descendants().Where(x => x.Name.LocalName.Equals("TransactionDetailContract", StringComparison.OrdinalIgnoreCase)))
                {
                    var amount = FirstNonEmpty(GetChildValue(detail, "Amount"), GetChildValue(detail, "Credit"), GetChildValue(detail, "Debit"), "0");
                    var tranDate = ParseDate(GetChildValue(detail, "TranDate"));
                    var valueDate = ParseDate(GetChildValue(detail, "ValueDate")) ?? tranDate;
                    var processId = FirstNonEmpty(
                        GetChildValue(detail, "TranRef"),
                        GetChildValue(detail, "BusinessKey"),
                        GetChildValue(detail, "TransactionId"),
                        $"{accountNo}-{suffix}-{tranDate:O}-{amount}");

                    list.Add(new LegacyBankRow
                    {
                        BNKCODE = "EML",
                        HESAPNO = JoinAccount(accountNo, suffix),
                        URF = customerNo,
                        SUBECODE = GetChildValue(account, "BranchId"),
                        CURRENCYCODE = FirstNonEmpty(GetChildValue(account, "FECName"), GetChildValue(account, "FECLongName")),
                        PROCESSID = processId,
                        PROCESSREFNO = FirstNonEmpty(GetChildValue(detail, "TranRef"), processId),
                        PROCESSTIMESTR = GetChildValue(detail, "TranDate"),
                        PROCESSTIMESTR2 = GetChildValue(detail, "ValueDate"),
                        PROCESSTIME = tranDate,
                        PROCESSTIME2 = valueDate,
                        PROCESSAMAOUNT = amount,
                        PROCESSBALANCE = GetChildValue(detail, "CurrentBalance"),
                        PROCESSDESC = GetChildValue(detail, "Description") ?? string.Empty,
                        PROCESSDESC2 = GetChildValue(detail, "TranType"),
                        PROCESSDESC3 = GetChildValue(detail, "ResourceCode"),
                        PROCESSIBAN = GetChildValue(account, "IBAN"),
                        FRMIBAN = GetChildValue(detail, "SenderIBAN"),
                        PROCESSVKN = GetChildValue(detail, "SenderIdentityNumber"),
                        PROCESSDEBORCRED = MapDebitCredit(GetChildValue(detail, "Credit"), GetChildValue(detail, "Debit"), amount),
                        PROCESSTYPECODE = GetChildValue(detail, "TranType"),
                        Durum = 0
                    });
                }
            }

            return LegacyBankRowMapper.ToResult(list, xml);
        }

        private static (string? accountNo, string? suffix) ParseRequestedAccount(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return (null, null);

            var raw = accountNumber.Trim();
            var parts = raw.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? (parts[0], parts[1]) : (raw, null);
        }

        private static bool MatchesRequestedAccount(string? accountNo, string? suffix, (string? accountNo, string? suffix) requested)
        {
            if (string.IsNullOrWhiteSpace(requested.accountNo))
                return true;

            if (!string.Equals(accountNo?.Trim(), requested.accountNo, StringComparison.OrdinalIgnoreCase))
                return false;

            return string.IsNullOrWhiteSpace(requested.suffix) ||
                   string.Equals(suffix?.Trim(), requested.suffix, StringComparison.OrdinalIgnoreCase);
        }

        private static string JoinAccount(string? accountNo, string? suffix)
            => string.IsNullOrWhiteSpace(suffix) ? accountNo ?? string.Empty : $"{accountNo}-{suffix}";

        private static string? GetChildValue(XElement element, string localName)
            => element.Elements()
                .FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim();

        private static string? GetDescendantValue(XElement element, string localName)
            => element.Descendants()
                .FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim();

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var formats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "dd.MM.yyyy", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var parsed))
                return parsed;

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out parsed)
                ? parsed
                : null;
        }

        private static string MapDebitCredit(string? credit, string? debit, string? amount)
        {
            if (TryParseDecimal(credit) > 0)
                return "A";
            if (TryParseDecimal(debit) > 0)
                return "B";

            return (amount ?? string.Empty).TrimStart().StartsWith("-", StringComparison.Ordinal) ? "B" : "A";
        }

        private static decimal TryParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out parsed)
                ? parsed
                : 0m;
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

        private static string XmlEscape(string? value)
            => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        private static string SafePreview(string? value)
        {
            value = (value ?? string.Empty).Trim();
            return value.Length <= 600 ? value : value[..600];
        }
    }
}
