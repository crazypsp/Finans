# Finans Yönetim Sistemi — A'dan Z'ye Test Rehberi
## Uzman QA & Full Stack .NET Developer Perspektifi

---

# BÖLÜM A: ORTAM HAZIRLIĞI

## A.1 — Solution Build Testi

1. Visual Studio 2022'de `Finans.sln` dosyasını açın
2. Solution Explorer → Solution'a sağ tık → **Restore NuGet Packages**
3. NuGet paketlerinin tamamının indiğini Output penceresinden doğrulayın
4. **Build → Rebuild Solution** (Ctrl+Shift+B)
5. Error List penceresinde:
   - **0 Error** olmalı
   - Warning'ler olabilir (nullable uyarıları gibi — bunlar normal)

**Eğer hata alırsanız:**
- Error'a çift tıklayın → ilgili dosyaya gider
- Hatayı not alın ve bana gönderin

## A.2 — Migration Testi

1. **Tools → NuGet Package Manager → Package Manager Console** açın
2. Console'da **Default project** olarak `Finans.Data` seçin
3. Eğer daha önce migration yaptıysanız, `Finans.Data/Migrations` klasörünü silin
4. Çalıştırın:

```
Add-Migration InitialCreate -StartupProject Finans.WebMvc
```

5. **Beklenen sonuç:** `Migrations` klasöründe `XXXXXXXX_InitialCreate.cs` dosyası oluşur
6. Çalıştırın:

```
Update-Database -StartupProject Finans.WebMvc
```

7. **Beklenen sonuç:** Console'da `Done.` yazar

## A.3 — Veritabanı Doğrulama

1. SQL Server Management Studio (SSMS) açın
2. `DESKTOP-54QF28R\ZRV2014EXP` sunucusuna bağlanın
3. **Databases → FinansDB** genişletin
4. **Tables** altında şu tabloları doğrulayın:

| Tablo | İçerik |
|-------|--------|
| Users | 3 kayıt (admin, system, muhasebe) |
| Roles | 6 kayıt (ADMIN, DEALER, SUB_DEALER, ACCOUNTANT, COMPANY_ADMIN, COMPANY_USER) |
| UserRoles | 3 kayıt |
| Companies | 1 kayıt (Demo Firma A.Ş.) |
| CompanyUsers | 3 kayıt |
| MenuItems | 14 kayıt |
| RoleMenuPermissions | 40 kayıt |
| ErpSystems | 1 kayıt (Logo Tiger) |

5. SSMS'de yeni sorgu açın ve çalıştırın:

```sql
SELECT u.UserName, u.Email, r.Code AS RoleCode, r.Name AS RoleName
FROM Users u
JOIN UserRoles ur ON ur.UserId = u.Id
JOIN Roles r ON r.Id = ur.RoleId
WHERE u.IsDeleted = 0
ORDER BY u.Id
```

**Beklenen sonuç:**

| UserName | Email | RoleCode | RoleName |
|----------|-------|----------|----------|
| admin | admin@finans.local | ADMIN | Sistem Yöneticisi |
| system | system@finans.local | COMPANY_ADMIN | Firma Yöneticisi |
| muhasebe | muhasebe@finans.local | ACCOUNTANT | Muhasebeci |

---

# BÖLÜM B: WEB PROJESİ TESTLERİ (Finans.WebMvc)

## B.1 — Başlatma

1. Solution Explorer → `Finans.WebMvc` sağ tık → **Set as Startup Project**
2. **F5** basın (Debug mode)
3. Tarayıcı açılır
4. **Beklenen:** Login sayfası gelir (gradient arka plan, kurumsal kart tasarımı)

## B.2 — Kimlik Doğrulama (Authentication)

### Test B.2.1: Başarılı Admin Girişi
1. Kullanıcı: `admin` / Şifre: `Admin123!`
2. **Giriş Yap** butonuna basın
3. **Beklenen:** Dashboard sayfasına yönlendirilir, sol menüde TÜM menüler görünür:
   - Dashboard, Hesap Hareketleri, Banka Tanımları, Aktarım Kuyrukları,
     Başarısız Aktarımlar, ERP Kod Eşleme, Kural Motoru, ERP Senkronize,
     Raporlar, Sistem Logları, Desktop Connector, Kullanıcı Yönetimi

