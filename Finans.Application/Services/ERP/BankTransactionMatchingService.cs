using Finans.Application.Abstractions.ERP;
using Finans.Data.Context;
using Finans.Entities.Banking;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.ERP
{
    /// <summary>
    /// Bankadan gelen hesap hareketlerini Logo Tiger hesap plani ile eslestirir.
    /// Oncelik sirasi:
    ///   1) Kural Motoru (BankTransactionRules)
    ///   2) ERP Kod Esleme (ErpCodeMappings)
    ///   3) Aciklama bazli GL hesap arama (muhasebe hesap plani adinda gecen kelimeler)
    ///   4) Aciklama bazli Cari hesap arama (cari hesap adinda gecen kelimeler)
    /// </summary>
    public sealed class BankTransactionMatchingService : IBankTransactionMatchingService
    {
        private readonly FinansDbContext _db;
        private readonly IBankTransactionRuleResolver _ruleResolver;
        private readonly IErpCodeResolver _codeResolver;

        public BankTransactionMatchingService(
            FinansDbContext db,
            IBankTransactionRuleResolver ruleResolver,
            IErpCodeResolver codeResolver)
        {
            _db = db;
            _ruleResolver = ruleResolver;
            _codeResolver = codeResolver;
        }

        public async Task<MatchingSummary> MatchUnmatchedTransactionsAsync(int companyId, CancellationToken ct = default)
        {
            var summary = new MatchingSummary();

            var unmatched = await _db.BankTransactions
                .Where(x => x.CompanyId == companyId
                         && !x.IsMatched
                         && !x.IsTransferred
                         && !x.IsDeleted)
                .OrderByDescending(x => x.TransactionDate)
                .Take(500)
                .ToListAsync(ct);

            // Logo Tiger'dan cekilmis hesap planlarini bellege al
            var glAccounts = await _db.ErpGlAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .ToListAsync(ct);

            var currentAccounts = await _db.ErpCurrentAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .ToListAsync(ct);

            var bankAccounts = await _db.ErpBankAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .ToListAsync(ct);

            // Banka hareketi aciklamalarinda sik gecen anahtar kelimeler -> GL eslesmesi
            // Ornek: "EFT" -> 102.xx (Bankalar hesabi), "HAVALE" -> 102.xx
            //         "FAİZ" -> 642.xx (Faiz Gelirleri), "KOMİSYON" -> 653.xx
            var descriptionKeywords = BuildDescriptionKeywordMap(glAccounts);

            foreach (var tx in unmatched)
            {
                ct.ThrowIfCancellationRequested();
                summary.TotalProcessed++;

                // 1. Kural Motoru
                var ruleMatch = await _ruleResolver.ResolveAsync(tx, ct);
                if (ruleMatch.IsMatched)
                {
                    ApplyMatch(tx, ruleMatch.CurrentCode, ruleMatch.GlCode,
                              ruleMatch.BankAccountCode, "Rule",
                              currentAccounts, glAccounts);
                    summary.MatchedCount++;
                    continue;
                }

                // 2. ERP Kod Esleme
                var (currentCode, glCode, bankAccountCode) = await _codeResolver.ResolveAsync(tx, ct);
                if (!string.IsNullOrWhiteSpace(currentCode) || !string.IsNullOrWhiteSpace(glCode))
                {
                    ApplyMatch(tx, currentCode, glCode, bankAccountCode, "Mapping",
                              currentAccounts, glAccounts);
                    summary.MatchedCount++;
                    continue;
                }

                // 3. Aciklama bazli GL hesap eslesmesi
                // Banka hareketi aciklamasindaki kelimeleri GL hesap adlariyla karsilastir
                if (!string.IsNullOrWhiteSpace(tx.Description))
                {
                    var glMatch = MatchDescriptionToGlAccount(tx.Description, glAccounts, descriptionKeywords);
                    if (glMatch != null)
                    {
                        tx.IsMatched = true;
                        tx.MatchedAtUtc = DateTime.UtcNow;
                        tx.MatchedCurrentCode = glMatch.Value.GlCode;
                        tx.MatchedCurrentName = glMatch.Value.GlName + " (GL)";
                        tx.UpdatedAtUtc = DateTime.UtcNow;
                        summary.MatchedCount++;
                        continue;
                    }

                    // 4. Aciklama bazli Cari hesap eslesmesi
                    var matchedCurrent = MatchDescriptionToCurrentAccount(tx.Description, currentAccounts);
                    if (matchedCurrent != null)
                    {
                        tx.IsMatched = true;
                        tx.MatchedAtUtc = DateTime.UtcNow;
                        tx.MatchedCurrentCode = matchedCurrent.CurrentCode;
                        tx.MatchedCurrentName = matchedCurrent.CurrentName;
                        tx.UpdatedAtUtc = DateTime.UtcNow;
                        summary.MatchedCount++;
                        continue;
                    }
                }

                summary.UnmatchedCount++;
            }

            if (unmatched.Count > 0)
                await _db.SaveChangesAsync(ct);

            return summary;
        }

        public async Task<MatchingResult> MatchSingleTransactionAsync(int companyId, int bankTransactionId, CancellationToken ct = default)
        {
            var tx = await _db.BankTransactions
                .FirstOrDefaultAsync(x => x.CompanyId == companyId
                                       && x.Id == bankTransactionId
                                       && !x.IsDeleted, ct);

            if (tx == null)
                return new MatchingResult { IsMatched = false };

            var glAccounts = await _db.ErpGlAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .ToListAsync(ct);

            var currentAccounts = await _db.ErpCurrentAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .ToListAsync(ct);

            // 1. Kural motoru
            var ruleMatch = await _ruleResolver.ResolveAsync(tx, ct);
            if (ruleMatch.IsMatched)
            {
                var gl = glAccounts.FirstOrDefault(g => g.GlCode == ruleMatch.GlCode);
                var cur = currentAccounts.FirstOrDefault(c => c.CurrentCode == ruleMatch.CurrentCode);
                return new MatchingResult
                {
                    IsMatched = true,
                    CurrentCode = ruleMatch.CurrentCode,
                    CurrentName = cur?.CurrentName,
                    GlCode = ruleMatch.GlCode,
                    GlName = gl?.GlName,
                    BankAccountCode = ruleMatch.BankAccountCode,
                    MatchSource = "Rule"
                };
            }

            // 2. ERP kod esleme
            var (currentCode, glCode, bankAccountCode) = await _codeResolver.ResolveAsync(tx, ct);
            if (!string.IsNullOrWhiteSpace(currentCode) || !string.IsNullOrWhiteSpace(glCode))
            {
                var gl = glAccounts.FirstOrDefault(g => g.GlCode == glCode);
                var cur = currentAccounts.FirstOrDefault(c => c.CurrentCode == currentCode);
                return new MatchingResult
                {
                    IsMatched = true,
                    CurrentCode = currentCode,
                    CurrentName = cur?.CurrentName,
                    GlCode = glCode,
                    GlName = gl?.GlName,
                    BankAccountCode = bankAccountCode,
                    MatchSource = "Mapping"
                };
            }

            // 3. Aciklama bazli GL
            if (!string.IsNullOrWhiteSpace(tx.Description))
            {
                var keywords = BuildDescriptionKeywordMap(glAccounts);
                var glMatch = MatchDescriptionToGlAccount(tx.Description, glAccounts, keywords);
                if (glMatch != null)
                {
                    return new MatchingResult
                    {
                        IsMatched = true,
                        GlCode = glMatch.Value.GlCode,
                        GlName = glMatch.Value.GlName,
                        MatchSource = "DescriptionGL"
                    };
                }

                // 4. Aciklama bazli Cari
                var curMatch = MatchDescriptionToCurrentAccount(tx.Description, currentAccounts);
                if (curMatch != null)
                {
                    return new MatchingResult
                    {
                        IsMatched = true,
                        CurrentCode = curMatch.CurrentCode,
                        CurrentName = curMatch.CurrentName,
                        MatchSource = "DescriptionCurrent"
                    };
                }
            }

            return new MatchingResult { IsMatched = false };
        }

        /// <summary>
        /// Banka islem aciklamalarinda sik gecen anahtar kelimeleri GL hesaplariyla esle.
        /// Ornek: EFT/HAVALE -> Bankalar, FAIZ -> Faiz Gelirleri, KOMISYON -> Komisyon Giderleri
        /// </summary>
        private static Dictionary<string, (string GlCode, string GlName)> BuildDescriptionKeywordMap(
            List<Finans.Entities.ERP.ErpGlAccount> glAccounts)
        {
            var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

            foreach (var gl in glAccounts)
            {
                if (string.IsNullOrWhiteSpace(gl.GlName)) continue;

                // GL hesap adindan anahtar kelimeler cikar
                // "BANKALAR HESABI" -> "BANKALAR"
                // "FAİZ GELİRLERİ" -> "FAİZ"
                var words = gl.GlName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var word in words)
                {
                    // 3 karakterden kisa kelimeleri atla (VE, İLE, VS gibi)
                    if (word.Length < 4) continue;

                    // Genel kelimeler atla
                    if (IsGenericWord(word)) continue;

                    if (!map.ContainsKey(word))
                    {
                        map[word] = (gl.GlCode, gl.GlName);
                    }
                }
            }

            return map;
        }

        private static (string GlCode, string GlName)? MatchDescriptionToGlAccount(
            string description,
            List<Finans.Entities.ERP.ErpGlAccount> glAccounts,
            Dictionary<string, (string GlCode, string GlName)> keywordMap)
        {
            var descUpper = description.ToUpperInvariant();

            // Once tam GL hesap adi eslemesi dene
            foreach (var gl in glAccounts)
            {
                if (string.IsNullOrWhiteSpace(gl.GlName)) continue;
                if (gl.GlName.Length < 4) continue;

                if (descUpper.Contains(gl.GlName.ToUpperInvariant()))
                {
                    return (gl.GlCode, gl.GlName);
                }
            }

            // Sonra anahtar kelime eslemesi
            var descWords = description
                .ToUpperInvariant()
                .Split(new[] { ' ', '/', '-', '.', ',', ';', ':', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in descWords)
            {
                if (word.Length < 4) continue;
                if (keywordMap.TryGetValue(word, out var match))
                {
                    return match;
                }
            }

            return null;
        }

        private static Finans.Entities.ERP.ErpCurrentAccount? MatchDescriptionToCurrentAccount(
            string description,
            List<Finans.Entities.ERP.ErpCurrentAccount> currentAccounts)
        {
            var descUpper = description.ToUpperInvariant();

            // Cari hesap adi aciklamada geciyor mu?
            // Uzun isimlerden kisa isimlere dogru ara (daha spesifik eslesme oncelikli)
            return currentAccounts
                .Where(c => !string.IsNullOrWhiteSpace(c.CurrentName) && c.CurrentName.Length >= 4)
                .OrderByDescending(c => c.CurrentName!.Length)
                .FirstOrDefault(c => descUpper.Contains(c.CurrentName!.ToUpperInvariant()));
        }

        private static bool IsGenericWord(string word)
        {
            var generics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "HESABI", "HESAP", "HESAPLARI", "TOPLAM", "GENEL",
                "DİĞER", "DIGER", "ÇEŞİTLİ", "CESITLI", "TİCARİ",
                "TICARI", "ALACAK", "BORÇ", "BORC", "GELİR", "GELIR",
                "GİDER", "GIDER", "KISA", "UZUN", "VADELİ", "VADELI"
            };
            return generics.Contains(word);
        }

        private static void ApplyMatch(
            BankTransaction tx,
            string? currentCode, string? glCode, string? bankAccountCode,
            string matchSource,
            List<Finans.Entities.ERP.ErpCurrentAccount> currentAccounts,
            List<Finans.Entities.ERP.ErpGlAccount> glAccounts)
        {
            tx.IsMatched = true;
            tx.MatchedAtUtc = DateTime.UtcNow;
            tx.UpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(currentCode))
            {
                tx.MatchedCurrentCode = currentCode;
                tx.MatchedCurrentName = currentAccounts
                    .FirstOrDefault(c => c.CurrentCode == currentCode)?.CurrentName;
            }
            else if (!string.IsNullOrWhiteSpace(glCode))
            {
                // Cari yoksa GL kodunu eslesme bilgisi olarak yaz
                tx.MatchedCurrentCode = glCode;
                tx.MatchedCurrentName = glAccounts
                    .FirstOrDefault(g => g.GlCode == glCode)?.GlName + " (GL)";
            }
        }
    }
}
