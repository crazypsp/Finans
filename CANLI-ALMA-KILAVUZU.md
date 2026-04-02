# Finans Yönetim Sistemi — Canlıya Alma Kılavuzu
## Çarşamba Sunumu İçin Eksiksiz Adım Adım Rehber

---

# BÖLÜM 1: ÖN GEREKSİNİMLER

## 1.1 Sunucuda Olması Gerekenler

### Windows Server
- Windows Server 2019 veya üzeri (Windows 10/11 da olur geliştirme için)
- Yönetici (Administrator) erişimi

### SQL Server
- SQL Server 2014 Express veya üzeri (sizde ZRV2014EXP var — uygun)
- SQL Server Management Studio (SSMS)

### .NET 8 Runtime
- Sunucuya .NET 8 **Hosting Bundle** yüklenmelidir
- İndirme: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- "Hosting Bundle" bölümünden indirin (ASP.NET Core Runtime + IIS desteği birlikte gelir)
- Yükledikten sonra bilgisayarı yeniden başlatın

### IIS (Internet Information Services)
- Kontrol Paneli → Programlar → Windows Özelliklerini Aç/Kapat
- "Internet Information Services" kutucuğunu işaretleyin
- Alt seçeneklerden şunları da işaretleyin:
  - Web Yönetim Araçları → IIS Yönetim Konsolu
  - World Wide Web Hizmetleri → Uygulama Geliştirme Özellikleri → ASP.NET 4.8
- Tamam'a basın ve kurulumu bekleyin

### Logo Tiger (DesktopConnector için)
- Logo Tiger kurulu olmalı (aynı makinede)
- UnityObjects.dll COM olarak kayıtlı olmalı

---

# BÖLÜM 2: VERİTABANI HAZIRLIĞI

## 2.1 Migration Zaten Yapıldı

Veritabanınız zaten ayağa kalkmış durumda. Eğer sunucuda farklı bir SQL Server kullanacaksanız:

## 2.2 Sunucu SQL Server'a Taşıma (Gerekirse)

### Yöntem A: Backup/Restore
1. SSMS'de mevcut FinansDB'ye sağ tık → Tasks → Back Up
2. Backup dosyasını sunucuya kopyalayın
3. Sunucudaki SSMS'de Databases → Restore Database
4. Backup dosyasını seçin → OK

### Yöntem B: Aynı makine ise
- Zaten hazır, bir şey yapmanıza gerek yok

---

# BÖLÜM 3: WEB PROJESİNİ YAYINLAMA (Finans.WebMvc)

## 3.1 Visual Studio'dan Publish

### Adım 1: Publish Profili Oluşturma
1. Visual Studio'da Solution Explorer'da `Finans.WebMvc` projesine sağ tıklayın
2. **"Publish..."** seçin
3. **"Folder"** seçin → Next
4. Hedef klasör: `C:\Publish\FinansWeb` yazın
5. **"Finish"** butonuna basın
6. **"Publish"** butonuna basın

### Adım 2: appsettings.json Kontrolü
Publish edilen klasördeki `appsettings.json` dosyasını açın ve connection string'in doğru olduğundan emin olun:

