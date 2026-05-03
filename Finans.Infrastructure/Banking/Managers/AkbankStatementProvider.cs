using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;
using Finans.Infrastructure.Banking.Managers.BankProviders.Infrastructure;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class AkbankStatementProvider : IBankProvider
    {
        private const string DefaultEndpoint = "https://firmahizmetleri.akbank.com/Extre_InterfaceService/Service.asmx";
        private readonly IHttpClientFactory _httpClientFactory;

        public AkbankStatementProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public int BankId => BankIds.Akbank;
        public string BankCode => "AKB";
        public string ProviderCode => "AkbankStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "AKB" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            var list = new List<LegacyBankRow>();
            var rawResponses = new StringBuilder();
            var endpoint = ResolveEndpoint(request.Link);
            var client = _httpClientFactory.CreateClient(nameof(AkbankStatementProvider));
            client.Timeout = TimeSpan.FromSeconds(55);

            var urf = request.GetExtra("urf") ?? request.GetExtra("customerNo") ?? string.Empty;

            for (var day = request.StartDate.Date; day <= request.EndDate.Date; day = day.AddDays(1))
            {
                var basTarih = day.ToString("yyyyMMdd") + "000000000000";
                var bitTarih = day.ToString("yyyyMMdd") + "230000000000";

                var rawResponse = await PostSoapAsync(
                    client,
                    endpoint,
                    request.Username,
                    request.Password ?? string.Empty,
                    urf,
                    request.AccountNumber,
                    basTarih,
                    bitTarih,
                    ct);

                rawResponses.AppendLine(rawResponse);

                var doc = LoadAkbankResponse(rawResponse);
                var detayList = doc.GetElementsByTagName("Detay");
                var hesapList = doc.GetElementsByTagName("Hesap");

                foreach (XmlNode node in detayList)
                {
                    var pTim = GetChild(node, 0);
                    var pTim2 = GetChild(node, 1);

                    var row = new LegacyBankRow
                    {
                        BNKCODE = BankCode,
                        HESAPNO = request.AccountNumber,
                        PROCESSTIMESTR = pTim,
                        PROCESSTIMESTR2 = pTim2,
                        PROCESSTIME = ParseYmd(pTim),
                        PROCESSTIME2 = ParseYmd(pTim2),
                        PROCESSREFNO = GetChild(node, 12),
                        PROCESSIBAN = GetChild(node, 24),
                        PROCESSVKN = GetChild(node, 21),
                        PROCESSAMAOUNT = GetChild(node, 4),
                        PROCESSBALANCE = GetChild(node, 5),
                        PROCESSDESC = GetChild(node, 6),
                        PROCESSDESC2 = GetChild(node, 25),
                        PROCESSDESC3 = GetChild(node, 26),
                        PROCESSDEBORCRED = GetChild(node, 8) == "+" ? "A" : "B",
                        PROCESSTYPECODE = GetChild(node, 10),
                        PROCESSTYPECODEMT940 = GetChild(node, 11),
                        Durum = 0
                    };

                    foreach (XmlNode h in hesapList)
                    {
                        row.URF = GetChild(h, 3);
                        row.SUBECODE = GetChild(h, 4);
                        row.FRMIBAN = GetChild(h, 7);
                        row.PROCESSID = GetChild(h, 0);
                    }

                    row.PROCESSID ??= row.PROCESSREFNO;
                    list.Add(row);
                }
            }

            return LegacyBankRowMapper.ToResult(list, rawResponses.ToString());
        }

        private static Uri ResolveEndpoint(string? endpoint)
        {
            var value = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
            var wsdlIndex = value.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase);
            if (wsdlIndex >= 0)
                value = value[..wsdlIndex];

            return new Uri(value);
        }

        private static async Task<string> PostSoapAsync(
            HttpClient client,
            Uri endpoint,
            string username,
            string password,
            string urf,
            string hesapNo,
            string baslangicTarihi,
            string bitisTarihi,
            CancellationToken ct)
        {
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetExtreWithParams xmlns=""http://tempuri.org/"">
      <urf>{XmlEscape(urf)}</urf>
      <hesapNo>{XmlEscape(hesapNo)}</hesapNo>
      <dovizKodu></dovizKodu>
      <subeKodu></subeKodu>
      <baslangicTarihi>{XmlEscape(baslangicTarihi)}</baslangicTarihi>
      <bitisTarihi>{XmlEscape(bitisTarihi)}</bitisTarihi>
    </GetExtreWithParams>
  </soap:Body>
</soap:Envelope>";

            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            message.Headers.Add("SOAPAction", "\"http://tempuri.org/GetExtreWithParams\"");
            message.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");

            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var fault = TryReadSoapFault(responseText);
                throw new InvalidOperationException($"Akbank HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {fault ?? "Authorization or service error"}");
            }

            return responseText;
        }

        private static XmlDocument LoadAkbankResponse(string rawResponse)
        {
            var doc = new XmlDocument();
            doc.LoadXml(rawResponse);

            var resultNode = doc.GetElementsByTagName("GetExtreWithParamsResult").Cast<XmlNode>().FirstOrDefault();
            if (resultNode == null)
                return doc;

            var inlineXml = resultNode.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(inlineXml) && inlineXml.StartsWith("<", StringComparison.Ordinal))
            {
                var innerDoc = new XmlDocument();
                innerDoc.LoadXml(inlineXml);
                return innerDoc;
            }

            var firstElement = resultNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.NodeType == XmlNodeType.Element);
            if (firstElement != null)
            {
                var innerDoc = new XmlDocument();
                innerDoc.LoadXml(firstElement.OuterXml);
                return innerDoc;
            }

            return doc;
        }

        private static string? TryReadSoapFault(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return null;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(rawResponse);
                return doc.GetElementsByTagName("faultstring").Cast<XmlNode>().FirstOrDefault()?.InnerText;
            }
            catch
            {
                return null;
            }
        }

        private static string XmlEscape(string? value)
            => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        private static string GetChild(XmlNode node, int index)
        {
            var child = node.ChildNodes.Count > index ? node.ChildNodes[index] : null;
            return child?.InnerText ?? "";
        }

        private static DateTime? ParseYmd(string ymd)
        {
            if (string.IsNullOrWhiteSpace(ymd) || ymd.Length < 8) return null;
            if (!int.TryParse(ymd.Substring(0, 4), out var y)) return null;
            if (!int.TryParse(ymd.Substring(4, 2), out var m)) return null;
            if (!int.TryParse(ymd.Substring(6, 2), out var d)) return null;
            return new DateTime(y, m, d);
        }
    }
}