### Test B.2.2: Başarılı Muhasebe Girişi
1. Çıkış yapın (sağ üst dropdown → Çıkış)
2. Kullanıcı: `muhasebe` / Şifre: `Muhasebe123!`
3. **Beklenen:** Dashboard açılır, sol menüde şunlar GÖRÜNMEZ:
   - ~~Kullanıcı Yönetimi~~
   - ~~Desktop Connector~~
4. Şunlar görünür: Dashboard, Hesap Hareketleri, Banka Tanımları, Aktarım Kuyrukları, Başarısız Aktarımlar, ERP Kod Eşleme, Kural Motoru, Raporlar, Sistem Logları

### Test B.2.3: Başarılı System Girişi
1. Çıkış yapın
2. Kullanıcı: `system` / Şifre: `System123!`
3. **Beklenen:** Dashboard açılır, sol menüde şunlar GÖRÜNMEZ:
   - ~~ERP Kod Eşleme~~
   - ~~Kural Motoru~~
   - ~~ERP Senkronize~~
   - ~~Sistem Logları~~
   - ~~Kullanıcı Yönetimi~~

### Test B.2.4: Hatalı Giriş
1. Kullanıcı: `admin` / Şifre: `yanlis_sifre`
2. **Beklenen:** Login sayfasında kırmızı hata mesajı: "Şifre hatalı."

### Test B.2.5: Olmayan Kullanıcı
1. Kullanıcı: `olmayan_kullanici` / Şifre: `birsey`
2. **Beklenen:** "Kullanıcı bulunamadı." mesajı

### Test B.2.6: Yetkisiz Erişim
1. `muhasebe` ile giriş yapın
2. Tarayıcıda adres çubuğuna `http://localhost:XXXX/Admin/Users` yazın
3. **Beklenen:** "Erişim reddedildi." mesajı veya login sayfasına yönlendirme

## B.3 — Dashboard

1. `admin` ile giriş yapın
2. Dashboard sayfasını kontrol edin:

### Test B.3.1: Stat Kartları
- 4 kart görünmeli: Toplam İşlem, Aktarılmamış, Aktarılmış, Toplam Tutar
- İlk açılışta hepsi 0 olabilir (henüz veri yok — normal)

### Test B.3.2: Grafikler
- Bar chart (Aktarım Durumu) render olmalı
- Doughnut chart (İşlem Dağılımı) render olmalı
- Canvas elemanları tarayıcıda görünmeli (sağ tık → Incele → canvas elemanı)

### Test B.3.3: Sistem Sağlığı
- Sistem Sağlığı tablosu görünmeli
- Desktop Connector: "Pasif" (henüz çalıştırılmadı)
- Banka Credential: "Tanımsız" (henüz eklenmedi)

## B.4 — Banka Tanımları

### Test B.4.1: Banka Ekleme
1. Sol menü → **Banka Tanımları**
2. Formu doldurun:
   - Banka Adı: `Test Bankası`
   - Provider Kodu: `DUMMY`
   - External ID: `99`
3. **Ekle** butonuna basın
4. **Beklenen:** Sayfada "Banka eklendi." yeşil mesajı + tabloda yeni banka görünür
5. **SSMS'de doğrulayın:**
```sql
SELECT * FROM Banks WHERE BankName = 'Test Bankası'
```

### Test B.4.2: Hesap Ekleme
1. **Hesaplar** butonuna tıklayın
2. Formu doldurun:
   - Banka ID: `1` (az önce eklediğiniz bankanın ID'si — SSMS'den kontrol edin)
   - Hesap No: `0012345678`
   - IBAN: `TR330006100519786457841326`
   - Döviz: `TRY`
3. **Ekle** butonuna basın
4. **Beklenen:** "Hesap eklendi." mesajı + tabloda yeni hesap görünür
5. **SSMS doğrulama:**
```sql
SELECT * FROM BankAccounts WHERE AccountNumber = '0012345678'
```

### Test B.4.3: Credential Ekleme
1. **Credential** butonuna tıklayın
2. Formu doldurun:
   - Banka ID: `1`
   - Kullanıcı Adı: `testuser`
   - Şifre: `testpass123`
3. **Ekle** butonuna basın
4. **Beklenen:** "Credential eklendi." mesajı + tabloda yeni credential görünür
5. **SSMS doğrulama:**
```sql
SELECT Id, BankId, Username, IsActive FROM BankCredentials WHERE Username = 'testuser'
```

