# 33 — İkinci mutfak: Türk lokantası ilk kez koşturuldu

10 Eylül 2026. Faz 0'ın bütün denge çözümü, bütün testleri ve bütün ölçümleri **tek mutfakla** yapılmıştı: fast food. İkinci mutfak hiç çalıştırılmamıştı.

İlk koşuş sonucu:

| strateji | son kasa | servis | kapıda |
|---|---:|---:|---:|
| pasif | -7.271 | **0** | %100 |
| sadece_hal | -7.893 | **0** | %100 |
| makul | -7.434 | **0** | %100 |
| genişlemeyen | -7.434 | **0** | %100 |
| atılgan | -41.187 | **0** | %100 |
| plancı | -23.981 | **0** | %100 |
| yüksek_fiyat | -7.434 | **0** | %100 |
| fazla_kadro | -20.123 | **0** | %100 |

**Sekiz stratejinin hepsi sıfır müşteriyle battı.** Tek bir grup bile ağırlanmadı, tek bir kuruş ciro olmadı.

---

## 1. Sebep

Yemek grupları **mutfağa özel**. [13-data-schemas.md](13-data-schemas.md) bunu kasıtlı tasarlamış: örnek yemek `kuru_fasulye` ve grubu `sulu`, örnek arketipin tercihi `{ "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }`.

Simülasyon ise fast food'un sözlüğünü **sabit kodlamıştı**:

```csharp
private const string GroupMain = "ana";
private const string GroupSide = "yan";
private const string GroupDrink = "icecek";
```

Türk lokantasında hiç `ana` grubu yok. Gruplar: `sulu` (11), `corba` (4), `pilav` (5), `izgara` (5), `meze` (3), `tatli` (3), `icecek` (1).

Yani her müşteri kapıya geliyor, menüde bir ana yemek arıyor, bulamıyor ve dönüyor. Sonsuza kadar.

**Bu hata koddan bakarak görünmüyor.** İki dosya da kendi içinde doğru: yemek içeriği şemaya uyuyor, simülasyon derleniyor ve fast food'da kusursuz çalışıyor. Aradaki sözleşme yazılı değildi.

---

## 2. Düzeltme

`content/cuisines/<id>.json` artık **menü rollerini** taşıyor: hangi grup ana yemek, hangisi yan, hangisi içecek, hangisi tatlı yerine geçiyor.

| Mutfak | ana | yan | içecek | tatlı |
|---|---|---|---|---|
| fast food | `ana` | `yan` | `icecek` | `tatli` |
| Türk | `sulu`, `izgara` | `corba`, `pilav`, `meze` | `icecek` | `tatli` |

Çekirdek artık grup adı bilmiyor, **rol** biliyor. `ContentSet.MainGroups` içerikten geliyor.

### Doğrulama sert, ve iki yerde

Aynı hatanın sessizce dönmemesi için:

**Üretim aşamasında** (`tools/balance/export.py`) — yemek dosyasındaki her grup tam olarak bir role düşmeli, hiçbir grup iki role birden düşmemeli, olmayan bir gruba rol verilmemeli, ve ilk gün açık en az bir ana yemek olmalı.

**Yükleme aşamasında** (`ContentSetLoader`) — aynı dört kontrol, çünkü [23-core-contract.md](23-core-contract.md) §9.1 içerik geçersizse oyunun **açılmamasını** şart koşuyor. Sessiz varsayılan yok.

**Test aşamasında** (`tests/CuisineTests.cs`) — beş test, iki mutfak için ayrı ayrı koşuyor: her grup tam bir role düşüyor mu, ilk gün sipariş verilebiliyor mu, bir servis günü gerçekten müşteri ağırlıyor mu, **dört rolün de siparişi veriliyor mu**, dilim süreleri tick'e tam bölünüyor mu. **Yeni mutfak eklenince listeye eklenmeli.**

---

## 3. Düzeltmeden sonra

Aynı parametreler, aynı kiralar, aynı ekipman merdiveni — hiçbir şey mutfağa göre ayarlanmadı:

| strateji | son kasa | itibar | masa | servis | kayıp | ilk borç |
|---|---:|---:|---:|---:|---:|---:|
| pasif | -6.297 | 0,0 | 4 | 13 | 0 | **35** |
| sadece_hal | 5.094 | 74,3 | 4 | 1.008 | 59 | — |
| **makul** | **30.745** | 99,6 | **8,8** | 2.209 | 45 | — |
| genişlemeyen | 13.661 | 95,2 | 4 | 1.184 | 46 | — |
| atılgan | -66.054 | 0,0 | 14 | 247 | 14 | **7** |
| plancı | 19.331 | 100,0 | 14 | 3.104 | 32 | — |
| yüksek_fiyat | 12.468 | 0,8 | 4 | 545 | 4 | — |
| fazla_kadro | 4.466 | 93,8 | 4 | 1.190 | 48 | — |

