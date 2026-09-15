# Restoran Yönetim Oyunu — Proje Dokümanları

Mobil (iOS/Android) ve ileride Steam. 2.5D, yumuşak low-poly. Unity. Tek kişilik ve yapay zeka destekli üretim.

**Konsept:** Sen aşçı değil patronsun. Menüyü, fiyatı, tedariki ve ekibi sen kurarsın; servis sırasında sadece krizlere müdahale edersin; her hafta kira gününü karşılamak zorundasın.

**Yaklaşım:** Plan tamamlanmadan uygulamaya geçilmeyecek. Planın mevcut durumu için [06-plan-durumu.md](06-plan-durumu.md).

## Dokümanlar

| Dosya | İçerik | Durum |
|---|---|---|
| [research/01-pazar-arastirmasi.md](research/01-pazar-arastirmasi.md) | On oyunun incelenmesi, sevilen ve nefret edilen mekanikler, pazar verisi | Tamamlandı |
| [02-tasarim-onerisi.md](02-tasarim-onerisi.md) | Oyun mekaniği, günün döngüsü, batma merdiveni, yol haritası | v0.1 |
| [03-teknik-kararlar.md](03-teknik-kararlar.md) | Motor, ekip, sanat tarzı ve platform kararları | Güncel |
| [04-mimari.md](04-mimari.md) | Katmanlı mimari, port sınırı, çekirdeğin motordan bağımsızlığı | Güncel |
| [05-uretim-plani.md](05-uretim-plani.md) | Ne, ne ile yapılacak. Araçlar, lisanslar, maliyetler, yetenek sınırı | Güncel |
| [06-plan-durumu.md](06-plan-durumu.md) | **37 plan kaleminin durumu ve kalan işin beş partisi** | Güncel |
| [07-mutfak-sistemi.md](07-mutfak-sistemi.md) | Mutfak seçimi, imza mekanikleri, gelir modeli ve üç şart | Güncel |
| [08-oyun-sonu.md](08-oyun-sonu.md) | Oyunun sonu: puanlanan yıl sonu değerlendirmesi | Onaylandı |
| [09-icerik-envanteri.md](09-icerik-envanteri.md) | İçerik envanteri (v2, 32 yemek), paylaşılan taban ve altmış günlük ilerleme eğrisi | Karar bekliyor |
| [10-mutfak-kimligi.md](10-mutfak-kimligi.md) | Karakter giydirme sistemi, mutfağa özel ortam ve karakter sayımı | Karar bekliyor |
| [11-musteri-sistemi.md](11-musteri-sistemi.md) | Müşteri arketipleri (v2, 20 adet), sıklık kademeleri ve sekiz parametre | Karar bekliyor |
| [12-ekonomi.md](12-ekonomi.md) | Ekonomi sayıları, kira, maaş, marj, kredi ve müşteri formülleri | Dengelenmedi |
| [13-veri-semalari.md](13-veri-semalari.md) | On bir JSON şeması, dosya düzeni ve açılış doğrulaması | Karar bekliyor |
| [14-personel-sistemi.md](14-personel-sistemi.md) | Dört rol, on iki huy, moral, deneyim ve işe alım | Karar bekliyor |
| [15-kayit-sistemi.md](15-kayit-sistemi.md) | Dört yuva, bozulma koruması, sürüm göçü, bulut kaydı | Karar bekliyor |
| [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md) | On sekiz ekran, akış kuralları ve ilk on dakika planı | Karar bekliyor |
| [17-ses-tasarimi.md](17-ses-tasarimi.md) | Beş ses katmanı, katmanlı servis müziği, üretim yükü | Karar bekliyor |
| [18-hikaye-ve-metin.md](18-hikaye-ve-metin.md) | Hikaye yayları, şablonlu yorumlar, kelime bütçesi, yerelleştirme | Karar bekliyor |
| [19-teknik-kurulum.md](19-teknik-kurulum.md) | Unity sürümü, URP ayarları, performans bütçeleri, kayıt formatı, girdi haritası | Karar bekliyor |
| [20-uretim-kararlari.md](20-uretim-kararlari.md) | Model yolu, karakter hattı, ses kaynağı, yazı tipi, test planı | Karar bekliyor |
| [21-is-ve-yayin.md](21-is-ve-yayin.md) | Fiyat, lansman aşamaları, ölçülecek metrikler, hukuki zorunlular | Karar bekliyor |
| **[review/00-sentez.md](review/00-sentez.md)** | **Beş ajanlı değerlendirmenin sentezi: 3 onay, 21 düzelt, yedi partilik düzeltme planı, dokuz açık soru** | **Güncel** |
| [review/01-05](review/) | Beş ham değerlendirme raporu: tasarım, mimari, kapsam, pazar, oyuncu deneyimi | Güncel |
| **[22-cevaplar-ve-yon.md](22-cevaplar-ve-yon.md)** | **Dokuz soruya cevaplar, makine taraması, sıfır bütçe planı, açık iki karar için öneri** | **Güncel** |
| **[23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md)** | **Parti B: determinizm, tamsayı durum, RNG, kültür, komut günlüğü, şema eklemeleri, kabul ölçütleri. Bağlayıcı** | **Bağlayıcı** |
| **[24-sanat-hatti.md](24-sanat-hatti.md)** | **Render döngüsü, üç kademe, karakter planı, modüler tabaklama, zaman kutuları** | **Güncel** |
| [25-oyun-adi.md](25-oyun-adi.md) | On ad adayı, mağaza çarpışma taraması, öneri | Karar bekliyor |
| `../tools/balance/` | Denge modeli: `model.py` formüller, `solve.py` parametre arama, `render.py` dokümana yazma | Çalışıyor |
| `../tools/art/` | Blender üretim betikleri; `gen_table.py` doğrulama örneği | Çalışıyor |
| `../src/Lokanta.Core/` | Saf C# çekirdek: `Fx` tamsayı aritmetiği, `Rng`, talep ve kadro modelleri. Unity referansı yok | Faz 0, 1. dilim |
| `../src/Lokanta.Content/` | JSON yükleme ve doğrulama. Newtonsoft yalnızca burada | Faz 0, 1. dilim |
| `../tests/Lokanta.Core.Tests/` | 225 test: aritmetik, RNG çapraz doğrulama, kültür, kayan nokta yasağı, altın tablo, simülasyon | Hepsi geçiyor |
| **[29-faz0-simulasyon.md](29-faz0-simulasyon.md)** | **Faz 0 ikinci dilim: simülasyon, denge aracı ve simülasyonun bulduğu altı tasarım hatası** | **Güncel** |
| [27-zaman-modeli.md](27-zaman-modeli.md) | Servis günü, görev süreleri, hazırlama süresi türetmesi, eş zamanlılık, zirve doluluğu | Güncel |
| [28-zirve-karari.md](28-zirve-karari.md) | Mutfak saat dağılımı ile kapasite çelişkisinin kararı | Güncel |
| [30-mekan-yerlesimi.md](30-mekan-yerlesimi.md) | Arsa, oda ızgarası, kademe açılışı ve mobilya yerleşimi | Güncel |
| **[31-oda-ve-kamera.md](31-oda-ve-kamera.md)** | **İki kademeli kamera, ölçülmüş dokunma hedefi, mobilya/karakter yön kuralı** | **Güncel** |
| [32-ekipman-ve-yeniden-denge.md](32-ekipman-ve-yeniden-denge.md) | İstasyon slotları, `attendBp` ve gerçekleşme oranının kalibrasyonu | Güncel |
| [33-ikinci-mutfak.md](33-ikinci-mutfak.md) | Türk mutfağı: yemek grupları içerikten gelir, üç yerde doğrulanır | Güncel |
| [34-ilerleme-ve-kilit.md](34-ilerleme-ve-kilit.md) | İlerleme kilidi, karmaşıklık, mevsim ve beş ölü içeriğin canlandırılması | Güncel |
| **[35-canlandirma-ve-kamera.md](35-canlandirma-ve-kamera.md)** | **Yürüyüş katmanı, çalışan fırın/ocak, iki parmak yakınlaştırma ve kare masa** | **Güncel** |
| **[36-sokak-gun-ve-mutfak.md](36-sokak-gun-ve-mutfak.md)** | **Odalar arası kapılar, sokak ve yoldan geçenler, günün saati, mutfağın aşamaları, tepsi** | **Güncel** |
| **[37-dort-agent-incelemesi.md](37-dort-agent-incelemesi.md)** | **Dört agent incelemesi: fiyat tavanı, kombo açığı, sabır donması, yayın kapıları, kriz şeridi** | **Güncel** |
| **[38-sokak-ve-ic-isik.md](38-sokak-ve-ic-isik.md)** | **Sokak lambalarının düzeni ve klasik fener modeli, gece ışığının üç katmanı, yaya çarpışması ve figür profili, iç tavan aydınlatması, kanatların kaldırılması** | **Güncel** |
| **[39-tabak-dongusu.md](39-tabak-dongusu.md)** | **Devralınan kadro (aşçı + garson), sayılı tabak döngüsü, bulaşık nöbeti, ölüm sarmalını kıran oturtma eşiği** | **Güncel** |
| **[40-iki-dil.md](40-iki-dil.md)** | **İngilizce yerelleştirme: tek üreteçten iki tablo, çeviri politikası, cihaz diline göre varsayılan, turun bulduğu iki hata** | **Güncel** |
| **[41-arayuz-ve-mekan.md](41-arayuz-ve-mekan.md)** | **Oyun ekranının referansa göre yeniden tasarımı (kartlar, kapsüller, üç renk rolü), prosedürel süsleme ve mutfak kimliği** | **Güncel** |
| **[42-kadro-ve-mudahale.md](42-kadro-ve-mudahale.md)** | **Pasif oyuncu artık batıyor; müdahale baskı altında kazandırıyor; kadro tavsiyesinin adı yanlıştı** | **Güncel** |
| **[43-inceleme-ve-olcum.md](43-inceleme-ve-olcum.md)** | **Beş agentli inceleme: turun 107 kontrolü hiçbir şeyi kıramıyordu; vakumda yeşil kalan kontroller; ölçümün kendi kaynağını tüketmesi** | **Güncel** |
| **[44-magaza-metinleri.md](44-magaza-metinleri.md)** | **Mağaza kısa/tam açıklaması (iki dil), gizlilik politikası metni ve mağaza çözünürlüğünde ekran görüntüsü üretimi** | **Güncel** |
| **[45-tasarim-incelemesi.md](45-tasarim-incelemesi.md)** | **Beş agentli tasarım turu: fiyatın talebe kanalı yoktu (tavanda zam bedavaydı), müdahale salona da bağlandı, kombo ekseninin doygunluğu semptom çıktı** | **Güncel** |
| **[46-gonderilen-ikili.md](46-gonderilen-ikili.md)** | **Emülatör neden işe yaramıyor (ARM64-only APK) ve `link.xml`: hiç koşturulmamış budama koruması mutasyonla ölçüldü — kaldırılınca oyun açılışta ölüyor** | **Güncel** |
| **[47-tanima-ve-karne.md](47-tanima-ve-karne.md)** | **Günlük görev değil TANIMA: haftalık karne (tek başarı anı → dokuz) ve yedi nişan; birim hatası nişanı birinci günde dağıtıyordu** | **Güncel** |
| **[48-gunun-sivriligi.md](48-gunun-sivriligi.md)** | **Baskı neden hiç ateşlenmiyordu: günün en yoğun anı ortalamanın 1,24 katıydı; süre sivriltildi, müdahalenin değeri +505 → +4.953** | **Güncel** |
| **[49-ulasilamayan-mekanikler.md](49-ulasilamayan-mekanikler.md)** | **Yirmi komutun dort eksende taranmasi: malzeme kalitesi hicbir ekranda yoktu, "kovala" dugmesine kimse basmiyordu, erken tahsilat tuzak cikti** | **Güncel** |
| **[50-mutfak-dokusu.md](50-mutfak-dokusu.md)** | **Mutfaga gore duvar yuzeyi (ahsap lambri / celik bant), zemin ve disarinin tonu; tur artik fast food da oynuyor ve kirpilma olcumu var olmayan bir hatayi ariyordu** | **Güncel** |
| **[51-self-servis.md](51-self-servis.md)** | **Fast food self servis: garson yerine temizlikçi, salon rolleri mutfağa bağlandı, hacim +%43 fiş −%22; hacim toplam kasa için bir kol değil** | **Güncel** |
| `../unity/` | Unity 6.3 LTS projesi, Android hedefli. Ayarlar `ProjectSetup.cs` ile kodla uygulanıyor | Kuruldu |
| `../src/Lokanta.Harness/` | Denge aracı: beş strateji, çok tohumlu altmış günlük kampanya | Çalışıyor |

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