```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Eğer sunucuda farklı bir SQL Server varsa**, `Server=` kısmını sunucunun adıyla değiştirin.

## 3.2 IIS'de Web Sitesi Oluşturma

### Adım 1: Application Pool Oluşturma
1. **IIS Manager** açın (Başlat → "IIS" aratın)
2. Sol tarafta sunucu adınızın altında **"Application Pools"** öğesine tıklayın
3. Sağ tarafta **"Add Application Pool..."** tıklayın
4. Ayarlar:
   - **Name**: `FinansPool`
   - **.NET CLR version**: `No Managed Code`
   - **Managed pipeline mode**: `Integrated`
5. **OK** butonuna basın
6. Oluşan `FinansPool`'a sağ tık → **"Advanced Settings..."**
7. **Identity** satırına tıklayın → `...` butonuna basın
8. **"Custom account"** seçin → **"Set..."** butonuna basın
9. Windows kullanıcı bilgilerinizi girin (SQL Server'a Windows Auth ile bağlanabilecek kullanıcı)
   - User name: `DESKTOP-54QF28R\yusuf` (sizin kullanıcı adınız)
   - Password: Windows şifreniz
10. OK → OK → OK

### Adım 2: Web Sitesi Oluşturma
1. IIS Manager'da sol tarafta **"Sites"** öğesine sağ tıklayın
2. **"Add Website..."** seçin
3. Ayarlar:
   - **Site name**: `Finans`
   - **Application pool**: `FinansPool` (az önce oluşturduğunuz)
   - **Physical path**: `C:\Publish\FinansWeb` (publish ettiğiniz klasör)
   - **Binding**:
     - Type: `http`
     - IP Address: `All Unassigned`
     - Port: `8080` (veya istediğiniz port, 80 başka site kullanıyorsa)
     - Host name: boş bırakın
4. **OK** butonuna basın

### Adım 3: Klasör İzinleri
1. `C:\Publish\FinansWeb` klasörüne sağ tıklayın → Özellikler → Güvenlik sekmesi
2. **Düzenle** → **Ekle** butonuna basın
3. `IIS_IUSRS` yazın → Adları Denetle → Tamam
4. `IIS_IUSRS` için **Okuma ve Yürütme** izni verin
5. Ayrıca `IIS AppPool\FinansPool` kullanıcısını da ekleyin ve aynı izinleri verin
6. Tamam → Tamam

### Adım 4: Test
1. Tarayıcıyı açın
2. `http://localhost:8080` adresine gidin
3. Login ekranı gelmeli
4. `admin` / `Admin123!` ile giriş yapın

### Sorun Giderme
- **502.5 hatası**: .NET 8 Hosting Bundle yüklü değildir → yükleyin ve bilgisayarı yeniden başlatın
- **500 hatası**: `C:\Publish\FinansWeb\web.config` dosyasında `stdoutLogEnabled` değerini `true` yapın:
  ```xml
  <aspNetCore processPath="dotnet" arguments=".\Finans.WebMvc.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
  ```
  Sonra `logs` klasörü oluşturun ve hatayı log dosyasından okuyun.
- **Veritabanı bağlantı hatası**: appsettings.json'daki connection string'i kontrol edin

---

# BÖLÜM 4: DESKTOP CONNECTOR KURULUMU (Finans.DesktopConnector)

## 4.1 Publish

### Visual Studio'dan:
1. `Finans.DesktopConnector` projesine sağ tık → **"Publish..."**
2. **"Folder"** seçin → Next
3. Hedef: `C:\Publish\FinansConnector`
4. **Finish** → **Publish**

### Veya Komut Satırından:
```
cd C:\...\Finans\Finans
dotnet publish Finans.DesktopConnector -c Release -o C:\Publish\FinansConnector
```

## 4.2 appsettings.json Düzenleme

`C:\Publish\FinansConnector\appsettings.json` dosyasını açın ve Logo Tiger bilgilerinizi girin:

