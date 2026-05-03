using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class QnbMaestroStatementProvider : IBankProvider
    {
        private const string DefaultSoap11Endpoint =
            "http://fbmaestro.qnb.com.tr:9086/MaestroCoreEkstre/services/TeknikBaglantiService.TeknikBaglantiServiceHttpSoap11Endpoint/";

        public int BankId => BankIds.QnbFinans;
        public string BankCode => "QNB";
        public string ProviderCode => "QnbMaestroStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "QNB", "QNB_FINANS" };

        private readonly IHttpClientFactory _httpFactory;

        public QnbMaestroStatementProvider(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("QNB icin Username zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("QNB icin Password zorunlu.");
            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                throw new ArgumentException("QNB icin AccountNumber zorunlu.");

            var endpoint = ResolveEndpoint(request.Link);
            var soapXml = BuildSoapEnvelope(
                request.Username,
                request.Password,
                request.AccountNumber,
                request.StartDate,
                request.EndDate,
                request.GetExtra("iban"));

            var http = _httpFactory.CreateClient(nameof(QnbMaestroStatementProvider));
            http.Timeout = TimeSpan.FromSeconds(90);

            using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);
            msg.Headers.TryAddWithoutValidation("SOAPAction", "\"urn:getTransactionInfo\"");
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            msg.Content = new StringContent(soapXml, Encoding.UTF8, "text/xml");

            using var resp = await http.SendAsync(msg, ct).ConfigureAwait(false);
            var respText = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"QNB servis HTTP {(int)resp.StatusCode}: {SafePreview(respText)}");

            return ParseResponseToRows(respText, request);
        }

        private static string ResolveEndpoint(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return DefaultSoap11Endpoint;

            var endpoint = link.Trim();
            if (endpoint.Contains("?wsdl", StringComparison.OrdinalIgnoreCase) ||
                endpoint.EndsWith("/TeknikBaglantiService", StringComparison.OrdinalIgnoreCase))
                return DefaultSoap11Endpoint;

            return endpoint.EndsWith("/", StringComparison.Ordinal)
                ? endpoint
                : endpoint + "/";
        }

        private static string BuildSoapEnvelope(
            string userName,
            string password,
            string accountNo,
            DateTime start,
            DateTime end,
            string? iban)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://teknikbaglantiekstre.genericekstrenew2.driver.maestro.ibtech.com";

            var body =
                new XElement(ns + "getTransactionInfo",
                    new XElement("transactionInfo",
                        new XElement("password", password),
                        new XElement("transactionInfoInputType",
                            new XElement("accountNo", accountNo),
                            new XElement("startDate", start.ToString("o", CultureInfo.InvariantCulture)),
                            new XElement("endDate", end.ToString("o", CultureInfo.InvariantCulture)),
                            string.IsNullOrWhiteSpace(iban) ? null : new XElement("iban", iban)
                        ),
                        new XElement("userName", userName)
                    )
                );

            var doc =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement(soap + "Envelope",
                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                        new XAttribute(XNamespace.Xmlns + "ns", ns),
                        new XElement(soap + "Header"),
                        new XElement(soap + "Body", body)
                    )
                );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private static BankStatementResult ParseResponseToRows(string xml, BankStatementRequest request)
        {
            var doc = XDocument.Parse(xml);

            var fault = doc.Descendants()
                .FirstOrDefault(x => x.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
            if (fault != null)
            {
                var faultString =
                    fault.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("faultstring", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? fault.Value;
                throw new Exception($"QNB SOAP Fault: {SafePreview(faultString)}");
            }

            var ret = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "return");
            if (ret == null)
                return LegacyBankRowMapper.ToResult(Array.Empty<LegacyBankRow>(), xml);

            var errorCode = GetVal(ret, "errorCode");
            var errorDesc = GetVal(ret, "errorDescription");
            if (!string.IsNullOrWhiteSpace(errorCode) && errorCode != "0")
                throw new Exception($"QNB hata. Code={errorCode} Desc={errorDesc}");

            var list = new List<LegacyBankRow>();
            var accountInfos = ret.Descendants().Where(x => x.Name.LocalName == "accountInfos");

            foreach (var acc in accountInfos)
            {
                var accNo = GetVal(acc, "accountNo");
                var customerNo = GetVal(acc, "customerNo");
                var branchCode = GetVal(acc, "branchCode");
                var accCurrency = GetVal(acc, "accountCurrencyCode");
                var accountIban = GetVal(acc, "iban");

                foreach (var trx in acc.Descendants().Where(x => x.Name.LocalName == "transactions"))
                {
                    var trxId = GetVal(trx, "transactionId");
                    var order = GetVal(trx, "statementTransactionOrder");
                    var processId = FirstNonEmpty(trxId, order);

                    var trxDateStr = GetVal(trx, "transactionDate");
                    var dt = ParseDate(trxDateStr);

                    var refNo =
                        FirstNonEmpty(
                            GetVal(trx, "eftInquiryNumber"),
                            GetVal(trx, "ddReferenceNumber"),
                            GetVal(trx, "ddFirmReferenceNumber"),
                            GetVal(trx, "productOperationRefNo"),
                            processId);

                    list.Add(new LegacyBankRow
                    {
                        BNKCODE = "QNB",
                        HESAPNO = string.IsNullOrWhiteSpace(accNo) ? request.AccountNumber : accNo,
                        URF = customerNo,
                        SUBECODE = branchCode,
                        CURRENCYCODE = FirstNonEmpty(GetVal(trx, "currencyCode"), accCurrency),

                        PROCESSID = processId,
                        PROCESSTIMESTR = trxDateStr,
                        PROCESSTIMESTR2 = trxDateStr,
                        PROCESSTIME = dt,
                        PROCESSTIME2 = dt,

                        PROCESSAMAOUNT = GetVal(trx, "transactionAmount"),
                        PROCESSBALANCE = GetVal(trx, "transactionBalance"),
                        PROCESSDESC = GetVal(trx, "transactionDescription"),
                        PROCESSDEBORCRED = MapDebitCredit(GetVal(trx, "debitOrCreditCode")),

                        PROCESSREFNO = refNo,
                        PROCESSIBAN = GetVal(trx, "opponentIBAN"),
                        PROCESSVKN = GetVal(trx, "opponentTAXNoPIDNo"),
                        FRMIBAN = accountIban,
                        Durum = 0
                    });
                }
            }

            return LegacyBankRowMapper.ToResult(list, xml);
        }

        private static string GetVal(XElement parent, string localName)
            => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value?.Trim() ?? "";

        private static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;

            return DateTime.TryParse(s, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out dt)
                ? dt
                : null;
        }

        private static string MapDebitCredit(string code)
        {
            var value = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Contains('C') || value == "+")
                return "A";
            if (value.Contains('D') || value == "-")
                return "B";

            return string.IsNullOrWhiteSpace(code) ? "A" : code;
        }

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

        private static string SafePreview(string? value)
        {
            value = (value ?? string.Empty).Trim();
            return value.Length <= 600 ? value : value[..600];
        }
    }
}
