# Restoran Yönetim Oyunu — Proje Dokümanları

Mobil (iOS/Android) ve ileride Steam. 2.5D, yumuşak low-poly. Unity. Tek kişilik ve yapay zeka destekli üretim.

**Konsept:** Sen aşçı değil patronsun. Menüyü, fiyatı, tedariki ve ekibi sen kurarsın; servis sırasında sadece krizlere müdahale edersin; her hafta kira gününü karşılamak zorundasın.

**Yaklaşım:** Plan tamamlanmadan uygulamaya geçilmeyecek. Planın mevcut durumu için [06-plan-status.md](06-plan-status.md).

## Konuya göre

Dosyalar **kronolojik**: her numara bir işin kaydı. Aşağıdaki gruplar
yalnızca ikinci bir kapı — numaralandırma ve sıra değişmiyor.

**Buradan başla** — [06](06-plan-status.md) planın ve işin durumu · [02](02-design-proposal.md) oyunun ne olduğu · [04](04-architecture.md) katmanlar ve port sınırı · [23](23-core-contract.md) çekirdeğin değişmezleri

**Tasarım** — [07](07-cuisine-system.md) iki mutfak, imza mekanikleri · [08](08-endgame.md) altmışıncı gün ve yedi eksen · [09](09-content-inventory.md) 32 yemek · [11](11-customer-system.md) müşteri arketipleri · [34](34-progression-and-locks.md) kilit, mevsim, soran müşteri · [47](47-recognition-and-report-card.md) nişanlar ve haftalık karne · [53](53-pending-decisions.md) bulaşıkçı, kombo, personelin sesi

**Ekonomi ve denge** — [12](12-economy.md) sayılar, kira, maaş, formüller · [27](27-time-model.md) gün ve tick · [28](28-peak-decision.md) zirvenin şekli · [29](29-phase0-simulation.md) simülasyon ve denge aracı · [32](32-equipment-and-rebalance.md) istasyon yuvaları · [42](42-crew-and-intervention.md) kadro tavanı, müdahale bütçesi · [48](48-day-sharpness.md) günü sivriltmek · [52](52-split-and-rent.md) mutfak ayrımı ve kira

**Çekirdek ve veri** — [03](03-technical-decisions.md) motor, platform, sanat tarzı · [13](13-data-schemas.md) JSON şemaları · [15](15-save-system.md) dört yuva, sürüm göçü · [19](19-technical-setup.md) proje kurulumu · [14](14-staff-system.md) roller, huylar, moral · [39](39-plate-cycle.md) sayılı tabak, bulaşık nöbeti · [51](51-self-service.md) fast food'un servis modeli · [33](33-second-cuisine.md) Türk mutfağı

**Arayüz, sanat ve mekân** — [16](16-screens-and-tutorial.md) ekran akışı ve ilk on dakika · [41](41-ui-and-venue.md) kartlı HUD, renk rolleri · [24](24-art-pipeline.md) model hattı · [10](10-cuisine-identity.md) giydirme ve ortam kimliği · [30](30-venue-layout.md) kat planı · [31](31-rooms-and-camera.md) oda görünümü, kamera, URP · [35](35-animation-and-camera.md) canlandırma katmanı · [36](36-street-time-and-kitchen.md) sokak ve günün saati · [38](38-street-and-interior-light.md) gece ışığı · [50](50-kitchen-texture.md) doku üretimi · [17](17-audio-design.md) ses katmanları

**Metin ve dil** — [18](18-story-and-text.md) hikâye yayları ve kelime bütçesi · [40](40-two-languages.md) tek üreteçten iki dil · [54](54-five-languages.md) beş dil, Arapça birleştirme, aynalama · [55](55-translation-review.md) dört agentli çeviri denetimi

**İnceleme ve ölçüm** — [22](22-answers-and-direction.md) sorular ve yön · [37](37-four-agent-review.md) dört agentli inceleme · [43](43-review-and-measurement.md) ölçümün yanılttığı yerler · [45](45-design-review.md) beş agentli tasarım turu · [49](49-unreachable-mechanics.md) oyuncuya ulaşmayan mekanikler · [46](46-shipped-binary.md) gönderilen ikili, link.xml

**Yayın** — [05](05-production-plan.md) araçlar, lisanslar, maliyet · [20](20-production-decisions.md) üretim kararları · [21](21-business-and-release.md) yayın denetimi ve kalan işler · [44](44-store-texts.md) mağaza metinleri, gizlilik politikası · [25](25-game-name.md) ad kararı — AÇIK