```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "LogoTiger": {
    "UserName": "GERCEK_LOGO_KULLANICI_ADINIZ",
    "Password": "GERCEK_LOGO_SIFRENIZ",
    "FirmNr": 1,
    "PeriodNr": 1
  },
  "Connector": {
    "CompanyId": 1,
    "ConnectorKey": "DEFAULT-CONNECTOR",
    "Version": "1.0.0",
    "MachineName": "",
    "PollIntervalSeconds": 60
  },
  "Worker": {
    "ErpTransferIntervalSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**ÖNEMLİ ALANLAR:**
- `LogoTiger:UserName` → Logo Tiger'a giriş yaptığınız kullanıcı adı
- `LogoTiger:Password` → Logo Tiger şifresi
- `LogoTiger:FirmNr` → Logo Tiger'daki firma numarası (genelde 1)
- `LogoTiger:PeriodNr` → Çalışma dönemi (genelde 1, yılın dönemi)

## 4.3 Önce Console Olarak Test Edin

Windows Service olarak kurmadan önce, düzgün çalıştığından emin olun:

1. Komut satırını **Yönetici olarak** açın
2. Şu komutu çalıştırın:
```
cd C:\Publish\FinansConnector
Finans.DesktopConnector.exe
```
3. Console'da şu mesajları görmelisiniz:
```
[INF] ERP Transfer Worker başlatıldı. Interval=30s
[INF] Connector heartbeat yazıldı.
```
4. Hata yoksa Ctrl+C ile durdurun

### Olası Hatalar:
- **"Logo Tiger COM bileşeni bulunamadı"** → Logo Tiger kurulu değil veya UnityObjects.dll kayıtlı değil
  - Çözüm: Logo Tiger kurulu makinede çalıştırın
  - Veya: `regsvr32 "C:\Logo\Tiger3\UnityObjects.dll"` komutunu çalıştırın
- **"Logo login başarısız"** → Kullanıcı adı/şifre yanlış veya Logo Tiger açık değil
- **Veritabanı hatası** → Connection string'i kontrol edin

## 4.4 Windows Service Olarak Kurma

Test başarılıysa, artık Windows Service olarak kurun:

1. Komut satırını **Yönetici olarak** açın
2. Service oluşturun:
```
sc create FinansConnector binpath="C:\Publish\FinansConnector\Finans.DesktopConnector.exe" start=auto displayname="Finans Desktop Connector"
```
3. Açıklama ekleyin:
```
sc description FinansConnector "Logo Tiger ERP entegrasyonu ve heartbeat servisi"
```
4. Service'i başlatın:
```
sc start FinansConnector
```
5. Durumunu kontrol edin:
```
sc query FinansConnector
```
**STATE: RUNNING** yazmalı.

### Service Yönetimi:
```
sc stop FinansConnector      ← Durdurmak için
sc start FinansConnector     ← Başlatmak için
sc delete FinansConnector    ← Tamamen silmek için (önce stop edin)
```

### Logları Kontrol Etme:
- `C:\Publish\FinansConnector\logs\` klasöründe günlük log dosyaları oluşur
- Windows Event Viewer → Application loglarında da görebilirsiniz

---

# BÖLÜM 5: WORKER SERVICE KURULUMU (Finans.WorkerService)

## 5.1 Publish

```
cd C:\...\Finans\Finans
dotnet publish Finans.WorkerService -c Release -o C:\Publish\FinansWorker
```

Veya Visual Studio'dan: Sağ tık → Publish → Folder → `C:\Publish\FinansWorker`

## 5.2 appsettings.json Düzenleme

`C:\Publish\FinansWorker\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Worker": {
    "BankImportIntervalSeconds": 300,
    "ErpTransferIntervalSeconds": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## 5.3 Console Test

```
cd C:\Publish\FinansWorker
Finans.WorkerService.exe
```

Şu mesajları görmelisiniz:
```
[INF] Bank import worker çalışıyor...
[INF] ERP transfer worker çalışıyor...
```

## 5.4 Windows Service Olarak Kurma

```
sc create FinansWorker binpath="C:\Publish\FinansWorker\Finans.WorkerService.exe" start=auto displayname="Finans Worker Service"
sc description FinansWorker "Banka hesap hareketi import ve ERP transfer servisi"
sc start FinansWorker
```

---

# BÖLÜM 6: SUNUM SENARYOSU (Demo Akışı)

## 6.1 Sunum Öncesi Kontrol Listesi

Çarşamba sunumundan önce bunları kontrol edin:

- [ ] SQL Server çalışıyor ve FinansDB mevcut
- [ ] Web sitesi IIS'de çalışıyor (veya Visual Studio F5 ile)
- [ ] DesktopConnector çalışıyor (console veya service)
- [ ] Logo Tiger açık ve login bilgileri doğru
- [ ] admin / Admin123! ile giriş yapılabiliyor
- [ ] Dashboard'da grafikler görünüyor

## 6.2 Demo Senaryosu — Adım Adım

### Sahne 1: Giriş (1 dakika)
1. Tarayıcıda `http://localhost:8080` açın
2. Login ekranı gösterin: "Kurumsal giriş ekranı"
3. `admin` / `Admin123!` ile giriş yapın
4. "3 farklı kullanıcı tipi var: Admin, Sistem, Muhasebe"

### Sahne 2: Dashboard (2 dakika)
1. Dashboard'u gösterin: "Finansal özet bir bakışta"
2. Stat kartlarını gösterin: "Toplam işlem, aktarılmamış, aktarılmış sayıları"
3. Grafikleri gösterin: "Chart.js ile interaktif grafikler"
4. Sistem sağlığını gösterin: "Connector durumu, son import/transfer tarihleri"

