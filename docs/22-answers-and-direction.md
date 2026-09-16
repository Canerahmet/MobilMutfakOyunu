# Cevaplar ve Yön

**Son güncelleme:** 10 Eylül 2026
**Amaç:** Değerlendirmenin dokuz sorusuna verilen cevapları, cevapların planda neyi değiştirdiğini ve hâlâ açık olanları tek yerde tutmak.
**Kaynak:** [review/00-synthesis.md](review/00-synthesis.md) §9

---

## 1. Cevaplar

| # | Soru | Cevap | Ne değişti | Nerede |
|---|---|---|---|---|
| 1 | Tam zamanlı mı, akşamları mı | Claude Code uzaktan kontrolle, yapay zeka çalışır | İş eşzamansız ve partiler hâlinde; takvim çarpanı belirsiz. Aşağıda §3 | [05-production-plan.md](05-production-plan.md) |
| 2 | Mac var mı | **Yok.** İlk sürüm Android | iOS ilk sürümden çıktı. Apple 99 dolar ödenmiyor. Metal ayarları ertelendi | [21-business-and-release.md](21-business-and-release.md), [20-production-decisions.md](20-production-decisions.md), [19-technical-setup.md](19-technical-setup.md) |
| 3 | Blender biliyor musun | **Hayır**, yapay zekaya devrediyor | Sanat hattı yeniden tasarlandı: başsız render döngüsü, bugün doğrulandı | [24-art-pipeline.md](24-art-pipeline.md) |
| 4 | Bütçe sıfır mı | **Şimdilik sıfır**; yayın ücretleri gerektiğinde ödenecek | İlk yıl maliyeti 124 dolardan 25 dolara indi. Ücretli test edinimi yok; soft launch organik | §4 aşağıda |
| 5 | Oyunun adı | **Belli değil** | On aday tarandı, öneri hazır | [25-game-name.md](25-game-name.md) |
| 6 | Yemek 20 mi 32 mi | **32**, çeşitlilik taraftarı | Tasarımcının şartı bağlayıcı: her yemek dört parametreli. Sanat maliyeti modüler tabaklamayla sabit | [23-core-contract.md](23-core-contract.md) §8.3, [24-art-pipeline.md](24-art-pipeline.md) |
| 7 | Gün sonu ödüllü reklam | **İlk sürümde yok.** Oyun tamamen bitip yayına çıkmadan önceki son adımda eklenir; sıklığına ve ödülüne o zaman karar verilir | Karar 10 Eylül 2026. Analiz §5'te not olarak duruyor | [21-business-and-release.md](21-business-and-release.md) |
| 8 | Steam öne alınsın mı | **Hayır.** Sayfa da sürüm de oyun bittikten sonraki aşama | Karar 10 Eylül 2026. Lansman tablosu güncellendi | [21-business-and-release.md](21-business-and-release.md) |
| 9 | Yatay mod | **Yatay** | Kesinleşti. Başparmak bölgesi düzeni ve sol el aynalama Parti D'de | [16-screens-and-tutorial.md](16-screens-and-tutorial.md) |

---

## 2. Makinede ne bulundu

Cevapların yanında makine de tarandı:

| Araç | Durum | Not |
|---|---|---|
| Blender | **5.2 LTS kurulu** | Başsız render doğrulandı, 4 saniyede üç açı |
| Unity | **6000.5.8f1 kurulu** | Bu Unity 6.5, LTS değil. Plan 6.3 LTS diyor. Aşağıda |
| Unity Hub | Kurulu | |
| Python | 3.13 | Denge aracı bununla çalışıyor |
| .NET SDK | Kurulu | Çekirdek testleri ve denge aracı için |
| Git | Kurulu | Proje henüz depo değil |
| Node | Yok | Gerekmiyor |
| Disk | C 305 GB, D 152 GB boş | Yeterli |

**Unity sürümü: karar verildi 9 Eylül 2026.** Hub'dan 6.3 LTS kurulacak, proje onunla açılacak. Kurulu 6.5 durabilir. Gerekçe [19-technical-setup.md](19-technical-setup.md): uzun ömürlü oyun deneysel sürümde tutulmaz.

---

## 3. Çalışma biçimi: uzaktan kontrol ne demek

Claude Code uzaktan kontrol, işi şöyle şekillendiriyor:

- **Eşzamansız partiler.** Sen bir hedef verirsin, ben çalışırım, sonuç PNG, tablo veya APK olarak önüne gelir. Sen makinenin başında olmak zorunda değilsin.
- **Ben komut satırında her şeyi yapabiliyorum:** Blender, Unity toplu derleme, testler, denge aracı. Bugün Blender'ı ben çalıştırdım.
- **Sen yalnızca üç şeyi yaparsın:** web araçları (Mixamo, mağaza konsolları), cihaza APK kurmak, beğeni kararı.