## Dokümanlar

| Dosya | İçerik | Durum |
|---|---|---|
| [research/01-market-research.md](research/01-market-research.md) | On oyunun incelenmesi, sevilen ve nefret edilen mekanikler, pazar verisi | Tamamlandı |
| [02-design-proposal.md](02-design-proposal.md) | Oyun mekaniği, günün döngüsü, batma merdiveni, yol haritası | v0.1 |
| [03-technical-decisions.md](03-technical-decisions.md) | Motor, ekip, sanat tarzı ve platform kararları | Güncel |
| [04-architecture.md](04-architecture.md) | Katmanlı mimari, port sınırı, çekirdeğin motordan bağımsızlığı | Güncel |
| [05-production-plan.md](05-production-plan.md) | Ne, ne ile yapılacak. Araçlar, lisanslar, maliyetler, yetenek sınırı | Güncel |
| [06-plan-status.md](06-plan-status.md) | **37 plan kaleminin durumu ve kalan işin beş partisi** | Güncel |
| [07-cuisine-system.md](07-cuisine-system.md) | Mutfak seçimi, imza mekanikleri, gelir modeli ve üç şart | Güncel |
| [08-endgame.md](08-endgame.md) | Oyunun sonu: puanlanan yıl sonu değerlendirmesi | Onaylandı |
| [09-content-inventory.md](09-content-inventory.md) | İçerik envanteri (v2, 32 yemek), paylaşılan taban ve altmış günlük ilerleme eğrisi | Güncel |
| [10-cuisine-identity.md](10-cuisine-identity.md) | Karakter giydirme sistemi, mutfağa özel ortam ve karakter sayımı | Güncel |
| [11-customer-system.md](11-customer-system.md) | Müşteri arketipleri (v2, 20 adet), sıklık kademeleri ve sekiz parametre | Güncel |
| [12-economy.md](12-economy.md) | Ekonomi sayıları, kira, maaş, marj, kredi ve müşteri formülleri | Güncel |
| [13-data-schemas.md](13-data-schemas.md) | On bir JSON şeması, dosya düzeni ve açılış doğrulaması | Güncel |
| [14-staff-system.md](14-staff-system.md) | Dört rol, on iki huy, moral, deneyim ve işe alım | Güncel |
| [15-save-system.md](15-save-system.md) | Dört yuva, bozulma koruması, sürüm göçü, bulut kaydı | Güncel |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | On sekiz ekran, akış kuralları ve ilk on dakika planı | Güncel |
| [17-audio-design.md](17-audio-design.md) | Beş ses katmanı, katmanlı servis müziği, üretim yükü | Güncel |
| [18-story-and-text.md](18-story-and-text.md) | Hikaye yayları, şablonlu yorumlar, kelime bütçesi, yerelleştirme | Güncel |
| [19-technical-setup.md](19-technical-setup.md) | Unity sürümü, URP ayarları, performans bütçeleri, kayıt formatı, girdi haritası | Güncel |
| [20-production-decisions.md](20-production-decisions.md) | Model yolu, karakter hattı, ses kaynağı, yazı tipi, test planı | Güncel |
| [21-business-and-release.md](21-business-and-release.md) | Fiyat, lansman aşamaları, ölçülecek metrikler, hukuki zorunlular | Güncel |
| **[review/00-synthesis.md](review/00-synthesis.md)** | **Beş ajanlı değerlendirmenin sentezi: 3 onay, 21 düzelt, yedi partilik düzeltme planı, dokuz açık soru** | **Güncel** |
| [review/01-05](review/) | Beş ham değerlendirme raporu: tasarım, mimari, kapsam, pazar, oyuncu deneyimi | Güncel |
| **[22-answers-and-direction.md](22-answers-and-direction.md)** | **Dokuz soruya cevaplar, makine taraması, sıfır bütçe planı, açık iki karar için öneri** | **Güncel** |
| **[23-core-contract.md](23-core-contract.md)** | **Parti B: determinizm, tamsayı durum, RNG, kültür, komut günlüğü, şema eklemeleri, kabul ölçütleri. Bağlayıcı** | **Bağlayıcı** |
| **[24-art-pipeline.md](24-art-pipeline.md)** | **Render döngüsü, üç kademe, karakter planı, modüler tabaklama, zaman kutuları** | **Güncel** |
| [25-game-name.md](25-game-name.md) | On ad adayı, mağaza çarpışma taraması, öneri | Karar bekliyor |
| `../tools/balance/` | Denge modeli: `model.py` formüller, `solve.py` parametre arama, `render.py` dokümana yazma | Çalışıyor |
| `../tools/art/` | Blender üretim betikleri; `gen_table.py` doğrulama örneği | Çalışıyor |
| `../src/Lokanta.Core/` | Saf C# çekirdek: `Fx` tamsayı aritmetiği, `Rng`, talep ve kadro modelleri. Unity referansı yok | Faz 0, 1. dilim |
| `../src/Lokanta.Content/` | JSON yükleme ve doğrulama. Newtonsoft yalnızca burada | Faz 0, 1. dilim |
| `../tests/Lokanta.Core.Tests/` | 225 test: aritmetik, RNG çapraz doğrulama, kültür, kayan nokta yasağı, altın tablo, simülasyon | Hepsi geçiyor |
| **[29-phase0-simulation.md](29-phase0-simulation.md)** | **Faz 0 ikinci dilim: simülasyon, denge aracı ve simülasyonun bulduğu altı tasarım hatası** | **Güncel** |
| [27-time-model.md](27-time-model.md) | Servis günü, görev süreleri, hazırlama süresi türetmesi, eş zamanlılık, zirve doluluğu | Güncel |
| [28-peak-decision.md](28-peak-decision.md) | Mutfak saat dağılımı ile kapasite çelişkisinin kararı | Güncel |
| [30-venue-layout.md](30-venue-layout.md) | Arsa, oda ızgarası, kademe açılışı ve mobilya yerleşimi | Güncel |
| **[31-rooms-and-camera.md](31-rooms-and-camera.md)** | **İki kademeli kamera, ölçülmüş dokunma hedefi, mobilya/karakter yön kuralı** | **Güncel** |
| [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md) | İstasyon slotları, `attendBp` ve gerçekleşme oranının kalibrasyonu | Güncel |
| [33-second-cuisine.md](33-second-cuisine.md) | Türk mutfağı: yemek grupları içerikten gelir, üç yerde doğrulanır | Güncel |
| [34-progression-and-locks.md](34-progression-and-locks.md) | İlerleme kilidi, karmaşıklık, mevsim ve beş ölü içeriğin canlandırılması | Güncel |
| **[35-animation-and-camera.md](35-animation-and-camera.md)** | **Yürüyüş katmanı, çalışan fırın/ocak, iki parmak yakınlaştırma ve kare masa** | **Güncel** |
| **[36-street-time-and-kitchen.md](36-street-time-and-kitchen.md)** | **Odalar arası kapılar, sokak ve yoldan geçenler, günün saati, mutfağın aşamaları, tepsi** | **Güncel** |
| **[37-four-agent-review.md](37-four-agent-review.md)** | **Dört agent incelemesi: fiyat tavanı, kombo açığı, sabır donması, yayın kapıları, kriz şeridi** | **Güncel** |
| **[38-street-and-interior-light.md](38-street-and-interior-light.md)** | **Sokak lambalarının düzeni ve klasik fener modeli, gece ışığının üç katmanı, yaya çarpışması ve figür profili, iç tavan aydınlatması, kanatların kaldırılması** | **Güncel** |
| **[39-plate-cycle.md](39-plate-cycle.md)** | **Devralınan kadro (aşçı + garson), sayılı tabak döngüsü, bulaşık nöbeti, ölüm sarmalını kıran oturtma eşiği** | **Güncel** |
| **[40-two-languages.md](40-two-languages.md)** | **İngilizce yerelleştirme: tek üreteçten iki tablo, çeviri politikası, cihaz diline göre varsayılan, turun bulduğu iki hata** | **Güncel** |
| **[41-ui-and-venue.md](41-ui-and-venue.md)** | **Oyun ekranının referansa göre yeniden tasarımı (kartlar, kapsüller, üç renk rolü), prosedürel süsleme ve mutfak kimliği** | **Güncel** |
| **[42-crew-and-intervention.md](42-crew-and-intervention.md)** | **Pasif oyuncu artık batıyor; müdahale baskı altında kazandırıyor; kadro tavsiyesinin adı yanlıştı** | **Güncel** |
| **[43-review-and-measurement.md](43-review-and-measurement.md)** | **Beş agentli inceleme: turun 107 kontrolü hiçbir şeyi kıramıyordu; vakumda yeşil kalan kontroller; ölçümün kendi kaynağını tüketmesi** | **Güncel** |
| **[44-store-texts.md](44-store-texts.md)** | **Mağaza kısa/tam açıklaması (iki dil), gizlilik politikası metni ve mağaza çözünürlüğünde ekran görüntüsü üretimi** | **Güncel** |
| **[45-design-review.md](45-design-review.md)** | **Beş agentli tasarım turu: fiyatın talebe kanalı yoktu (tavanda zam bedavaydı), müdahale salona da bağlandı, kombo ekseninin doygunluğu semptom çıktı** | **Güncel** |
| **[46-shipped-binary.md](46-shipped-binary.md)** | **Emülatör neden işe yaramıyor (ARM64-only APK) ve `link.xml`: hiç koşturulmamış budama koruması mutasyonla ölçüldü — kaldırılınca oyun açılışta ölüyor** | **Güncel** |
| **[47-recognition-and-report-card.md](47-recognition-and-report-card.md)** | **Günlük görev değil TANIMA: haftalık karne (tek başarı anı → dokuz) ve yedi nişan; birim hatası nişanı birinci günde dağıtıyordu** | **Güncel** |
| **[48-day-sharpness.md](48-day-sharpness.md)** | **Baskı neden hiç ateşlenmiyordu: günün en yoğun anı ortalamanın 1,24 katıydı; süre sivriltildi, müdahalenin değeri +505 → +4.953** | **Güncel** |
| **[49-unreachable-mechanics.md](49-unreachable-mechanics.md)** | **Yirmi komutun dort eksende taranmasi: malzeme kalitesi hicbir ekranda yoktu, "kovala" dugmesine kimse basmiyordu, erken tahsilat tuzak cikti** | **Güncel** |
| **[50-kitchen-texture.md](50-kitchen-texture.md)** | **Mutfaga gore duvar yuzeyi (ahsap lambri / celik bant), zemin ve disarinin tonu; tur artik fast food da oynuyor ve kirpilma olcumu var olmayan bir hatayi ariyordu** | **Güncel** |
| **[51-self-service.md](51-self-service.md)** | **Fast food self servis: garson yerine temizlikçi, salon rolleri mutfağa bağlandı, hacim +%43 fiş −%22; hacim toplam kasa için bir kol değil** | **Güncel** |
| **[52-split-and-rent.md](52-split-and-rent.md)** | **Fast food kirası ×1,15 (kirayı artırmak botun kasasını *artırıyor* — ölçüldü); görüntü aracı self servisi hiç çizmiyordu, asılı menü panelleri arkasını kapatıyordu, mobilya laminata döndü** | **Güncel** |
| **[53-pending-decisions.md](53-pending-decisions.md)** | **Bulaşıkçı açık kararı ölçümle kapandı (iki "düzeltme" denendi, ikisi de bozdu); kombo cevabı self servisle tersine döndü ve düğmesine tur hiç basmıyormuş; personele ilk kez ses verildi (`trait.*.voice`)** | **Güncel** |
| **[54-five-languages.md](54-five-languages.md)** | **Beş dil (tr/en/es/zh/ar), varsayılan İngilizce; Arapça harf birleşmesi Gelişmiş Metin Üreticisi'yle çözüldü ve genişlik farkıyla ölçüldü (60 → 40 dp); yerleşim aynalandı; şerit bütçesi beş dilde ölçülüyor — en uzunu İspanyolca** | **Güncel** |
| **[55-translation-review.md](55-translation-review.md)** | **Dört agentli çeviri denetimi: müdavim kadrosu es/zh/ar'da başka bir kadroydu; Çince yapı üç yerden bozukken yazı tipi denetimi yeşildi (isim havuzu + gömülü simgeler); yedi metin simülasyonu yalanlıyordu** | **Güncel** |
| `../unity/` | Unity 6.3 LTS projesi, Android hedefli. Ayarlar `ProjectSetup.cs` ile kodla uygulanıyor | Kuruldu |
| `../src/Lokanta.Harness/` | Denge aracı: beş strateji, çok tohumlu altmış günlük kampanya | Çalışıyor |