Ayrıntı ve her kalemin açıklaması için [06-plan-durumu.md](06-plan-durumu.md).

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
`tools/unity/tur.ps1` ve `tools/balance` çıktılarından geliyor.*

### Yayına ne kaldı

Teknik taraf hazır ([21](21-is-ve-yayin.md) yayın denetimi paketin içinden
doğruladı: izin yok, veri toplanmıyor, 64 bit, 16 KB hizalı, lisanslar
derlemede). Kalanların çoğu **kullanıcının kararı** — parola, hesap, yasal form:

1. Yükleme anahtarı (keystore) üret ve **iki ayrı yerde yedekle**
2. Ad kararı + marka taraması — paket adı ilk yüklemede kilitleniyor
3. Play Console hesabı, ardından 14 günlük kapalı test
4. Gizlilik politikası URL'si — metin hazır, [44](44-magaza-metinleri.md)
5. Veri Güvenliği formu — cevap hazır: "veri toplanmıyor"
6. IARC yaş derecelendirmesi
7. **Para modeli.** Satın alma kodu yok; oyun bugün ücretsiz ve iki mutfak
   açık olarak yayınlanabilir. Ücretsizden ücretliye geçiş imkânsız, yani
   geri dönüşsüz bir karar
8. Öne çıkan görsel (1024×500). Ekran görüntüleri otomatik:
   `tools\unity\tur.ps1 -Magaza`

### İlk güncellemeden önce bilinmesi gereken

`Simulation.SaveVersion` değişirse **her oyuncunun kaydı "bozuk" görünür** ve
altmış günlük kampanyası gider. Bugün zararsız (yayınlanmış kayıt yok), ama
yayından sonra sürüm artırmak bir **göç yolu** gerektirir: okuyucunun eksik
alanları varsayılanla karşılaması. Bunu yazmadan içerik yaması çıkarılmamalı.

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
6. ~~Ekonomi sayıları ve müşteri formülleri~~ — dengelendi, bkz. [42](42-kadro-ve-mudahale.md)
7. ~~Parti 1~~ — veri şemaları, personel sistemi, kayıt sistemi
8. ~~Parti 2~~ — ekranlar, öğretici, ses tasarımı, hikaye metinleri
9. ~~Parti 3, 4 ve 5~~ — teknik kurulum, üretim kararları, iş ve yayın
