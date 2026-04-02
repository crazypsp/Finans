# Visual Studio Test Rehberi — Adım Adım

## ADIM 1: Solution'ı Açın ve Build Edin

1. Visual Studio 2022 açın
2. File → Open → Project/Solution → `Finans.sln` seçin
3. Solution Explorer'da Solution'a sağ tık → **Restore NuGet Packages**
4. **Build → Rebuild Solution** (Ctrl+Shift+B)
5. Error List'te **0 Error** olmalı

---

## ADIM 2: Migration — Veritabanını Oluşturun

1. Tools → NuGet Package Manager → **Package Manager Console**
2. Default project → **Finans.Data** seçin
3. Çalıştırın:

```
Add-Migration InitialCreate -StartupProject Finans.WebMvc
```

4. Hata yoksa:

```
Update-Database -StartupProject Finans.WebMvc
```

5. SSMS'de **FinansDB** veritabanının oluştuğunu doğrulayın

---

## ADIM 3: Web Projesini Test Edin (Finans.WebMvc)

### 3.1 Başlatma
1. Solution Explorer → **Finans.WebMvc** sağ tık → Set as Startup Project
2. **F5** basın (Debug mode)
3. Tarayıcı açılır

### 3.2 Login Testi
- Kullanıcı: `admin` — Şifre: `Admin123!` → **Tüm menüler** görünmeli
- Kullanıcı: `muhasebe` — Şifre: `Muhasebe123!` → Kullanıcı Yönetimi ve Connector **görünmemeli**
- Kullanıcı: `system` — Şifre: `System123!` → Tanımlar ve Loglar **görünmemeli**

### 3.3 Dashboard Testi
- 4 stat kartı görünmeli (Toplam İşlem, Aktarılmamış, Aktarılmış, Toplam Tutar)
- Bar chart ve doughnut chart render olmalı
- Sistem Sağlığı tablosu görünmeli

### 3.4 Banka Tanımları Testi
1. Sol menü → **Banka Tanımları**
2. Yeni banka ekleyin:
   - Banka Adı: `Test Bankası`
   - Provider Kodu: `DUMMY`
   - Ekle butonuna basın
3. **Hesaplar** sekmesi → Yeni hesap:
   - Banka ID: `1`
   - Hesap No: `0012345678`
   - Döviz: `TRY`
4. **Credential** sekmesi → Yeni credential:
   - Banka ID: `1`
   - Kullanıcı: `test`
   - Şifre: `test123`

### 3.5 Hesap Hareketleri Testi
1. Sol menü → **Hesap Hareketleri**
2. Filtre alanları görünmeli
3. Tarih aralığı seçin → **Listele** basın
4. Eğer kayıt varsa: bir kaydın **Aktar** butonuna basın
5. Modal açılır → GL Kodu ve Banka Hesap Kodu seçin → **Aktar**

### 3.6 Diğer Sayfalar
- **ERP Aktarım Kuyrukları** → Batch listesi
- **Başarısız Aktarımlar** → Retry butonu
- **ERP Kod Eşleme** → Yeni eşleme oluştur/sil
- **Kural Motoru** → Yeni kural oluştur/sil
- **Raporlar** → 3 rapor tablosu + 6 stat kartı
- **Sistem Logları** → Bildirimler + denetim logları
- **Kullanıcı Yönetimi** → Kullanıcı listesi + yeni kullanıcı + rol ata

---

## ADIM 4: WorkerService'i Test Edin (Finans.WorkerService)

### 4.1 Ayrı başlatma
1. Solution Explorer → **Finans.WorkerService** sağ tık → Set as Startup Project
2. **F5** basın
3. Console penceresinde şunları görmeli:
```
info: Finans.WorkerService.Workers.BankImportWorker[0]
      Bank import worker çalışıyor...
```
4. 5 dakika bekleyin — DummyBankProvider bir test kaydı oluşturur
5. Web'de Hesap Hareketleri sayfasında kontrol edin
6. **Ctrl+C** ile durdurun

---

## ADIM 5: DesktopConnector'ı Test Edin (Finans.DesktopConnector)

### 5.1 Logo Tiger OLMADAN test
1. Solution Explorer → **Finans.DesktopConnector** sağ tık → Set as Startup Project
2. **F5** basın
3. Console'da:
```
[INF] ERP Transfer Worker başlatıldı. Interval=30s
[ERR] Logo Tiger COM bileşeni bulunamadı.
```
4. Bu normal — Logo Tiger yoksa COM hatası verir ama heartbeat çalışır
5. Web → **Connector** sayfasında connector kartı görünmeli

### 5.2 Logo Tiger VARSA test
1. `appsettings.json`'da Logo Tiger bilgilerini girin
2. F5 ile başlatın
3. Console'da login başarılı mesajı görünmeli
4. Web'den bir hareket aktarın → 30 sn bekleyin → Durum "Success" olmalı

---

## ADIM 6: 3 Projeyi Aynı Anda Çalıştırın

1. Solution Explorer → Solution'a sağ tık → **Set Startup Projects...**
2. **Multiple startup projects** seçin
3. Şu projeleri **Start** yapın:
   - Finans.WebMvc → **Start**
   - Finans.DesktopConnector → **Start**
   - Finans.WorkerService → **Start**
4. **OK** → **F5**
5. 3 console/tarayıcı penceresi açılır

### Test akışı:
1. Tarayıcıda `admin` ile giriş yapın
2. Banka Tanımları → DUMMY banka + hesap + credential ekleyin
3. 5 dakika bekleyin → WorkerService otomatik import yapar
4. Hesap Hareketleri → Test kaydı görünür
5. Kaydı "Aktar" butonuyla aktarın
6. DesktopConnector 30 sn içinde işler (Logo yoksa FakeClient ile başarılı olur)
7. Durum "Pending" → "Success" olur

---

## SORUN GİDERME

| Sorun | Çözüm |
|-------|-------|
| Build failed | NuGet Restore yapın, Error List'e bakın |
| Migration hatası | Migrations klasörünü temizleyip yeniden oluşturun |
| Login çalışmıyor | Connection string doğru mu kontrol edin |
| Dashboard boş | Henüz veri yok, WorkerService ile import yapın |
| Aktar modal boş | ERP Senkronize'den hesap planı çekin veya manuel ekleyin |
| Connector sayfası boş | DesktopConnector çalıştırın (heartbeat gönderir) |
