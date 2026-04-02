# Finans Yönetim Sistemi — Test ve Sunum Dokümanı
## Çarşamba Sunumu İçin Hazırlanmıştır

---

# 1. SİSTEM MİMARİSİ

## 1.1 Proje Yapısı

| Proje | Rolü | Çalıştığı Yer |
|-------|------|---------------|
| Finans.WebMvc | Web arayüzü, kullanıcı etkileşimi | IIS / Kestrel |
| Finans.DesktopConnector | Logo Tiger COM entegrasyonu | Windows Service (Logo makinesi) |
| Finans.WorkerService | Otomatik banka import | Windows Service (sunucu) |
| Finans.Application | İş kuralları katmanı | Diğer 3 proje tarafından kullanılır |
| Finans.Infrastructure | Banka provider'ları, Dapper sorguları | Diğer 3 proje tarafından kullanılır |
| Finans.Data | EF Core DbContext, Migration | Diğer 3 proje tarafından kullanılır |
| Finans.Entities | Domain entity'leri | Tüm projeler tarafından kullanılır |
| Finans.Contracts | DTO'lar | Tüm projeler tarafından kullanılır |

## 1.2 İletişim Modeli

3 proje birbirleriyle doğrudan HTTP/API üzerinden konuşmaz.
Tüm iletişim SQL Server üzerinden yapılır (shared database pattern).

Web → DB'ye "Pending" yazar → Connector DB'den okur → Logo Tiger'a aktarır → Sonucu DB'ye yazar

---

# 2. VISUAL STUDIO TEST ADIMLARI

## 2.1 Ön Hazırlık

### Adım 1: Solution'ı Açma
1. Visual Studio 2022'yi açın
2. File → Open → Project/Solution
3. `Finans.sln` dosyasını seçin

### Adım 2: NuGet Restore
1. Solution Explorer'da Solution'a sağ tıklayın
2. "Restore NuGet Packages" seçin
3. Tüm paketlerin yüklenmesini bekleyin

### Adım 3: Build
1. Build → Build Solution (Ctrl+Shift+B)
2. Error List penceresinde 0 hata olmalı
3. Eğer hata varsa → Error List'teki hatayı çift tıklayın → ilgili dosyaya gidin

### Adım 4: Migration (İlk kez ise)
1. Tools → NuGet Package Manager → Package Manager Console
2. Default project: Finans.Data seçin
3. Çalıştırın:
```
Add-Migration InitialCreate -StartupProject Finans.WebMvc
Update-Database -StartupProject Finans.WebMvc
```

## 2.2 Web Projesi Testi (Finans.WebMvc)

### Test 1: Başlatma ve Login
1. Solution Explorer → Finans.WebMvc sağ tık → "Set as Startup Project"
2. F5 (Debug) veya Ctrl+F5 (Debug olmadan)
3. Tarayıcı açılır → Login ekranı gelmeli
4. Giriş: `admin` / `Admin123!`
5. BEKLENEN: Dashboard sayfası açılır, sol menü görünür

### Test 2: Dashboard Kontrolü
1. Dashboard'da 4 stat kartı görünmeli (Toplam İşlem, Aktarılmamış, Aktarılmış, Toplam Tutar)
2. Bar chart ve doughnut chart görünmeli
3. Sistem Sağlığı tablosu görünmeli
4. BEKLENEN: Tüm kartlar ve grafikler sorunsuz render olur

### Test 3: Banka Tanımları
1. Sol menüden "Banka Tanımları" tıklayın
2. Yeni banka ekleyin:
   - Banka Adı: Akbank
   - Provider Kodu: AKB
   - External ID: 1
3. "Ekle" butonuna basın
4. BEKLENEN: "Banka eklendi" mesajı, tablo güncellenir

### Test 4: Hesap Ekleme
1. "Hesaplar" sekmesine geçin
2. Yeni hesap ekleyin:
   - Banka ID: 1 (az önce eklediğiniz)
   - Hesap No: 0012345678
   - IBAN: TR330006100519786457841326
   - Döviz: TRY
3. "Ekle" butonuna basın
4. BEKLENEN: Hesap tabloya eklenir

### Test 5: Credential Ekleme
1. "Credential" sekmesine geçin
2. Yeni credential:
   - Banka ID: 1
   - Kullanıcı Adı: test_user
   - Şifre: test_pass
3. "Ekle" butonuna basın
4. BEKLENEN: Credential eklenir

### Test 6: Hesap Hareketleri Sayfası
1. Sol menüden "Hesap Hareketleri" tıklayın
2. Filtre alanları görünmeli (Banka, Tarih, Hesap No vs.)
3. "Listele" butonuna basın
4. BEKLENEN: Eğer import yapılmışsa kayıtlar görünür, yoksa "Kayıt bulunamadı"