### Sahne 3: Banka Tanımları (2 dakika)
1. Sol menüden "Banka Tanımları"na tıklayın
2. Yeni banka ekleyin:
   - Banka Adı: "Test Bankası"
   - Provider Kodu: "DUMMY"
   - Ekle butonuna basın
3. "Hesaplar" sekmesine geçin → yeni hesap ekleyin
4. "Credential" sekmesi → credential ekleyin
5. "Bankalar dinamik olarak ekleniyor, her banka için provider kodu tanımlanıyor"

### Sahne 4: Hesap Hareketleri (3 dakika)
1. Sol menüden "Hesap Hareketleri"ne tıklayın
2. Filtre alanlarını gösterin: "Banka, tarih aralığı, hesap no, açıklama ile filtreleme"
3. "Listele" butonuna basın
4. Eğer kayıt varsa:
   - "Aktarılmamış kayıtlar 'Yeni' statüsünde"
   - Bir kaydın yanındaki "Aktar" butonuna basın
   - Modal açılır: "GL kodu, cari hesap, banka hesap kodu seçiliyor"
   - "Bu bilgiler Logo Tiger'dan senkronize ediliyor"
   - "Aktar" butonuna basın
5. "Toplu aktarım da yapılabiliyor — checkbox ile seçip 'Kuyruğa Gönder'"

### Sahne 5: ERP Aktarım (2 dakika)
1. "Aktarım Kuyrukları" sayfasını gösterin
2. "Batch bazlı takip: toplam, başarılı, başarısız sayıları"
3. "Başarısız Aktarımlar" sayfasını gösterin
4. "Retry mekanizması var — tekrar deneme butonu ile"

### Sahne 6: Tanımlar (1 dakika)
1. "ERP Kod Eşleme": "Banka hareketi açıklamasına göre otomatik muhasebe hesabı eşleme"
2. "Kural Motoru": "Tutar aralığı, döviz, borç/alacak bazlı otomatik eşleme"

### Sahne 7: Raporlar (1 dakika)
1. Raporlar sayfasını gösterin
2. "3 farklı rapor: Banka Import, ERP Transfer, Connector"
3. "Filtrelenebilir ve detaylı"

### Sahne 8: Sistem (1 dakika)
1. "Sistem Logları": "Denetim logları ve bildirimler"
2. "Connector": "Desktop Connector durumu, heartbeat, versiyon"
3. "Kullanıcı Yönetimi": "Rol bazlı yetkilendirme"

### Sahne 9: Mimari (2 dakika)
1. "3 katmanlı mimari: Web + DesktopConnector + WorkerService"
2. "Hepsi aynı veritabanını paylaşıyor"
3. "Web kullanıcı arayüzü, Connector Logo Tiger entegrasyonu"
4. "Asenkron aktarım: Kullanıcı beklemez, kuyruğa alınır"

---

# BÖLÜM 7: SORUN GİDERME

## 7.1 Web Sitesi Açılmıyor

### Hata: 502.5 Process Failure
**Sebep**: .NET 8 Hosting Bundle yüklü değil
**Çözüm**: https://dotnet.microsoft.com/download/dotnet/8.0 → Hosting Bundle indirip yükleyin → Bilgisayarı yeniden başlatın

### Hata: 500 Internal Server Error
**Sebep**: Uygulama hatası (genelde connection string)
**Çözüm**:
1. `web.config` dosyasında `stdoutLogEnabled="true"` yapın
2. Publish klasöründe `logs` klasörü oluşturun
3. Siteyi yeniden başlatın
4. `logs/stdout_*.log` dosyasını okuyun

### Hata: Login sayfası gelmiyor
**Sebep**: Routing sorunu
**Çözüm**: IIS'de site → Advanced Settings → Physical Path'in doğru olduğunu kontrol edin

## 7.2 DesktopConnector Çalışmıyor

### Hata: "Logo Tiger COM bileşeni bulunamadı"
**Çözüm**:
1. Logo Tiger'ın kurulu olduğu makinede çalıştırın
2. UnityObjects.dll'i kaydedin:
```
regsvr32 "C:\Logo\Tiger3\UnityObjects.dll"
```
3. 64-bit sorun olursa 32-bit olarak çalıştırın:
```
C:\Windows\SysWOW64\regsvr32.exe "C:\Logo\Tiger3\UnityObjects.dll"
```

