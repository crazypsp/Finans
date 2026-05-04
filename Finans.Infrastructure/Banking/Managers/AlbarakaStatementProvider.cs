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
    public sealed class AlbarakaStatementProvider : IBankProvider
    {
        private const string DefaultEndpoint =
            "https://eservice.albarakaturk.com.tr:10214/invoiceincomingsite/HesapBilgileriService.asmx";

        private readonly IHttpClientFactory _httpClientFactory;

        public AlbarakaStatementProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public int BankId => BankIds.AlbarakaTurk;
        public string BankCode => "ALB";
        public string ProviderCode => "AlbarakaStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "ALB", "ALBARAKA" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Albaraka icin Username zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Albaraka icin Password zorunlu.");

            var endpoint = ResolveEndpoint(request.Link);
            var customerNo = request.GetExtra("customerNo") ?? request.Username;
            var accountNo = NormalizeAccountNo(request.AccountNumber);
            var requestXml = BuildSoapEnvelope(
                request.Username,
                request.Password,
                customerNo,
                accountNo,
                request.StartDate,
                request.EndDate);

            var http = _httpClientFactory.CreateClient(nameof(AlbarakaStatementProvider));
            http.Timeout = TimeSpan.FromSeconds(45);

            using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);
            msg.Headers.TryAddWithoutValidation("SOAPAction", "\"http://tempuri.org/getHesapHareketleri\"");
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            msg.Content = new StringContent(requestXml, Encoding.UTF8, "text/xml");

            using var resp = await http.SendAsync(msg, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            var responseText = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Albaraka servis HTTP {(int)resp.StatusCode}: {SafePreview(responseText)}");

            return Parse(responseText, request, customerNo);
        }

        private static Uri ResolveEndpoint(string? endpoint)
        {
            var value = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
            var wsdlIndex = value.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase);
            if (wsdlIndex >= 0)
                value = value[..wsdlIndex];

            return new Uri(value);
        }

        private static string NormalizeAccountNo(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return string.Empty;

            var value = accountNumber.Trim();
            var dashIndex = value.IndexOf('-', StringComparison.Ordinal);
            return dashIndex > 0 ? value[(dashIndex + 1)..] : value;
        }

        private static string BuildSoapEnvelope(
            string userName,
            string password,
            string customerNo,
            string accountNo,
            DateTime startDate,
            DateTime endDate)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <getHesapHareketleri xmlns=""http://tempuri.org/"">
      <pId>{XmlEscape(userName)}</pId>
      <pIdPass>{XmlEscape(password)}</pIdPass>
      <pParams>
        <musteriNo>{XmlEscape(customerNo)}</musteriNo>
        <hesapNo>{XmlEscape(accountNo)}</hesapNo>
        <basTarih>{startDate:yyyyMMdd}</basTarih>
        <sonTarih>{endDate:yyyyMMdd}</sonTarih>
      </pParams>
    </getHesapHareketleri>
  </soap:Body>
</soap:Envelope>";
        }

        private static BankStatementResult Parse(string xml, BankStatementRequest request, string customerNo)
        {
            var doc = XDocument.Parse(xml);
            var fault = doc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
            if (fault != null)
            {
                var faultText = GetDescendantValue(fault, "faultstring") ?? fault.Value;
                throw new InvalidOperationException($"Albaraka SOAP Fault: {SafePreview(faultText)}");
            }

            var error = doc.Descendants()
                .FirstOrDefault(x => x.Name.LocalName.Equals("errorMessage", StringComparison.OrdinalIgnoreCase) ||
                                     x.Name.LocalName.Equals("hataAciklama", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(error))
                return LegacyBankRowMapper.Fail(error.Trim(), xml);

            var list = new List<LegacyBankRow>();
            var accountNodes = doc.Descendants()
                .Where(x => x.HasElements && HasAnyChild(x, "hesapNo", "musteriNo") && x.Descendants().Any(d => d.Name.LocalName.Equals("hesapHareket", StringComparison.OrdinalIgnoreCase)));

            foreach (var account in accountNodes)
            {
                var accountNo = FirstNonEmpty(GetChildValue(account, "hesapNo"), NormalizeAccountNo(request.AccountNumber));
                var iban = GetChildValue(account, "hesapIban") ?? GetChildValue(account, "iban");
                var vkn = GetChildValue(account, "hesapTcknVkn") ?? GetChildValue(account, "tcknVkn");

                foreach (var trx in account.Descendants().Where(x => x.Name.LocalName.Equals("hesapHareket", StringComparison.OrdinalIgnoreCase)))
                {
                    var dateText = GetChildValue(trx, "tarih");
                    var timeText = GetChildValue(trx, "saat");
                    var processId = FirstNonEmpty(
                        GetChildValue(trx, "fisNo"),
                        GetChildValue(trx, "MuhRefNo"),
                        GetChildValue(trx, "muhRefNo"),
                        GetChildValue(trx, "Code"));

                    list.Add(new LegacyBankRow
                    {
                        BNKCODE = "ALB",
                        HESAPNO = accountNo,
                        URF = customerNo,
                        FRMIBAN = iban,
                        FRMVKN = vkn,
                        CURRENCYCODE = GetChildValue(account, "paraCinsi"),
                        PROCESSID = processId,
                        PROCESSREFNO = FirstNonEmpty(GetChildValue(trx, "MuhRefNo"), GetChildValue(trx, "muhRefNo"), processId),
                        PROCESSTIMESTR = dateText,
                        PROCESSTIMESTR2 = timeText,
                        PROCESSTIME = ParseDate(dateText, timeText),
                        PROCESSTIME2 = ParseDate(dateText, timeText),
                        PROCESSAMAOUNT = GetChildValue(trx, "islemTutari") ?? GetChildValue(trx, "tutar") ?? "0",
                        PROCESSBALANCE = FirstNonEmpty(GetChildValue(trx, "islemsonrasibakiye"), GetChildValue(trx, "bakiye")),
                        PROCESSDESC = GetChildValue(trx, "aciklama") ?? string.Empty,
                        PROCESSDESC3 = GetChildValue(trx, "karsiHesapIban"),
                        PROCESSDESC4 = GetChildValue(trx, "karsiHesapTcknVkn"),
                        PROCESSDEBORCRED = MapDebitCredit(GetChildValue(trx, "borcAlacak"), GetChildValue(trx, "islemTutari")),
                        PROCESSTYPECODE = GetChildValue(trx, "Code"),
                        Durum = 0
                    });
                }
            }

            return LegacyBankRowMapper.ToResult(list, xml);
        }

        private static bool HasAnyChild(XElement element, params string[] names)
            => names.Any(name => !string.IsNullOrWhiteSpace(GetChildValue(element, name)));

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

        private static DateTime? ParseDate(string? date, string? time)
        {
            var raw = string.Join(" ", new[] { date, time }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var formats = new[] { "yyyyMMdd HHmmss", "yyyyMMdd HH:mm:ss", "yyyyMMdd", "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy" };
            return DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
                ? parsed
                : null;
        }

        private static string MapDebitCredit(string? value, string? amount)
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized is "A" or "ALACAK" or "C" or "CREDIT" or "+")
                return "A";
            if (normalized is "B" or "BORC" or "BORÇ" or "D" or "DEBIT" or "-")
                return "B";

            return (amount ?? string.Empty).TrimStart().StartsWith("-", StringComparison.Ordinal) ? "B" : "A";
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