**Durum sütunu 16 Eylül 2026'da düzeltildi.** On üç satırda hâlâ "Karar
bekliyor" yazıyordu; bu belgenin *kendi* alt bölümü ("Plan kütüğü — kapandı")
aynı kalemlerin 9 Eylül'de kapandığını ve uygulamanın ondan sonra başladığını
söylüyor. Yani tablo, altındaki bölümü yalanlıyordu. Tek istisna
[25-game-name.md](25-game-name.md): ad ve marka taraması gerçekten açık.

*Sütun belgenin durumunu söylüyor, işin durumunu değil. Yayın öncesi kalan
işler [21-business-and-release.md](21-business-and-release.md)'de.*

## Yayınlanmış okunabilir sürümler

- Araştırma ve tasarım önerisi: https://claude.ai/code/artifact/409721e3-d115-4985-a498-f54b8720e47e
- Sanat tarzı karşılaştırması: https://claude.ai/code/artifact/0e98e412-5ec7-4c99-95a8-d532384f161e
- Katmanlı mimari: https://claude.ai/code/artifact/521351fd-1618-4372-a271-c9e72484bc42
- Üretim planı ve eksikler kütüğü: https://claude.ai/code/artifact/e338ccdd-44d2-4547-ac96-1134f43fb943
- Mutfak sistemi ve gelir modeli: https://claude.ai/code/artifact/a1ae2487-ed9b-4bf0-bfa4-32d05df221af
- Oyunun sonu: https://claude.ai/code/artifact/23501b45-3584-406c-a7a6-10fb0760b760
- İçerik envanteri ve ilerleme: https://claude.ai/code/artifact/c1201234-32b7-4cde-8342-4d3715d166c0
- Mutfak kimliği ve karakter sistemi: https://claude.ai/code/artifact/ee1d0bfa-4654-4087-ac42-cffc9ca6fa1b
- Müşteri arketipleri: https://claude.ai/code/artifact/51069155-7773-40aa-ab89-5bcf0fa8b63f
- Ekonomi ve formüller: https://claude.ai/code/artifact/9cbe26ce-ddc4-4626-95a4-087d941eb13d
- Sikke ikonu adayları: https://claude.ai/code/artifact/a90e6a88-de82-429b-b03c-f5552451a8f1
- Personel sistemi: https://claude.ai/code/artifact/258d1c68-2b67-45b6-a3c7-922236b97004
- Oyuncu deneyimi (öğretici, ses, metin): https://claude.ai/code/artifact/7971d53e-7ba6-4c70-b7ee-7545d94a6990
- **Plan durumu ve gözden geçirme listesi: https://claude.ai/code/artifact/f4a38388-fa0f-44ac-b36e-fa3d65ce6a6b**
- **Beş ajanlı değerlendirme sentezi: https://claude.ai/code/artifact/27dd1ec2-42d8-4b52-aeb1-88b7fe1f7901**