### Hata: "Logo login başarısız"
**Çözüm**:
1. Logo Tiger'ı açın ve manuel giriş yapabildiğinizi doğrulayın
2. appsettings.json'daki UserName/Password'u kontrol edin
3. FirmNr ve PeriodNr'ın doğru olduğunu kontrol edin

### Hata: "Veritabanına bağlanılamadı"
**Çözüm**: Connection string'i kontrol edin — sunucudan SQL Server'a erişim olmalı

## 7.3 Aktarım Çalışmıyor

### Belirti: Web'de "Aktar" basıyorum ama statü değişmiyor
**Sebep**: DesktopConnector çalışmıyor
**Çözüm**:
1. DesktopConnector'ın çalıştığından emin olun
2. Console modunda çalıştırıp logları izleyin
3. ErpTransferItems tablosunda Status="Pending" kayıtlar var mı kontrol edin

### Belirti: Aktarım "Failed" oluyor
**Sebep**: Logo Tiger bağlantı veya veri sorunu
**Çözüm**:
1. SystemLog sayfasında hata mesajını okuyun
2. DesktopConnector loglarını kontrol edin
3. GL kodu ve banka hesap kodunun Logo Tiger'da mevcut olduğunu doğrulayın

---

# BÖLÜM 8: GÜVENLİK AYARLARI (Production)

## 8.1 Yapılması Gerekenler

1. **HTTPS aktif edin**: IIS'de SSL sertifikası bağlayın
2. **DevTools controller'ı kapatın**: Production'da `/dev/hash` endpoint'i kapatılmalı
3. **Varsayılan şifreleri değiştirin**: admin, system, muhasebe kullanıcılarının şifrelerini değiştirin
4. **SQL Server güvenliği**: Trusted_Connection yerine ayrı bir SQL kullanıcısı oluşturun
5. **Firewall**: Sadece gerekli portları açın (8080 veya 443)

---

# BÖLÜM 9: HIZLI REFERANS KARTLARI

## 9.1 Kullanıcı Bilgileri

| Kullanıcı | Şifre | Rol | Erişim |
|-----------|-------|-----|--------|
| admin | Admin123! | Sistem Yöneticisi | Tüm menüler |
| system | System123! | Firma Yöneticisi | Dashboard, Banka, Transfer, Raporlar |
| muhasebe | Muhasebe123! | Muhasebeci | Dashboard, Banka, Transfer, Tanımlar, Raporlar, Loglar |

## 9.2 Port ve Adresler

| Servis | Adres | Port |
|--------|-------|------|
| Web Uygulaması | http://localhost:8080 | 8080 |
| SQL Server | DESKTOP-54QF28R\ZRV2014EXP | 1433 |
| DesktopConnector | Windows Service | - |
| WorkerService | Windows Service | - |

## 9.3 Dosya Konumları

| Bileşen | Konum |
|---------|-------|
| Web Publish | C:\Publish\FinansWeb |
| Connector Publish | C:\Publish\FinansConnector |
| Worker Publish | C:\Publish\FinansWorker |
| Connector Logları | C:\Publish\FinansConnector\logs\ |
| IIS Logları | C:\inetpub\logs\LogFiles\ |
| Web stdout Logları | C:\Publish\FinansWeb\logs\ |

## 9.4 Service Komutları

```cmd
:: DesktopConnector
sc create FinansConnector binpath="C:\Publish\FinansConnector\Finans.DesktopConnector.exe" start=auto displayname="Finans Desktop Connector"
sc start FinansConnector
sc stop FinansConnector
sc delete FinansConnector

:: WorkerService
sc create FinansWorker binpath="C:\Publish\FinansWorker\Finans.WorkerService.exe" start=auto displayname="Finans Worker Service"
sc start FinansWorker
sc stop FinansWorker
sc delete FinansWorker
```

## 9.5 Migration Komutları

```powershell
# Package Manager Console (Visual Studio)
Add-Migration MigrationAdi -StartupProject Finans.WebMvc
Update-Database -StartupProject Finans.WebMvc

# Komut Satırı
dotnet ef migrations add MigrationAdi --project Finans.Data --startup-project Finans.WebMvc
dotnet ef database update --project Finans.Data --startup-project Finans.WebMvc
```