Takvim çarpanı hâlâ belirsiz: günde kaç saat "hedef ver, sonuca bak" yapabildiğin. Kapsam değerlendirmesi 13-14 kişi-ay dedi. Bunun kaçı takvim ayı, senin ritmine bağlı. Ne olursa olsun şu doğru: **ilk sürüm iki mutfakla çıkar**, fast food ve Türk. İtalyan ve Japon güncelleme olarak gelir. Bu zaten [12-economy.md](12-economy.md) §9'da yazılıydı; şimdi kesin.

---

## 4. Sıfır bütçe planı

| Kalem | Eski | Yeni | Ne zaman |
|---|---|---|---|
| Google Play geliştirici | 25 $ | 25 $ | Kapalı test başlarken |
| Apple geliştirici | 99 $/yıl | **0** | Mac olunca |
| Steamworks | 100 $ | 100 $ | Oyun bittikten sonra, Steam sayfası açılırken |
| Unity | 0 | 0 | Personal, 200 bin dolar altı |
| Blender, Python, .NET | 0 | 0 | |
| Varlıklar | 0 | 0 | CC0 ve prosedürel |
| Test edinimi | Yazılmamıştı | **0** | Gelir gelince |
| **İlk yıl, Android** | 124 $ | **25 $** | |

**Soft launch'ın anlamı değişti.** Yayıncı değerlendirmesi "300-500 dolar test edinimi olmadan soft launch veri üretmez" dedi; haklı, ücretli kurulum yok. Yeni tanım: **organik kapalı test.** Yirmi ile elli test oyuncusu; Türk bağımsız oyun toplulukları, r/tycoon, Discord'dan elle toplanır. Ölçülen şey kurulum maliyeti değil, D1 ve D7 tutundurma ve ekonomi telemetrisi ("kaçıncı günde para sorun olmaktan çıktı"). Bu veri, sıfır bütçeyle üretilebilir ve dengeyi ayarlamaya yeter. Dönüşüm oranı için ise gerçek yayın beklenir.

---

## 5. Reklam ve Steam: karar verildi, analiz not olarak duruyor

**10 Eylül 2026 kararı.** İkisi de ilk sürümde yok. Ödüllü reklam, oyun tamamen bitip yayına çıkmadan önceki son adımda eklenir ve biçimine o zaman karar verilir. Steam sayfası ve Steam sürümü oyun bittikten sonraki aşama. Aşağıdaki analiz o güne kadar not olarak kalıyor; o gün geldiğinde buradan devam edilir.

### Ödüllü reklam: **ilk sürümde yok, yayın öncesi son adımda eklenecek**

| Lehte | Aleyhte |
|---|---|
| Ödemeyen %98'den tek gelir | Reklam SDK'sı gizlilik yükü getirir: GDPR onay formu, Play veri güvenliği beyanı, çocuk hedefleme kontrolü. Tek kişiye bir hafta |
| Oyuncu basmadan çıkmaz | "Enerji yok, sayaç yok, reklam yok" mesajı araştırmanın en sevilen vaadi. Bir reklam bile bu cümleyi bozar |
| | Yayıncının kendi bulgusu: kitle Steam tarzı, reklamdan nefret ediyor |
| | Küçük kurulum tabanında getiri önemsiz: 10-15 dolar eCPM × günde birkaç yüz gösterim |

Yeniden bakma zamanı: oyun tamamen bittiğinde, yayından hemen önce. O gün karar verilecek iki şey ve bugünkü not:

**Sıklık.** Öneri gün sonu, günde en fazla bir, sadece oyuncu basınca, gün sonu raporu ekranında.

| Sıklık | Kampanya başına gösterim | Oyuncu başına getiri | Değerlendirme |
|---|---|---|---|
| Haftalık, kira günü | 8 | Sıfıra yakın | SDK'nın gizlilik yüküne değmez; "kirayı düşürmek için izle" biçimine kayar ve ekonominin çekirdek gerilimini satın alınabilir yapar |
| Günlük, gün sonu | 60 | 0,12 ile 1,20 dolar arası, bölgeye göre | 4,99 dolarlık mutfak satışının kurulum başına getirisiyle aynı büyüklükte |

**Ödül.** Para ödülü dengelenmiş ekonomiyi bozar:

| Ödül | 60 günde toplam | Oyun sonu kasasına oranı (11.630) |
|---|---|---|
| Günde 50 sikke | 3.000 | %26 |
| Günde 100 sikke | 6.000 | %52 |
| Günlük cironun %2'si | ~3.300 | %29 |