**Bütün tasarım hedefleri Türk mutfağında da tutuyor:** pasif oyuncu 35. günde batıyor, pervasız genişleyen 7. günde, büyümek 2,25 kat ödüllendiriyor (13.661 → 30.745), plancı takvimi tutturuyor, yüksek fiyat ve fazla kadro cezalandırılıyor.

Bu önemli bir sonuç: **ekonomi tek mutfağa aşırı uydurulmamış.** Kiralar, kapasiteler ve ekipman fiyatları fast food ile çözülmüştü ve ikinci mutfakta ayar gerektirmeden çalışıyor.

### İki mutfak arasındaki farklar

| | fast food | Türk |
|---|---|---|
| Dilim tick | 960 / 1440 / 960 / 1440 | 576 / **2304** / 1200 / 720 |
| makul oyuncunun ulaştığı masa | 7,0 | **8,8** |
| makul oyuncunun son kasası | 33.251 | 30.745 |
| büyüme çarpanı | 2,41× | 2,25× |
| fazla kadro | **56. günde batıyor** | batmıyor, 4.466 ile bitiriyor |

Farkların hepsi zirvenin keskinliğinden çıkıyor. Türk lokantasının öğle dilimi günün %48'i; o pencerede fazla kadro **işe yarıyor**, fast food'un daha düz gününde ise sadece maaş oluyor. Bu, [10-cuisine-identity.md](10-cuisine-identity.md)'nin istediği "mutfaklar farklı oynansın" hedefinin ölçülmüş ilk kanıtı.

---

## 4. Aynı taramada çıkan ikinci ölü içerik: tatlı

Menü rolleri yazılınca `tatli` rolü de tanımlandı ve o anda görüldü ki **tatlı hiç sipariş edilmiyordu.**

`PickOrder` üç kalem seçiyordu: ana yemek kesin, %30 yan, %40 içecek. Tatlıya hiç bakmıyordu. Yani:

- fast food'da **6**, Türk lokantasında **3** tatlı yemeği hiç satılamıyordu
- [27-time-model.md](27-time-model.md) §3.3 zirve tablosu tatlıya 0,08 eş zamanlı tabak veriyor — o satır boşa çalışıyordu
- [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md)'de yazdığım **tatlı istasyonunun ekipman yükseltmesi** satın alınabiliyor ama hiçbir işe yaramıyordu

Dördüncü kalem eklendi: `dessertChanceBp = 1800`, içecekten düşük çünkü tatlı sonda gelir ve herkes almaz. Grup başına en fazla iş sayısı 3'ten **4**'e çıktı — dört kalem dört ayrı istasyona gidebiliyor.

Ölçülen etkisi (beş gün, iki mutfak):

| | ana | yan | içecek | tatlı |
|---|---:|---:|---:|---:|
| fast food | 70 | 12 | 45 | **6** |
| Türk | 62 | 15 | 21 | **6** |

Ciroya etkisi de görünür: `plancı` 16.880'den **20.720**'ye çıktı. `CuisineTests.Her_rolden_siparis_veriliyor` artık dört rolün de sipariş edildiğini her mutfak için ayrı doğruluyor.

Türk lokantasının içecek sayısının fast food'un yarısı olması tesadüf değil: **tek içecek var** (çay) ve stok onu sınırlıyor. Aşağıdaki açık madde tam olarak bunu çözecek.

---

## 5. Açık kalan: `orderPreference` hâlâ yazılmadı

[13-data-schemas.md](13-data-schemas.md) arketip başına bir sipariş tercihi tasarlamış:

```json
"orderPreference": { "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }
```

Simülasyon bunu okumuyor. Onun yerine sabit bir model kullanıyor: **ana yemek kesin, %30 yan, %40 içecek** ([12-economy.md](12-economy.md) `order` bloğu). Menü rolleri o modeli ikinci mutfakta çalışır hâle getirdi ama tasarlanan modeli uygulamadı.

Bunun bugün görünen bedeli: Türk lokantasında **tek bir içecek** var (çay) ve müşterilerin %40'ı onu alıyor. Arketip tercihi olsaydı esnaf komşu ayranı, öğrenci kolayı seçebilirdi.

İkinci bir bedel: `sulu` ile `izgara` aynı role düştüğü için eşit olasılıkla seçiliyorlar. Gerçek bir lokantada sulu yemek ağır basar; docs/13'ün örneği de %60 diyor.

**Bu bir sonraki dilimin işi.** Roller onu engellemiyor, tam tersine üstüne kurulacak zemin.

---

## Nasıl koşturulur

```powershell
dotnet run --project src/Lokanta.Harness -- --mutfak turk
dotnet run --project src/Lokanta.Harness -- --mutfak fastfood
```

**Denge değişikliği yapan her koşuda ikisi de çalıştırılmalı.** Bu belge, tek mutfakla ölçmenin neye mal olduğunun kaydı.