### Test 7: ERP Kod Eşleme
1. Sol menüden "ERP Kod Eşleme" tıklayın
2. "Yeni Eşleme" butonuna basın
3. Bilgileri doldurun ve kaydedin
4. BEKLENEN: Eşleme listede görünür

### Test 8: Kural Motoru
1. Sol menüden "Kural Motoru" tıklayın
2. "Yeni Kural" butonuna basın
3. Kural oluşturun (açıklama içerir: "EFT", GL Kod, Cari Kod)
4. BEKLENEN: Kural listede görünür

### Test 9: Raporlar
1. Sol menüden "Raporlar" tıklayın
2. 6 stat kartı görünmeli
3. Banka Import, ERP Transfer, Connector raporları görünmeli
4. BEKLENEN: Sayfada hata yok

### Test 10: Sistem Logları
1. Sol menüden "Sistem Logları" tıklayın
2. Bildirimler ve Denetim Logları görünmeli
4. BEKLENEN: Sayfada hata yok

### Test 11: Kullanıcı Yönetimi (Admin)
1. Sol menüden "Kullanıcı Yönetimi" tıklayın
2. 3 kullanıcı listelenmeli (admin, system, muhasebe)
3. Roller sütununda roller görünmeli
4. BEKLENEN: Kullanıcı tablosu doğru görünür

### Test 12: Farklı Kullanıcılarla Giriş
1. Çıkış yapın (sağ üst dropdown → Çıkış)
2. `muhasebe` / `Muhasebe123!` ile giriş yapın
3. BEKLENEN: Sol menüde "Kullanıcı Yönetimi" ve "Connector" görünmez
4. Dashboard, Banka İşlemleri, Tanımlar, Raporlar, Loglar görünür

## 2.3 DesktopConnector Testi (Finans.DesktopConnector)

### Ön Koşul: Logo Tiger Kurulu Olmalı

### Test 1: Console Modunda Başlatma
1. Solution Explorer → Finans.DesktopConnector sağ tık → "Set as Startup Project"
2. F5 ile başlatın
3. Console penceresi açılır
4. BEKLENEN MESAJLAR:
```
[INF] ERP Transfer Worker baslatildi. Interval=30s
[INF] Connector heartbeat yazildi.
```
5. Eğer Logo Tiger yoksa: "Logo Tiger COM bileseni bulunamadi" mesajı normal

### Test 2: Heartbeat Kontrolü
1. DesktopConnector çalışırken web uygulamasını açın
2. Sol menü → Connector sayfasına gidin
3. BEKLENEN: Connector kartında "Aktif" durumu, son heartbeat tarihi görünür

### Test 3: Logo Tiger Bağlantı
1. appsettings.json'da Logo Tiger bilgilerini doğru girin
2. DesktopConnector'ı yeniden başlatın
3. BEKLENEN: "Logo Tiger login basarisiz" hatası yoksa bağlantı başarılı

### Test 4: ERP Transfer Akışı (End-to-End)
1. Web'de "Hesap Hareketleri" → bir kaydı seçin → "Aktar" butonu
2. Modal'da GL Kodu ve Banka Hesap Kodu seçin → "Aktar"
3. 30 saniye bekleyin (DesktopConnector periyodik çalışır)
4. BEKLENEN: Kaydın durumu "Pending" → "Success" olur

## 2.4 WorkerService Testi (Finans.WorkerService)

### Test 1: Console Modunda Başlatma
1. Solution Explorer → Finans.WorkerService sağ tık → "Set as Startup Project"
2. F5 ile başlatın
3. BEKLENEN:
```
[INF] Bank import worker çalışıyor...
```
4. Worker 5 dakikada bir banka import yapar

### Test 2: DummyBankProvider ile Test
1. Banka tanımlarında Provider Kodu "DUMMY" olan bir banka ekleyin
2. Hesap ve credential ekleyin
3. WorkerService'i çalıştırın
4. 5 dakika bekleyin
5. Web'de "Hesap Hareketleri" sayfasında DummyBankProvider'ın oluşturduğu test kaydı görünmeli

## 2.5 Üç Projeyi Aynı Anda Çalıştırma

### Yöntem 1: Multiple Startup Projects
1. Solution Explorer → Solution'a sağ tıklayın
2. "Set Startup Projects..." seçin
3. "Multiple startup projects" seçin
4. Finans.WebMvc → Start
5. Finans.DesktopConnector → Start
6. (Opsiyonel) Finans.WorkerService → Start
7. OK → F5

### Yöntem 2: Ayrı Ayrı Çalıştırma
1. Finans.WebMvc'yi F5 ile başlatın
2. Yeni terminal açın → `cd Finans.DesktopConnector && dotnet run`
3. Yeni terminal açın → `cd Finans.WorkerService && dotnet run`