## B.5 — Hesap Hareketleri (Transfer Sayfası)

### Test B.5.1: Sayfa Açılışı
1. Sol menü → **Hesap Hareketleri**
2. **Beklenen:** Filtre formu görünür (Banka, Başlangıç, Bitiş, Hesap No, Açıklama, Aktarılmayanlar checkbox)
3. Banka dropdown'unda eklediğiniz bankalar listelenmeli

### Test B.5.2: Boş Liste
1. Filtre ile **Listele** butonuna basın
2. **Beklenen:** "Filtrelere uygun kayıt bulunamadı." mesajı (henüz import yapılmadı)

### Test B.5.3: Veri Varsa — Tablo Görünümü
(Bu test Bölüm C — WorkerService testinden sonra yapılabilir, veri oluştuktan sonra)
1. Filtre ayarlarını yapın → **Listele**
2. **Beklenen:** Tablo sütunları:
   - Checkbox | ID | Banka | Hesap No | Tarih | Açıklama | Tutar | Döviz | B/A | **Eşleşme** | Durum | İşlem
3. **Eşleşme** sütununda:
   - Yeşil "Eşleşti" badge → otomatik eşleşmiş kayıtlar (tooltip'de hesap kodu görünür)
   - Gri "Eşleşmedi" badge → eşleşmemiş kayıtlar
4. **Durum** sütununda: "Yeni", "Bekliyor" veya "Aktarıldı" badge'leri

### Test B.5.4: Tekli Aktarım (Modal)
1. Aktarılmamış bir kaydın yanındaki **mavi ok butonuna** tıklayın
2. **Beklenen:** Modal açılır:
   - İşlem bilgisi (açıklama + tutar + döviz) gösterilir
   - GL Kodu dropdown'u yüklenir (Logo Tiger'dan çekilmiş hesap planı)
   - Cari Hesap dropdown'u yüklenir
   - Banka Hesap Kodu dropdown'u yüklenir
3. GL Kodu ve Banka Hesap Kodu seçin → **Aktar** butonuna basın
4. **Beklenen:** Sayfaya dönülür, "Aktarım kuyruğa alındı. BatchNo=TRF-..." mesajı

5. **SSMS doğrulama:**
```sql
SELECT * FROM ErpTransferBatches ORDER BY Id DESC
SELECT * FROM ErpTransferItems ORDER BY Id DESC
SELECT TransferBatchNo, IsTransferred FROM BankTransactions WHERE TransferBatchNo IS NOT NULL
```

### Test B.5.5: Toplu Aktarım
1. Birden fazla kaydın checkbox'ını işaretleyin
2. **Tümünü Seç** checkbox'ını deneyin (tüm satırlar seçilmeli)
3. **Seçilenleri Kuyruğa Gönder** butonuna basın
4. Onay dialogu gelir → **Tamam**
5. **Beklenen:** "Batch oluşturuldu. BatchNo=..., Kayıt=X" mesajı

### Test B.5.6: Zaten Aktarılmış Kayıt
1. Daha önce aktarılmış bir kayda bakın
2. **Beklenen:** Checkbox ve Aktar butonu GÖRÜNMEZ, Durum: "Aktarıldı" veya "Bekliyor"

## B.6 — ERP Aktarım Kuyrukları

1. Sol menü → **Aktarım Kuyrukları**
2. **Beklenen:** Oluşturduğunuz batch'ler listelenir
3. Her satırda: Batch No | Durum | Toplam | Başarılı | Başarısız | Başlangıç | Bitiş
4. Durum badge'leri: Yeşil "Başarılı", Kırmızı "Başarısız", Sarı "Bekliyor"

## B.7 — Başarısız Aktarımlar

1. Sol menü → **Başarısız Aktarımlar**
2. Eğer başarısız aktarım varsa: satırlar listelenir
3. **Tekrar Dene** butonu ile retry yapılabilir
4. Checkbox'larla seçip **Seçilenleri Tekrar Dene** butonu çalışmalı

## B.8 — Tanımlar