## Plan tamamlanma durumu

| Alan | Karar verildi | Karar bekliyor | Yazılmadı |
|---|---|---|---|
| A. Tasarım | 5 | 11 | 0 |
| B. Teknik | 2 | 5 | 0 |
| C. Üretim | 3 | 5 | 0 |
| D. İş | 3 | 3 | 0 |
| **Toplam** | **13** | **24** | **0** |

Ayrıntı ve her kalemin açıklaması için [06-plan-status.md](06-plan-status.md).

## Verilmiş kararlar

| Konu | Karar |
|---|---|
| Motor | Unity |
| Ekip ve üretim yöntemi | Tek kişi, yapay zeka destekli |
| Sanat tarzı | Yumuşak low-poly, portreler pixel olabilir |
| Platform | Mobil önce, Steam ileride, katmanlı planlama |
| Mimari | Çekirdek saf C#, platform portların arkasında |
| Doku | Palet atlası veya vertex color |
| Sürüm kontrolü | Git artı Git LFS |
| Yerelleştirme | Türkçe ve İngilizce, metin dosyalarda |
| Gelir modeli | Mutfak içerik satın alması, güç satılmıyor |
| Kapsam | Mekân genişletme dahil, ikinci şube hariç |
| Mutfaklar | Fast food ücretsiz, Türk ücretli, İtalyan ve Japon güncelleme |
| Oyunun sonu | 60. günde puanlanan değerlendirme, sonrası serbest oyun |
| Para birimi | İsimsiz düz sikke yığını ikonu artı sayı. Gerçek para sembolü yok |

