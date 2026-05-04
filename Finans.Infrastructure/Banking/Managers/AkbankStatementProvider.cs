using AkbankSrv;
using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;
using Finans.Infrastructure.Banking.Managers.BankProviders.Infrastructure;
using System.Net;
using System.ServiceModel;
using System.Xml;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class AkbankStatementProvider : IBankProvider
    {
        public int BankId => BankIds.Akbank;
        public string BankCode => "AKB";
        public string ProviderCode => "AkbankStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "AKB" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            var list = new List<LegacyBankRow>();

            var client = string.IsNullOrWhiteSpace(request.Link)
                ? new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap)
                : new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap, NormalizeEndpoint(request.Link));

            try
            {
                var urf = request.GetExtra("urf") ?? request.GetExtra("customerNo") ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(request.Username))
                {
                    if (client.Endpoint.Binding is BasicHttpBinding binding)
                        binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

                    client.ClientCredentials.UserName.UserName = request.Username;
                    client.ClientCredentials.UserName.Password = request.Password ?? string.Empty;
                }

                for (var day = request.StartDate.Date; day <= request.EndDate.Date; day = day.AddDays(1))
                {
                    ct.ThrowIfCancellationRequested();

                    var basTarih = day.ToString("yyyyMMdd") + "000000000000";
                    var bitTarih = day.ToString("yyyyMMdd") + "230000000000";

                    var xml = await client.GetExtreWithParamsAsync(
                        urf: urf,
                        hesapNo: request.AccountNumber,
                        dovizKodu: string.Empty,
                        subeKodu: string.Empty,
                        baslangicTarihi: basTarih,
                        bitisTarihi: bitTarih
                    ).ConfigureAwait(false);

                    if (xml == null)
                        continue;

                    var doc = new XmlDocument();
                    doc.LoadXml(xml.OuterXml);

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

                return LegacyBankRowMapper.ToResult(list);
            }
            finally
            {
                client.SafeClose();
            }
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            var value = endpoint.Trim();
            var wsdlIndex = value.IndexOf("?wsdl", StringComparison.OrdinalIgnoreCase);
            return wsdlIndex >= 0 ? value[..wsdlIndex] : value;
        }

        private static string GetChild(XmlNode node, int index)
        {
            var child = node.ChildNodes.Count > index ? node.ChildNodes[index] : null;
            return child?.InnerText ?? string.Empty;
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
