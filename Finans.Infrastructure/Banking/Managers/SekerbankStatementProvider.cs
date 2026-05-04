using System.Globalization;
using System.ServiceModel;
using System.Xml;
using System.Xml.Linq;
using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;
using Finans.Infrastructure.Banking.Managers.BankProviders.Infrastructure;
using SekerSrv;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class SekerbankStatementProvider : IBankProvider
    {
        private const string DefaultEndpoint =
            "http://nakityonetimi.sekerbank.com.tr/SekerbankNakitYonetimiWebservisleri/CashManagement.asmx";

        public int BankId => BankIds.Sekerbank;
        public string BankCode => "SKR";
        public string ProviderCode => "SekerbankStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "SKR", "SEKER", "SEKERBANK" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Sekerbank icin Username zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Sekerbank icin Password zorunlu.");
            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                throw new ArgumentException("Sekerbank icin AccountNumber zorunlu.");

            var customerNo = request.GetExtra("customerNo") ?? request.CustomerNoFallback();
            if (string.IsNullOrWhiteSpace(customerNo))
                throw new ArgumentException("Sekerbank icin customerNo zorunlu.");

            var endpoint = ResolveEndpoint(request.Link);
            var dateFormat = request.GetExtra("dateFormat") ?? "yyyyMMdd";

            var client = new SekerbankNakitYonetimiServisleriSoapClient(
                CreateBinding(endpoint),
                new EndpointAddress(endpoint));

            try
            {
                var response = await client.GetTransactionsByTransactionDateAsync(
                    request.Username,
                    request.Password,
                    customerNo,
                    request.AccountNumber,
                    request.StartDate.ToString(dateFormat, CultureInfo.InvariantCulture),
                    request.EndDate.ToString(dateFormat, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                var xml = response?.Body?.GetTransactionsByTransactionDateResult;
                if (xml == null)
                    return LegacyBankRowMapper.ToResult(Array.Empty<LegacyBankRow>());

                return Parse(xml, request, customerNo);
            }
            finally
            {
                client.SafeClose();
            }
        }

        private static Uri ResolveEndpoint(string? endpoint)
        {
            var value = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
            var wsdlIndex = value.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase);
            if (wsdlIndex >= 0)
                value = value[..wsdlIndex];

            return new Uri(value);
        }

        private static BasicHttpBinding CreateBinding(Uri endpoint)
        {
            var binding = new BasicHttpBinding
            {
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                AllowCookies = true,
                Security =
                {
                    Mode = endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                        ? BasicHttpSecurityMode.Transport
                        : BasicHttpSecurityMode.None
                }
            };

            return binding;
        }

        private BankStatementResult Parse(XmlElement xml, BankStatementRequest request, string customerNo)
        {
            var raw = xml.OuterXml;
            var doc = XDocument.Parse(raw);
            var root = doc.Root;
            if (root == null)
                return LegacyBankRowMapper.ToResult(Array.Empty<LegacyBankRow>(), raw);

            var error = FindProviderError(root);
            if (!string.IsNullOrWhiteSpace(error))
                return LegacyBankRowMapper.Fail(error, raw);

            var list = new List<LegacyBankRow>();
            foreach (var item in FindTransactionNodes(root))
            {
                var amount = GetFirst(item, "Amount", "Tutar", "TransactionAmount", "IslemTutari", "Miktar");
                var date = GetFirst(item, "TransactionDate", "IslemTarihi", "Date", "Tarih", "TranDate");
                var time = GetFirst(item, "Time", "Saat", "TransactionTime", "IslemSaati");
                var id = FirstNonEmpty(
                    GetFirst(item, "TransactionId", "TransactionID", "Id", "ID", "FisNo", "DekontNo"),
                    GetFirst(item, "TransactionReference", "ReferenceNumber", "ReferansNo", "RefNo"));

                if (string.IsNullOrWhiteSpace(amount) && string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(id))
                    continue;

                var parsedDate = ParseDate(date, time);
                var description = FirstNonEmpty(
                    GetFirst(item, "Description", "Explanation", "Aciklama", "Açıklama"),
                    GetFirst(item, "TransactionName", "TransactionType", "IslemTipi"));

                list.Add(new LegacyBankRow
                {
                    BNKCODE = BankCode,
                    HESAPNO = FirstNonEmpty(GetFirst(item, "AccountNo", "AccountNumber", "HesapNo"), request.AccountNumber),
                    URF = customerNo,
                    SUBECODE = GetFirst(item, "BranchCode", "SubeKodu", "TransactionBranchCode"),
                    CURRENCYCODE = GetFirst(item, "CurrencyCode", "DovizKodu", "ParaBirimi"),
                    PROCESSID = id,
                    PROCESSREFNO = FirstNonEmpty(GetFirst(item, "ReceiptNumber", "TransactionReference", "ReferenceNumber", "DekontNo"), id),
                    PROCESSTIME = parsedDate,
                    PROCESSTIME2 = parsedDate,
                    PROCESSTIMESTR = $"{date} {time}".Trim(),
                    PROCESSTIMESTR2 = date,
                    PROCESSAMAOUNT = amount ?? "0",
                    PROCESSBALANCE = GetFirst(item, "Balance", "RemainingBalance", "CurrentBalance", "Bakiye", "SonBakiye"),
                    PROCESSDESC = description ?? "",
                    PROCESSDESC2 = GetFirst(item, "TransactionCode", "IslemKodu", "Code"),
                    PROCESSIBAN = GetFirst(item, "IBAN", "Iban", "DestinationAccount"),
                    PROCESSVKN = GetFirst(item, "TaxNumber", "VKN", "TCKN", "TCKNVKN"),
                    PROCESSDEBORCRED = MapDebitCredit(GetFirst(item, "DebitCredit", "BorcAlacak", "DebitOrCredit"), amount),
                    PROCESSTYPECODE = GetFirst(item, "TransactionCode", "IslemKodu", "Code"),
                    Durum = 0
                });
            }

            return LegacyBankRowMapper.ToResult(list, raw);
        }

        private static string? FindProviderError(XElement root)
        {
            var code = GetFirst(root, "ErrorCode", "HataKodu", "ResultCode", "ReturnCode");
            var message = GetFirst(root, "ErrorMessage", "HataAciklama", "ResultMessage", "ReturnMessage", "Message");

            if (string.IsNullOrWhiteSpace(code) || code is "0" or "00" or "000")
                return null;

            return $"{code} - {message}".Trim(' ', '-');
        }

        private static IEnumerable<XElement> FindTransactionNodes(XElement root)
            => root.Descendants()
                .Where(x =>
                    x.HasElements &&
                    !string.Equals(x.Name.LocalName, root.Name.LocalName, StringComparison.OrdinalIgnoreCase) &&
                    (HasAny(x, "Amount", "Tutar", "TransactionAmount", "IslemTutari", "Miktar") ||
                     HasAny(x, "TransactionDate", "IslemTarihi", "Date", "Tarih", "TranDate") ||
                     HasAny(x, "TransactionReference", "ReferenceNumber", "ReferansNo", "RefNo")));

        private static bool HasAny(XElement element, params string[] names)
            => names.Any(name => !string.IsNullOrWhiteSpace(GetFirst(element, name)));

        private static string? GetFirst(XElement element, params string[] names)
        {
            foreach (var name in names)
            {
                var value = element.Elements()
                    .FirstOrDefault(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
                    ?.Value
                    ?.Trim();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static DateTime? ParseDate(string? date, string? time)
        {
            var raw = string.Join(" ", new[] { date, time }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var formats = new[]
            {
                "yyyyMMdd HHmmss",
                "yyyyMMdd HH:mm:ss",
                "yyyyMMdd",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd",
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy HH:mm",
                "dd.MM.yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy"
            };

            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dt))
                return dt;

            return DateTime.TryParse(raw, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt)
                ? dt
                : null;
        }

        private static string MapDebitCredit(string? value, string? amount)
        {
            var v = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (v is "A" or "C" or "ALACAK" or "CREDIT" or "+")
                return "A";
            if (v is "B" or "D" or "BORC" or "BORÇ" or "DEBIT" or "-")
                return "B";

            return (amount ?? string.Empty).TrimStart().StartsWith("-", StringComparison.Ordinal) ? "B" : "A";
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
    }

    internal static class SekerbankRequestExtensions
    {
        public static string? CustomerNoFallback(this BankStatementRequest request)
        {
            var accountNo = request.AccountNumber?.Trim();
            if (string.IsNullOrWhiteSpace(accountNo))
                return null;

            var dashIndex = accountNo.IndexOf('-', StringComparison.Ordinal);
            return dashIndex > 0 ? accountNo[..dashIndex] : accountNo;
        }
    }
}