### Test B.8.1: ERP Kod Eşleme
1. Sol menü → **ERP Kod Eşleme**
2. **Yeni Eşleme** butonuna basın
3. Formu doldurun:
   - Döviz: `TRY`
   - Borç/Alacak: `C` (Alacak)
   - Açıklama Anahtar Kelimesi: `EFT`
   - GL Kod: `102` (bir muhasebe hesap kodu)
   - Öncelik: `1`
   - Aktif: ✓
4. **Kaydet** butonuna basın
5. **Beklenen:** "Mapping kaydedildi." mesajı + listede görünür
6. **Sil** butonu ile silin → onay → "Mapping silindi."

### Test B.8.2: Kural Motoru
1. Sol menü → **Kural Motoru**
2. **Yeni Kural** butonuna basın
3. Formu doldurun:
   - Açıklama İçerir: `HAVALE`
   - Cari Kod: `120.01.001`
   - GL Kod: `102.01`
   - Öncelik: `1`
   - Aktif: ✓
4. **Kaydet** → "Rule kaydedildi."
5. **Sil** → "Rule silindi."

## B.9 — Raporlar

1. Sol menü → **Raporlar**
2. **Beklenen:**
   - 6 stat kartı (Toplam İşlem, Bekleyen, Başarılı, Başarısız, Aktif Connector, Aktif Banka)
   - Banka Import Raporu tablosu
   - ERP Transfer Raporu tablosu
   - Connector Raporu tablosu
3. Filtrelerle arama yapın → **Filtrele** butonu

## B.10 — Sistem Logları