Bu yüzden öneri para olmayan ödül: **yarının hal fiyatlarını önceden görmek** (bilgi avantajı, kasaya sikke girmiyor) ya da bir personelin moralini artırmak. Para istenirse günlük cironun en fazla %1'i, ve `tools/balance/model.py` içine "her gün izleyen oyuncu" senaryosu eklenip on yedi test yeniden koşulur.

**Teknik.** Reklam sağlayıcısı `IAdProvider` portunun arkasında; Steam derlemesinde port "yok" döner, Steam oyuncusu reklam görmez. Android'de GDPR onay ekranı ve Play veri güvenliği formu yaklaşık bir haftalık iş.

### Steam sayfası: **oyun bittikten sonra** (Faz 1 sonu önerisi kabul edilmedi)

Yayıncı haklı: araştırmadaki on oyunun dokuzu Steam'den, sayaçtan nefret eden kitle orada. Ama derleme sırası değişmiyor, Android önce. Değişen pazarlama sırası:

| Adım | Ne zaman | Bedel |
|---|---|---|
| Steam sayfası açılır, istek listesi toplanır | Oyun bittikten sonra; en erken Android yayını sırasında, o zaman yeniden bakılır | 100 $ |
| Next Fest'e demo | İlk uygun tarih | 0 |
| Steam sürümü | Android yayından sonra, iki mutfakla, erken erişim 9,99 | 0 |

Sayfa ne demek: Steam, oyun oynanabilir olmadan "Yakında" mağaza sayfası açmaya izin veriyor; tek düğmesi istek listesine ekleme. Steam sürümü çıktığı gün listedeki herkese bildirim gidiyor ve ilk gün satışı Steam'in görünürlük algoritmasını besliyor. Gerekenler: Steamworks 100 dolar (ilk 1.000 dolar gelirden sonra iade), oyun adı, beş ekran görüntüsü ve kapak görseli (Blender ve Unity'den render, ben üretirim), isteğe bağlı kısa video, Valve incelemesi üç ile beş gün. Next Fest yılda üç kez, oyun başına bir katılım, demo gerekir. Karar: sayfa oyun bittikten sonra.

---

## 6. Planda bu cevaplarla ne değişti, özet

| Doküman | Değişiklik |
|---|---|
| [12-economy.md](12-economy.md) | Parti A: bütün tablolar `tools/balance` tarafından üretiliyor |
| [14-staff-system.md](14-staff-system.md) | Parti A: iki havuzlu kapasite modeli, tavan 3/5/8/12 |
| [23-core-contract.md](23-core-contract.md) | Parti B: yeni, bağlayıcı |
| [24-art-pipeline.md](24-art-pipeline.md) | Yeni: render döngüsü, üç kademe, modüler tabaklama |
| [25-game-name.md](25-game-name.md) | Yeni: on aday, öneri Last Seating, ikinci Lokanta |
| [05-production-plan.md](05-production-plan.md) | Maliyet 25 $, Android, uzaktan kontrol |
| [20-production-decisions.md](20-production-decisions.md) | Cihaz matrisinden iOS çıktı |
| [21-business-and-release.md](21-business-and-release.md) | Android tek platform, organik kapalı test, reklam ve Steam yayın sonrasına |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | Yatay kesin |
| [19-technical-setup.md](19-technical-setup.md) | Kurulu sürüm notu, 6.3 LTS önerisi |
| [09-content-inventory.md](09-content-inventory.md) | 32 kesin, dört parametre şartı |
| [06-plan-status.md](06-plan-status.md) | A4 ve A8 kapandı, Parti A ve B tamam |

---

## 7. Şimdi ne bekleniyor

Senden, sırayla:

1. ~~Unity 6.3 LTS'yi Hub'dan kur~~ **Kararlaştırıldı**, kurulacak
2. **Mixamo klipleri yedek, uygulama aşamasında, sen indirirsin.** Birincil kaynak Quaternius Universal Animation Library 1 ve 2 (CC0, indirilebilir paket, ben bağlarım); restoran hareketleri Blender'da prosedürel. Bkz. [24-art-pipeline.md](24-art-pipeline.md)
3. ~~Reklam ve Steam önerilerine itirazın varsa söyle~~ **Karar verildi 10 Eylül 2026:** ikisi de oyun bittikten sonra
4. **Ad** için [25-game-name.md](25-game-name.md) §9'daki marka taramasını yapmadan karar verme

Benden: Parti A ve B kapandı, Faz 0 açık. Sıradaki iş `Lokanta.Core` iskeleti, `Fx`, `Rng`, ilk şemalar ve C# çekirdeğin Python modeliyle eşleşme testi.