---

# 3. LOGO TIGER ENTEGRASYON DETAYLARI

## 3.1 Hesap Planı Çekme Akışı

### Nereden Tetiklenir?
- Web: Sol menü → "ERP Senkronize" → `ErpSyncController.SyncAll()`
- DesktopConnector: `LogoTigerAccountSyncService` COM üzerinden

### Ne Çeker?
| Veri Tipi | Logo Tiger Nesne | DataObject ID | Hedef Tablo |
|-----------|------------------|---------------|-------------|
| Muhasebe Hesapları | AccountPlan | 12 | ErpGlAccounts |
| Cari Hesaplar | ArpCard | 18 | ErpCurrentAccounts |
| Banka Hesapları | BankAccount | 62 | ErpBankAccounts |

### COM Çağrı Sırası:
```
1. Type.GetTypeFromProgID("UnityObjects.UnityApplication")
2. unityApp = Activator.CreateInstance(unityType)
3. unityApp.Login(userName, password, firmNr, periodNr)
4. dataObj = unityApp.NewDataObject(12)  // GL hesapları
5. browser = dataObj.CreateBrowser()
6. browser.MoveFirst()
7. while (!browser.EOF) {
     code = browser.FieldByName("CODE").Value
     name = browser.FieldByName("DEFINITION_").Value
     // DB'ye kaydet
     browser.MoveNext()
   }
8. unityApp.CompanyLogout()
9. unityApp.UserLogout()
```

## 3.2 Eşleştirme (Matching) Akışı

### Otomatik Eşleştirme
Banka import sonrasında `BankTransactionMatchingService` çağrılır:

1. Eşleşmemiş tüm banka hareketleri çekilir
2. Her hareket için sırasıyla:
   a. **Kural Motoru** — BankTransactionRules tablosundaki kurallara göre eşleştir
      (açıklama içerir, tutar aralığı, döviz, borç/alacak)
   b. **ERP Kod Eşleme** — ErpCodeMappings tablosundaki eşlemelere göre eşleştir
      (banka, döviz, açıklama anahtar kelimesi)
   c. **Cari Hesap Arama** — İşlem açıklamasında Logo Tiger cari hesap adı geçiyor mu?
3. Eşleşen işlemler: `IsMatched=true`, `MatchedCurrentCode`, `MatchedCurrentName` güncellenir

### Manuel Eşleştirme
Web'de "Hesap Hareketleri" → "Aktar" butonu → Modal'da GL/Cari/Banka kodu manuel seçilir

## 3.3 Logo Tiger'a Aktarım (Fiş Oluşturma)

### Akış:
```
1. Web'de kullanıcı "Aktar" butonuna basar
2. ErpTransferBatch + ErpTransferItem oluşur (Status=Pending)
3. DesktopConnector'daki ErpTransferWorker 30 sn'de bir tarar
4. Pending item bulunca LogoTigerTransferService çağrılır:

   a. unityApp.Login()
   b. data = unityApp.NewDataObject(24)  // Muhasebe Fişi
   c. data.New()

   d. Header:
      - TYPE = 1 (Mahsup fişi)
      - DATE = İşlem tarihi
      - DOC_NUMBER = Referans numarası
      - NOTES1 = Açıklama

   e. Satırlar (Alacak işlemi ise):
      Satır 1: Banka hesabı → BORÇ (debit)
      Satır 2: GL hesabı → ALACAK (credit)

      (Borç işlemi ise tersi)

   f. data.Post() → Logo Tiger'a kaydet
   g. VoucherNo = data.FieldByName("NUMBER").Value

5. Başarılı: ErpTransferItem.Status = "Success"
6. BankTransaction.IsTransferred = true
```

---

# 4. SUNUM SENARYOSU (Çarşamba)

## Zaman Planı: ~15 dakika

### Bölüm 1: Giriş ve Mimari (2 dk)
- Projenin amacını açıklayın: "Bankalardan hesap hareketlerini çekip Logo Tiger'a aktaran sistem"
- 3 proje + veritabanı mimarisini anlatın
- "Clean Architecture + CQRS + Multi-tenant"

### Bölüm 2: Demo — Web Arayüzü (5 dk)
1. Login → Dashboard (grafikler)
2. Banka Tanımları → Banka + Hesap + Credential ekleme
3. Hesap Hareketleri → Filtreleme → Aktarım
4. ERP Aktarım Kuyrukları → Durumlar
5. Raporlar → 3 rapor türü
6. Kullanıcı Yönetimi → Rol bazlı yetkilendirme