1. Sol menü → **Sistem Logları**
2. **Beklenen:**
   - Sol taraf: Bildirimler (okunmamış bildirimlerin sayısı badge'de)
   - Sağ taraf: Denetim Logları tablosu
3. Bir bildirimde **Okundu** butonuna basın
4. **Beklenen:** Badge sayısı azalır

## B.11 — Connector Sayfası

1. Sol menü → **Desktop Connector**
2. İlk açılışta boş olacak ("Kayıtlı Desktop Connector bulunamadı.")
3. DesktopConnector çalıştıktan sonra: Connector kartı görünür (Makine adı, versiyon, heartbeat, durum)

## B.12 — Kullanıcı Yönetimi (Sadece Admin)

### Test B.12.1: Kullanıcı Listesi
1. Sol menü → **Kullanıcı Yönetimi** (sadece admin görür)
2. **Beklenen:** 3 kullanıcı listelenir (admin, system, muhasebe)
3. Her satırda roller badge olarak görünür (ADMIN, COMPANY_ADMIN, ACCOUNTANT)

### Test B.12.2: Yeni Kullanıcı
1. **Yeni Kullanıcı** butonuna basın
2. Formu doldurun:
   - Kullanıcı Adı: `testuser`
   - E-posta: `test@finans.local`
   - Ad: `Test`
   - Soyad: `Kullanıcı`
   - Şifre: `Test123!`
3. **Kaydet** butonuna basın
4. **Beklenen:** Rol atama sayfasına yönlendirilir

### Test B.12.3: Rol Atama
1. Rol dropdown'undan **Firma Kullanıcısı (COMPANY_USER)** seçin
2. **Ata** butonuna basın
3. **Beklenen:** Kullanıcı listesine dönülür, yeni kullanıcı COMPANY_USER rolüyle görünür

### Test B.12.4: Yeni Kullanıcı ile Giriş
1. Çıkış yapın
2. `testuser` / `Test123!` ile giriş yapın
3. **Beklenen:** Dashboard açılır, COMPANY_USER yetkilerine uygun menüler görünür

---

# BÖLÜM C: WORKERSERVICE TESTLERİ (Finans.WorkerService)

## C.1 — Console Modunda Başlatma

1. Visual Studio'da Web projesini durdurun (Shift+F5)
2. Solution Explorer → `Finans.WorkerService` sağ tık → **Set as Startup Project**
3. **F5** basın
4. Console penceresi açılır
5. **Beklenen log mesajları:**
```
info: Finans.WorkerService.Workers.BankImportWorker[0]
      ...
info: Finans.WorkerService.Workers.ErpTransferWorker[0]
      ...
```

## C.2 — DummyBankProvider ile Import Testi

**Ön koşul:** Bölüm B.4'te `DUMMY` provider kodlu banka + hesap + credential eklenmiş olmalı

1. WorkerService'i F5 ile çalıştırın
2. 5 dakika bekleyin (BankImportIntervalSeconds = 300)
3. **Beklenen:** Console'da import logları görünür
4. **SSMS doğrulama:**
```sql
SELECT TOP 10 * FROM BankTransactions ORDER BY Id DESC
SELECT TOP 10 * FROM BankApiPayloads ORDER BY Id DESC
SELECT TOP 10 * FROM BankIntegrationLogs ORDER BY Id DESC
```
5. `BankTransactions` tablosunda `DUMMY` provider'ın oluşturduğu test kaydı görünmeli

## C.3 — Otomatik Eşleştirme Doğrulama

Import tamamlandıktan sonra eşleştirme otomatik çalışır.

**SSMS doğrulama:**
```sql
SELECT Id, Description, IsMatched, MatchedCurrentCode, MatchedCurrentName
FROM BankTransactions
WHERE CompanyId = 1
ORDER BY Id DESC
```

- Eğer ERP Kod Eşleme veya Kural Motoru'nda tanım varsa ve açıklama eşleşiyorsa → `IsMatched = 1`

## C.4 — ErpTransferWorker Testi

1. WorkerService çalışırken, Web'den bir aktarım batch'i oluşturun (Bölüm B.5.4)
2. 60 saniye bekleyin (ErpTransferIntervalSeconds = 60)
3. **Beklenen:** FakeErpTransferClient kullanıldığı için aktarım başarılı olur
4. **SSMS doğrulama:**
```sql
SELECT Status, ResultMessage, VoucherNo FROM ErpTransferItems ORDER BY Id DESC
SELECT IsTransferred, ErpVoucherNo FROM BankTransactions WHERE TransferBatchNo IS NOT NULL
```
5. `Status = 'Success'` ve `VoucherNo = 'FAKE-VCH-...'` olmalı

## C.5 — Worker Durdurma

1. Console'da **Ctrl+C** basın
2. **Beklenen:** Graceful shutdown, "Application is shutting down" mesajı

---

# BÖLÜM D: DESKTOPCONNECTOR TESTLERİ (Finans.DesktopConnector)

## D.1 — Console Modunda Başlatma (Logo Tiger Olmadan)

1. Solution Explorer → `Finans.DesktopConnector` sağ tık → **Set as Startup Project**
2. **F5** basın
3. **Beklenen log mesajları:**
```
[INF] ERP Transfer Worker başlatıldı. Interval=30s
[ERR] Logo Tiger COM bileşeni bulunamadı.
```
4. Bu NORMAL — Logo Tiger kurulu değilse COM hatası verir
5. Ama heartbeat çalışmaya devam eder

## D.2 — Heartbeat Doğrulama

1. DesktopConnector çalışırken 60 saniye bekleyin
2. **SSMS doğrulama:**
```sql
SELECT * FROM DesktopConnectorClients ORDER BY Id DESC
SELECT TOP 5 * FROM DesktopConnectorHeartbeatLogs ORDER BY Id DESC
```
3. `DesktopConnectorClients` tablosunda makinenizin adıyla bir kayıt olmalı
4. `LastHeartbeatAtUtc` güncel olmalı, `IsActive = 1`

5. Web'i açın → Sol menü → **Desktop Connector**
6. **Beklenen:** Connector kartı görünür, "Aktif" badge, heartbeat tarihi

## D.3 — Logo Tiger ile Test (Logo Tiger Kurulu ise)

### D.3.1: Login Testi
1. `appsettings.json`'da Logo Tiger bilgileri doğru girilmiş olmalı:
```json
"LogoTiger": {
    "UserName": "LOGO1",
    "Password": "LOGO",
    "FirmNr": 325,
    "PeriodNr": 1
}
```
2. DesktopConnector'ı F5 ile çalıştırın
3. **Beklenen:** Console'da "Logo login basarisiz" hatası YOK
4. Heartbeat loglarında "Alive" durumu

### D.3.2: ERP Transfer Testi (End-to-End)
1. Web'de `admin` ile giriş yapın
2. Hesap Hareketleri → bir aktarılmamış kaydın **Aktar** butonuna basın
3. Modal'da GL Kodu ve Banka Hesap Kodu seçin → **Aktar**
4. DesktopConnector console'unu izleyin
5. 30 saniye içinde transfer işlenir
6. **Beklenen:** Console'da "Logo Tiger aktarımı başarılı." mesajı
7. Web'de sayfayı yenileyin → Kaydın durumu: **"Aktarıldı"** (yeşil badge)

8. **SSMS doğrulama:**
```sql
SELECT Status, VoucherNo, ResultMessage FROM ErpTransferItems ORDER BY Id DESC
SELECT IsTransferred, ErpVoucherNo FROM BankTransactions WHERE IsTransferred = 1
SELECT * FROM ErpTransferLogs ORDER BY Id DESC
```

### D.3.3: Başarısız Transfer
1. Modal'da GL Kodu boş bırakarak aktarım yapın (veya geçersiz kod girin)
2. **Beklenen:** Transfer "Failed" olur
3. Web → Başarısız Aktarımlar sayfasında görünür
4. **Retry** butonuyla tekrar deneyin

---

# BÖLÜM E: ÜÇ PROJEYİ BİRLİKTE ÇALIŞTIRMA

## E.1 — Multiple Startup Projects Ayarı

1. Solution Explorer → **Solution'a** sağ tıklayın
2. **Set Startup Projects...** seçin
3. **Multiple startup projects** seçeneğini işaretleyin
4. Ayarlayın:
   - Finans.WebMvc → **Start**
   - Finans.DesktopConnector → **Start**
   - Finans.WorkerService → **Start** (opsiyonel — sadece banka import testi için)
5. **OK** → **F5**

## E.2 — End-to-End Tam Akış Testi

Bu test tüm sistemin birlikte çalıştığını doğrular.

### Adım 1: Banka + Hesap + Credential Ekle (Web)
1. Tarayıcıda `admin` ile giriş yapın
2. Banka Tanımları → Banka: `DUMMY Test`, Provider: `DUMMY` → Ekle
3. Hesaplar → Hesap No: `9999999999`, Banka ID: (az önce eklenen) → Ekle
4. Credential → Kullanıcı: `dummy`, Şifre: `dummy` → Ekle

### Adım 2: Import Bekle (WorkerService)
1. WorkerService console penceresini izleyin
2. 5 dakika bekleyin
3. Console'da import logu görünür

### Adım 3: Hesap Hareketleri Kontrol (Web)
1. Tarayıcıda Hesap Hareketleri sayfasını açın
2. Filtre: Tüm tarihleri geniş tutun → **Listele**
3. **Beklenen:** DummyBankProvider'ın oluşturduğu kayıt görünür
4. **Eşleşme** sütununda badge görünür

### Adım 4: Aktarım Yap (Web)
1. Kaydın **Aktar** butonuna basın
2. Modal → GL Kodu seçin → **Aktar**
3. **Beklenen:** "Aktarım kuyruğa alındı."

### Adım 5: Aktarım İşlenir (DesktopConnector)
1. DesktopConnector console'unu izleyin
2. 30 saniye içinde transfer işlenir
3. Logo Tiger yoksa FakeClient ile başarılı olur

### Adım 6: Sonuç Doğrula (Web + SSMS)
1. Web'de Hesap Hareketleri → Kaydın durumu: **"Aktarıldı"**
2. ERP Aktarım Kuyrukları → Batch durumu: **"Başarılı"**
3. Dashboard → Stat kartları güncellenir

---

# BÖLÜM F: EŞLEŞTİRME TESTLERİ

## F.1 — Kural Motoru Eşleştirme

1. Kural Motoru → Yeni Kural:
   - Açıklama İçerir: `EFT`
   - Cari Kod: `120.01.001`
   - GL Kod: `102.01`
   - Aktif: ✓
2. WorkerService ile yeni import yapılmasını bekleyin (veya SSMS'den manuel kayıt ekleyin):
```sql
INSERT INTO BankTransactions (CompanyId, BankId, AccountNumber, TransactionDate, Description, Amount, Currency, DebitCredit, ExternalUniqueKey, IsMatched, IsTransferred, IsDeleted, CreatedAtUtc)
VALUES (1, 1, '0012345678', GETDATE(), 'EFT GÖNDERİM - YUSUF KARA', 5000.00, 'TRY', 'D', NEWID(), 0, 0, 0, GETUTCDATE())
```
3. Web'de Hesap Hareketleri → Listele
4. **Beklenen:** EFT içeren kayıt "Eşleşti" badge ile görünür (Kural ile)

## F.2 — GL Hesap Adı Eşleştirme

1. Önce ErpGlAccounts tablosunda hesap olmalı (Logo Tiger senkronize veya manuel):
```sql
INSERT INTO ErpGlAccounts (CompanyId, ErpSystemId, GlCode, GlName, IsActive, IsDeleted, CreatedAtUtc, LastSyncedAtUtc)
VALUES (1, 1, '642.01', 'FAİZ GELİRLERİ', 1, 0, GETUTCDATE(), GETUTCDATE())
```
2. Açıklamasında "FAİZ" geçen bir banka hareketi ekleyin:
```sql
INSERT INTO BankTransactions (CompanyId, BankId, AccountNumber, TransactionDate, Description, Amount, Currency, DebitCredit, ExternalUniqueKey, IsMatched, IsTransferred, IsDeleted, CreatedAtUtc)
VALUES (1, 1, '0012345678', GETDATE(), 'MEVDUAT FAİZ GELİRİ', 1250.00, 'TRY', 'C', NEWID(), 0, 0, 0, GETUTCDATE())
```
3. Eşleştirme çalışması için WorkerService import tetikleyin veya ERP Senkronize butonuna basın
4. **Beklenen:** Kayıt "Eşleşti" badge ile görünür, tooltip: "FAİZ GELİRLERİ (GL)"

## F.3 — Cari Hesap Eşleştirme

1. Cari hesap ekleyin:
```sql
INSERT INTO ErpCurrentAccounts (CompanyId, ErpSystemId, CurrentCode, CurrentName, IsActive, IsDeleted, CreatedAtUtc, LastSyncedAtUtc)
VALUES (1, 1, '120.01.005', 'AHMET YILMAZ LTD ŞTİ', 1, 0, GETUTCDATE(), GETUTCDATE())
```
2. Açıklamasında cari adı geçen hareket:
```sql
INSERT INTO BankTransactions (CompanyId, BankId, AccountNumber, TransactionDate, Description, Amount, Currency, DebitCredit, ExternalUniqueKey, IsMatched, IsTransferred, IsDeleted, CreatedAtUtc)
VALUES (1, 1, '0012345678', GETDATE(), 'HAVALE - AHMET YILMAZ LTD ŞTİ ÖDEME', 8500.00, 'TRY', 'C', NEWID(), 0, 0, 0, GETUTCDATE())
```
3. **Beklenen:** Kayıt "Eşleşti", tooltip: "AHMET YILMAZ LTD ŞTİ"

---

# BÖLÜM G: LOGO TIGER HESAP PLANI SENKRONIZASYON TESTİ

## G.1 — Web Üzerinden Senkronize (SQL Yöntemi)

**Ön koşul:** CompanyErpConnections tablosunda Logo Tiger SQL bağlantısı olmalı

1. SSMS'de ekleyin:
```sql
INSERT INTO CompanyErpConnections (CompanyId, ErpSystemId, Server, Port, DatabaseName, UseIntegratedSecurity, DbUser, DbPasswordEncrypted, IsActive, IsDeleted, CreatedAtUtc, ExtendedProperty01)
VALUES (1, 1, 'DESKTOP-54QF28R\ZRV2014EXP', 1433, 'LOGO_TIGER_DB_ADI', 1, NULL, NULL, 1, 0, GETUTCDATE(), '325')
```
(LOGO_TIGER_DB_ADI yerine Logo Tiger veritabanının gerçek adını yazın)

2. Web → Sol menü → **ERP Senkronize** bağlantısına tıklayın → onaylayın
3. **Beklenen:** "ERP hesap planı senkronizasyonu tamamlandı." mesajı
4. **SSMS doğrulama:**
```sql
SELECT COUNT(*) AS GlCount FROM ErpGlAccounts WHERE CompanyId = 1
SELECT COUNT(*) AS CurrentCount FROM ErpCurrentAccounts WHERE CompanyId = 1
SELECT COUNT(*) AS BankCount FROM ErpBankAccounts WHERE CompanyId = 1
SELECT TOP 10 GlCode, GlName FROM ErpGlAccounts WHERE CompanyId = 1 ORDER BY GlCode
SELECT TOP 10 CurrentCode, CurrentName FROM ErpCurrentAccounts WHERE CompanyId = 1 ORDER BY CurrentCode
```

## G.2 — DesktopConnector ile Senkronize (COM Yöntemi)

**Ön koşul:** Logo Tiger kurulu + UnityObjects.dll kayıtlı

1. DesktopConnector çalışırken Logo Tiger hesap planları COM üzerinden çekilir
2. Bu yöntem web'den tetiklenmez, connector içindeki worker tarafından yapılır

---

# BÖLÜM H: HATA SENARYOLARI

## H.1 — Veritabanı Bağlantı Kesintisi
1. SQL Server servisini durdurun
2. Web sayfasını yenileyin
3. **Beklenen:** 500 hata sayfası (güzel Error sayfası), crash değil

## H.2 — Aynı Kaydı İki Kez Aktarma
1. Bir kaydı aktarın
2. SSMS'den `TransferBatchNo`'yu NULL yapın ve tekrar aktarmayı deneyin
3. **Beklenen:** Unique constraint hatası veya "zaten aktarılmış" mesajı

## H.3 — Boş GL Kodu ile Aktarım
1. Aktar modal'ında GL Kodu seçmeden Aktar butonuna basın
2. **Beklenen:** Tarayıcı validasyonu "required" uyarısı verir

## H.4 — DesktopConnector Bağlantı Kesintisi
1. DesktopConnector'ı durdurun
2. Web'den aktarım yapın
3. **Beklenen:** Kayıt "Pending" durumunda kalır, Connector tekrar başlayınca işlenir

---

# BÖLÜM I: FİNAL KONTROL LİSTESİ

Her testi yaptıktan sonra işaretleyin:

### Ortam
- [ ] Build başarılı (0 error)
- [ ] Migration çalıştı
- [ ] Veritabanı seed data doğrulandı (3 user, 6 role, 14 menu)

### Authentication
- [ ] admin girişi → tüm menüler
- [ ] muhasebe girişi → kısıtlı menü
- [ ] system girişi → kısıtlı menü
- [ ] hatalı şifre → hata mesajı
- [ ] yetkisiz URL → erişim reddedildi

### Dashboard
- [ ] 4 stat kartı görünüyor
- [ ] Bar chart render oluyor
- [ ] Doughnut chart render oluyor
- [ ] Sistem sağlığı tablosu görünüyor

### Banka Tanımları
- [ ] Banka ekleme çalışıyor
- [ ] Hesap ekleme çalışıyor
- [ ] Credential ekleme çalışıyor
- [ ] Veriler SSMS'de doğrulandı

### Hesap Hareketleri
- [ ] Filtre formu çalışıyor
- [ ] Tablo doğru render oluyor
- [ ] Eşleşme sütunu görünüyor
- [ ] Tekli aktarım (modal) çalışıyor
- [ ] Toplu aktarım (checkbox) çalışıyor
- [ ] GL/Cari/Banka dropdown'ları yükleniyor

### ERP Aktarım
- [ ] Aktarım kuyrukları listeleniyor
- [ ] Başarısız aktarımlar listeleniyor
- [ ] Retry çalışıyor

### Tanımlar
- [ ] ERP Kod Eşleme CRUD çalışıyor
- [ ] Kural Motoru CRUD çalışıyor

### Raporlar & Loglar
- [ ] 6 stat kartı görünüyor
- [ ] 3 rapor tablosu görünüyor
- [ ] Bildirimler görünüyor
- [ ] "Okundu" butonu çalışıyor
- [ ] Denetim logları görünüyor

### Kullanıcı Yönetimi
- [ ] Kullanıcı listesi (roller ile)
- [ ] Yeni kullanıcı ekleme
- [ ] Rol atama
- [ ] Yeni kullanıcı ile giriş

### WorkerService
- [ ] Console'da hata yok
- [ ] DummyBankProvider import çalışıyor
- [ ] BankTransactions tablosuna kayıt yazılıyor
- [ ] Otomatik eşleştirme çalışıyor

### DesktopConnector
- [ ] Console'da başlatılıyor
- [ ] Heartbeat DB'ye yazılıyor
- [ ] Connector sayfasında görünüyor
- [ ] (Logo varsa) Login başarılı
- [ ] (Logo varsa) Transfer başarılı

### End-to-End
- [ ] Banka ekleme → Import → Eşleştirme → Aktarım → Başarılı akış tamamlandı
- [ ] Dashboard stat kartları güncellendi