## Nerede duruyoruz — 13 Eylül 2026

**Oyun uçtan uca oynanıyor.** Ana menü, mutfak seçimi, dört kayıt yuvası,
altmış günlük kampanya, yıl sonu değerlendirmesi ve serbest oyun; iki mutfak,
iki dil, canlandırma katmanı, gündüz-gece ışığı ve Android paketi.

| ölçüt | durum |
|---|---|
| Çekirdek testleri | 225, hepsi geçiyor |
| `tools/check.py` | 13 denetim, hepsi temiz |
| Duman turu (gerçek Windows yapısı, 873×393 dp) | 114 kontrol, 0 hata |
| Denge aracı | 20 strateji × 60 gün, mutabakat sıfır |
| Android APK | 90,4 MB, 0 uyarı |

*Bu satırlar elle yazılmıyor: sayılar `dotnet test`, `tools/check.py`,
`tools/unity/tour.ps1` ve `tools/balance` çıktılarından geliyor.*

### Yayına ne kaldı

Teknik taraf hazır ([21](21-business-and-release.md) yayın denetimi paketin içinden
doğruladı: izin yok, veri toplanmıyor, 64 bit, 16 KB hizalı, lisanslar
derlemede). Kalanların çoğu **kullanıcının kararı** — parola, hesap, yasal form:

1. Yükleme anahtarı (keystore) üret ve **iki ayrı yerde yedekle**
2. Ad kararı + marka taraması — paket adı ilk yüklemede kilitleniyor
3. Play Console hesabı, ardından 14 günlük kapalı test
4. Gizlilik politikası URL'si — metin hazır, [44](44-store-texts.md)
5. Veri Güvenliği formu — cevap hazır: "veri toplanmıyor"
6. IARC yaş derecelendirmesi
7. **Para modeli.** Satın alma kodu yok; oyun bugün ücretsiz ve iki mutfak
   açık olarak yayınlanabilir. Ücretsizden ücretliye geçiş imkânsız, yani
   geri dönüşsüz bir karar
8. Öne çıkan görsel (1024×500). Ekran görüntüleri otomatik:
   `tools\unity\tour.ps1 -Magaza`

### İlk güncellemeden önce bilinmesi gereken — **kapandı**

`Simulation.SaveVersion` artarsa her oyuncunun altmış günlük kampanyasının
gideceği yazıyordu. **Göç yolu yazıldı ve koştuğu kanıtlandı**
([53](53-pending-decisions.md) §8):

- `Restore` artık `MinReadableVersion`–`SaveVersion` aralığını kabul ediyor.
- Bir sürümde eklenen alanlar `if (version >= N)` kapısıyla okunuyor; daha
  eski kayıtta atlanıp varsayılanda bırakılıyorlar.
- `SaveTests.Eski_surum_kaydi_aciliyor` gerçek bir 21. sürüm kaydını alıp
  21'de eklenen alanları siliyor, sürümü 20 yapıyor ve yüklüyor.
  `Cok_eski_surum_reddediliyor` kapının hâlâ bir kapı olduğunu söylüyor.

**Sonraki sürüm için:** alanları `if (version >= N)` ile oku, `SaveVersion`
listesine bir satır yaz, teste bir kol ekle. Dosyanın eski kuralı (*"yeni
alanlar `Has()` ile okunur"*) **geçersiz** — 126 okumanın ikisinde
uygulanmıştı ve yanlış araçtı: `Has()` bir alanın yokluğunu her zaman meşru
sayar, yani bozuk kayıtla eski kaydı ayırt edemez.

### Açık denge soruları

1. Soft launch pazarı hangisi olsun
2. Mutfak fiyatı 4,99 bandı doğru mu (7. maddeye bağlı)
3. Analitik hiç olmasın mı, yoksa asgari mi olsun

---

## Plan kütüğü — kapandı

Otuz yedi kalem 9 Eylül 2026'da yazıldı, hepsi kapandı ve uygulama ondan sonra
başladı. *"Kütüğün tamamı kapanmadan uygulamaya geçilmeyecek"* anlaşması
tutuldu; bu bölüm tarihsel kayıt olarak duruyor.

1. ~~Kapsam ve tema kararı~~
2. ~~Oyunun sonu~~ — puanlanan yıl sonu değerlendirmesi
3. ~~İçerik envanteri~~ — 32 yemek
4. ~~Mutfak kimliği~~ — karakter ve ortam sistemi
5. ~~Müşteri arketipleri~~
6. ~~Ekonomi sayıları ve müşteri formülleri~~ — dengelendi, bkz. [42](42-crew-and-intervention.md)
7. ~~Parti 1~~ — veri şemaları, personel sistemi, kayıt sistemi
8. ~~Parti 2~~ — ekranlar, öğretici, ses tasarımı, hikaye metinleri
9. ~~Parti 3, 4 ve 5~~ — teknik kurulum, üretim kararları, iş ve yayın