### Bölüm 3: Demo — Logo Tiger Entegrasyonu (3 dk)
1. ERP Senkronize → Hesap planı çekme
2. Kural Motoru → Otomatik eşleştirme kuralları
3. Hesap Hareketleri → "Aktar" → Modal → GL/Cari seçimi
4. DesktopConnector loglarını gösterme

### Bölüm 4: Teknik Detaylar (3 dk)
- Entity Framework Core + Dapper hibrit kullanım
- Banka provider pattern (yeni banka eklemek kolay)
- Logo Tiger COM entegrasyonu
- Windows Service olarak çalışma
- Serilog ile loglama

### Bölüm 5: Soru-Cevap (2 dk)

## Sunumda Gösterilecek Ekranlar
1. Login ekranı (kurumsal tasarım)
2. Dashboard (grafikler, stat kartları)
3. Banka Tanımları (CRUD)
4. Hesap Hareketleri (filtreleme + aktarım modal)
5. ERP Aktarım Kuyrukları (batch takibi)
6. Raporlar (3 rapor + 6 stat kartı)
7. Kural Motoru (otomatik eşleştirme)
8. Kullanıcı Yönetimi (roller)
9. Connector sayfası (heartbeat durumu)
10. DesktopConnector console logları

---

# 5. BİLİNEN KISITLAR VE NOTLAR

## 5.1 Logo Tiger Gereksinimleri
- Logo Tiger aynı makinede kurulu olmalı (COM/DLL)
- UnityObjects.dll kayıtlı olmalı: `regsvr32 UnityObjects.dll`
- Logo Tiger açık olmalı (veya servis modunda çalışmalı)
- Doğru firma ve dönem numarası girilmeli

## 5.2 Banka Provider'ları
- Gerçek banka entegrasyonları (Akbank, İşBank vs.) Connected Services üzerinden çalışır
- Test için DummyBankProvider kullanılabilir (ProviderCode: "DUMMY")
- Her yeni banka için IBankProvider interface'i implement edilir

## 5.3 Güvenlik
- Şifreler PBKDF2-SHA256 ile hash'lenir (100K iterasyon)
- Cookie-based authentication (8 saat süre)
- Rol bazlı menü yetkilendirmesi
- Soft delete (fiziksel silme yok)
- Concurrency control (RowVersion)

---

# 6. HATA AYIKLAMA REHBERİ

## 6.1 Sık Karşılaşılan Hatalar

| Hata | Sebep | Çözüm |
|------|-------|-------|
| Build failed | NuGet paketleri yüklenmemiş | Solution sağ tık → Restore NuGet |
| CS1963 | Dynamic değişken LINQ'da kullanılmış | object? olarak açık cast yapın |
| Login başarısız | Şifre hash uyumsuzluğu | Seed data ile oluşan hash'ler kullanın |
| 502.5 IIS hatası | .NET 8 Hosting Bundle yüklü değil | Hosting Bundle yükleyin |
| Veritabanı bağlantı hatası | Connection string yanlış | appsettings.json kontrol edin |
| Logo COM bulunamadı | UnityObjects.dll kayıtlı değil | regsvr32 komutu çalıştırın |
| Aktarım "Failed" | Logo Tiger bağlantı sorunu | appsettings.json Logo ayarlarını kontrol edin |

## 6.2 Debug İpuçları
- **Breakpoint**: İlgili controller veya service'e F9 ile breakpoint koyun
- **Watch**: Debug sırasında değişkenleri Watch penceresine ekleyin
- **Output**: View → Output → ASP.NET Core loglarını izleyin
- **SQL Profiler**: Çalışan SQL sorgularını görmek için SSMS SQL Profiler kullanın
- **Serilog**: DesktopConnector logları `logs/` klasöründe

---

# 7. CHECKLIST — SUNUM ÖNCESİ

- [ ] Solution build ediyor (0 hata)
- [ ] Migration çalışıyor (veritabanı mevcut)
- [ ] Web projesi F5 ile açılıyor
- [ ] Login çalışıyor (admin / Admin123!)
- [ ] Dashboard'da grafikler görünüyor
- [ ] Banka tanımları CRUD çalışıyor
- [ ] Hesap hareketleri sayfası açılıyor
- [ ] "Aktar" modalı açılıyor, GL/Cari listeleri yükleniyor
- [ ] ERP aktarım kuyrukları sayfası çalışıyor
- [ ] Raporlar sayfası çalışıyor
- [ ] Kullanıcı yönetimi çalışıyor
- [ ] Farklı kullanıcılarla giriş — menü yetkilendirmesi çalışıyor
- [ ] DesktopConnector console'da hata yok
- [ ] (Logo Tiger varsa) Hesap planı senkronizasyonu çalışıyor
- [ ] (Logo Tiger varsa) Fiş aktarımı çalışıyor
