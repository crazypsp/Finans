using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking.Base;
using Finans.Infrastructure.Banking.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Finans.Infrastructure.Banking.Managers.BankProviders.Infrastructure;
using VakifSrv;

namespace Finans.Infrastructure.Banking.Managers.BankProviders
{
    public sealed class VakifBankStatementProvider : IBankProvider
    {
        public int BankId => BankIds.Vakifbank;
        public string BankCode => "VAK";
        public string ProviderCode => "VakifBankStatementProvider";
        public IReadOnlyCollection<string> ProviderAliases { get; } = new[] { "VAK", "VKF" };

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            var list = new List<LegacyBankRow>();

            // Vakıf için MusteriNo gerekiyor (sende nerede tutuyorsan oradan ver)
            // Örn: request.Username = KurumKullanici, request.Password = Sifre, request.CustomerNo = MusteriNo gibi.
            var musteriNo = request.GetExtraRequired("MusteriNo"); // eğer böyle bir sistemin varsa
                                                                   // yoksa üst satırı kaldırıp: var musteriNo = request.Username; gibi ayarla.

            var client = string.IsNullOrWhiteSpace(request.Link)
                ? new VakifSrv.SOnlineEkstreServisClient(VakifSrv.SOnlineEkstreServisClient.EndpointConfiguration.MetadataExchangeHttpsBinding_ISOnlineEkstreServis)
                : new VakifSrv.SOnlineEkstreServisClient(VakifSrv.SOnlineEkstreServisClient.EndpointConfiguration.MetadataExchangeHttpsBinding_ISOnlineEkstreServis, request.Link);

            try
            {
                var sorgu = new VakifSrv.DtoEkstreSorgu
                {
                    HesapNo = request.AccountNumber,
                    MusteriNo = musteriNo,
                    KurumKullanici = request.Username,
                    Sifre = request.Password ?? "",
                    SorguBaslangicTarihi = request.StartDate.ToString("yyyy-MM-dd 00:00"),
                    SorguBitisTarihi = request.EndDate.ToString("yyyy-MM-dd 23:59"),
                };

                var resp = await client.GetirHareketAsync(sorgu).ConfigureAwait(false);
                if (resp == null) return LegacyBankRowMapper.ToResult(list);

                // Başarılı mı kontrolü (sende beklediğin kod neyse)
                if (!string.Equals(resp.IslemKodu, "VBB0001", StringComparison.OrdinalIgnoreCase))
                    return LegacyBankRowMapper.ToResult(list);

                foreach (var hesap in resp.Hesaplar ?? Array.Empty<VakifSrv.DtoEkstreHesap>())
                {
                    foreach (var h in hesap.Hareketler ?? Array.Empty<VakifSrv.DtoEkstreHareket>())
                    {
                        list.Add(new LegacyBankRow
                        {
                            BNKCODE = BankCode,
                            HESAPNO = hesap.HesapNo,
                            FRMIBAN = hesap.HesapNoIban,
                            FRMVKN = hesap.VergiKimlikNumarasi,

                            PROCESSID = h.Id.ToString(),
                            PROCESSTIMESTR = h.IslemTarihi,
                            PROCESSTIMESTR2 = h.IslemTarihi,
                            PROCESSREFNO = h.IslemNo,
                            PROCESSDESC = h.Aciklama,
                            PROCESSDESC2 = h.IslemAdi,
                            PROCESSDEBORCRED = h.BorcAlacak,
                            PROCESSTYPECODEMT940 = h.IslemKodu,
                            PROCESSAMAOUNT = h.Tutar.ToString(),
                            PROCESSBALANCE = h.IslemSonrasıBakiye.ToString(), // Türkçe “ı” olan property
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
