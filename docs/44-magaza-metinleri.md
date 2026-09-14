# 44 — Mağaza metinleri ve gizlilik politikası

*13 Eylül 2026.* [docs/21](21-is-ve-yayin.md) yayın denetiminin 8. maddesi
"kısa açıklama, tam açıklama, öne çıkan görsel ve ekran görüntüleri yok" diyordu.
Bu belge **metin** tarafını kapatıyor; görsel tarafın otomatikleşen kısmı da
aşağıda.

Metinler **taslak**: ad kararı ([docs/25](25-oyun-adi.md)) ve marka taraması
kesinleşmeden mağazaya girmemeli, çünkü ad metinlerin içinde geçiyor.

---

## 1. Kısa açıklama (≤80 karakter)

| dil | metin | karakter |
|---|---|---:|
| TR | Patronsun, aşçı değil. Altmış günde bir lokantayı ayakta tut. | 60 |
| EN | You're the owner, not the cook. Keep a restaurant alive for 60 days. | 68 |

Kısa açıklama arama sonucunda başlığın altında görünüyor ve **tek işi** oyunun
ne olduğunu söylemek. "Patronsun, aşçı değil" cümlesi zaten oyunun ana menüsünde
duruyor ([docs/02](02-tasarim-onerisi.md)'nin temel ayrımı): bu bir yemek
yapma oyunu değil, bir **yönetim** oyunu. Mağazada bu ayrımın ilk satırda
olması gerekiyor, çünkü yanlış beklentiyle indiren oyuncu kötü yorum bırakıyor.

---

## 2. Tam açıklama (≤4000 karakter)

### Türkçe

```
Devraldığın dört masalık bir lokanta, huysuz bir aşçı ve altmış gün var.

Sen mutfakta değilsin. Senin işin menüyü kurmak, halden malzeme almak,
kimi işe alacağına karar vermek ve servis kızıştığında doğru masaya
yetişmek. Yemekleri aşçın yapıyor — iyi ya da kötü, tuttuğun kişiye göre.

• MENÜ BİR KARAR. Otuz iki yemek var ama hepsini açık tutamazsın: menüde
  duran her yemek için stok tutuluyor ve akşam bozulan her şey çöpe
  gidiyor. Dar menü az zayiat, geniş menü çok müşteri.

• KADRO BİR TAKAS. Ücret her gün ödeniyor, zirve haftada iki gün. Tam
  kadro herkese yetişir ama parayı yer; bir kişi eksik çalışmak kazandırır
  ve karşılığında masadan kızgın kalkan müşteriler bırakır.

• SERVİS SIRASINDA SEN VARSIN. Sayılı müdahale hakkın var — dükkân
  büyüdükçe artıyor. Mutfağı hızlandır, salona çay çıkar, ya da bir
  masayla kendin ilgilen: ilgilendiğin masa daha çabuk dönüyor. Hepsi
  aynı keseden çıkıyor ve harcanmayan hak gece yanıyor.

• MUTFAĞININ BİR İMZASI VAR. Fast food'da kombo: ortalama fişi yükseltir
  ama mutfağı yorar, zirvede kapatmak akıllıca olabilir. Türk mutfağında
  veresiye: müdavimine defterden yazarsın, tahsilat güvene bağlıdır.

• BATMAK OYUNU BİTİRMEZ. Kasa eksiye düşerse ekipman satılır, dükkân
  küçülür, borç silinir — ama yıl sonu değerlendirmesinde izi kalır.

Altmışıncı günde yedi eksende puanlanıyorsun: varlık, itibar, müdavimler,
ekip, mekân, sağlamlık ve mutfağının imzası. Sonrasında serbest oyun.

Reklam yok. Oyun içi satın alma ile güç satılmıyor. Hiçbir veri
toplanmıyor — internet izni bile istemiyor.

Türkçe ve İngilizce.
```

### English

```
You inherit a four-table restaurant, a cranky cook and sixty days.

You are not in the kitchen. Your job is to set the menu, buy at the
market, decide who to hire, and get to the right table when service
heats up. Your cook does the cooking — well or badly, depending on who
you hired.

• THE MENU IS A DECISION. There are thirty-two dishes but you cannot
  keep them all open: every dish on the menu ties up stock, and anything
  perishable goes in the bin tonight. A narrow menu wastes less; a wide
  one draws more.

• YOUR CREW IS A TRADE. Wages are paid every day; the peak is two days a
  week. A full crew serves everyone but eats the cash; running one short
  earns more and leaves guests walking out angry.

• SERVICE IS WHERE YOU ARE. You get a handful of interventions a day, and
  more as the place grows: rush the kitchen, send tea out to the room, or
  attend a table yourself — the table you attend turns over faster. They
  share one budget, and unspent ones burn at midnight.

• YOUR CUISINE HAS A SIGNATURE. Fast food has the combo: it raises the
  average ticket but loads the kitchen, so closing it at the peak can be
  the smart play. Turkish has credit: you write a regular into the book
  and collection depends on trust.

• GOING BROKE DOES NOT END THE GAME. Equipment is sold, the shop shrinks,
  the debt is written off — but it leaves a mark on the year-end score.

On day sixty you are scored on seven axes: wealth, reputation, regulars,
crew, place, resilience and your cuisine's signature. Free play after.

No ads. No power sold through purchases. No data collected at all — the
app does not even ask for internet permission.

Turkish and English.
```

**Müdahale cümlesi 14 Eylül'de güncellendi.** Üç yerden eskimişti: hak
sayısı artık sabit dört değil masayla büyüyor, çay tek masaya değil salona
gidiyor, ve ilgilenilen masanın salon işi yarıya iniyor — yani müdahale
yalnızca krizi savuşturmakla kalmıyor, masa devir hızını da artırıyor
(docs/45 §16). Metin ölçülene uydu, tersi değil.

**Neden bu yapı.** Play listesinde ilk üç satır kesilmeden görünüyor, gerisi
"devamını oku" ardında. O yüzden ilk paragraf oyunun kendisini anlatıyor ve
madde işaretleri **mekanikleri değil KARARLARI** sayıyor — bir yönetim oyununun
satış noktası özellik listesi değil, oyuncunun vereceği kararlar.

"Reklam yok / veri toplanmıyor" satırı sonda ve kısa: doğrulanmış bir gerçek
([docs/21](21-is-ve-yayin.md) yayın denetimi) ve bu kategoride ayırt edici.

---

## 3. Gizlilik politikası

Play, veri toplanmasa bile **her** uygulamadan bir URL istiyor. Metin kısa,
çünkü söylenecek şey tek cümle. Kullanıcının barındırması gerekiyor (GitHub
Pages yeterli).

```
GİZLİLİK POLİTİKASI — Lokanta

Son güncelleme: 13 Eylül 2026

Bu uygulama hiçbir kişisel veri toplamaz, saklamaz veya paylaşmaz.

• Hesap açmanız istenmez.
• Reklam ağı, analitik veya çökme raporlama aracı içermez.
• İnternet erişim izni istemez ve hiçbir sunucuya bağlanmaz.
• Oyun kayıtlarınız yalnızca cihazınızda tutulur. Uygulamayı
  kaldırdığınızda silinirler.

Çocuklara yönelik değildir.

Soru için: <e-posta adresi>
```

```
PRIVACY POLICY — Lokanta

Last updated: 13 September 2026

This app collects, stores and shares no personal data.

• No account is required.
• It contains no advertising network, analytics or crash reporting.
• It requests no internet permission and connects to no server.
• Your saved games are kept on your device only. Uninstalling the app
  deletes them.

It is not directed at children.

Contact: <email address>
```

**Doğrulanmış.** Bu iddiaların hepsi [docs/21](21-is-ve-yayin.md)'deki yayın
denetiminde paketin **içinden** kontrol edildi: izin listesi boş, INTERNET yok,
`UnityConnectSettings` kapalı, kodda ağ çağrısı yok. Yani Veri Güvenliği formu
da aynı cevabı verecek.

*Bir gizlilik politikası, doğruluğu ölçülmeden yazılırsa yasal bir risktir —
burada ölçüldü.*

---

## 4. Ekran görüntüleri

Otomatik tur artık mağaza çözünürlüğünde de koşuyor:

```
.\tools\unity\tur.ps1 -Magaza
```

Ne yapıyor: turu **2183×983** piksel penceresinde, `-lokanta-olcek 2.5` ile
koşuyor. Bu, 873×393 dp'nin tam iki buçuk katı — yani **aynı arayüz yerleşimi**,
farklı bir düzen değil. Oran da aynı (20:9), yani çerçeve telefonda görünenin
birebir aynısı. Görüntüler `render/magaza/` altına kopyalanıyor.

Farklar ölçüm turundan:

| | ölçüm turu | mağaza turu |
|---|---|---|
| ölçek | 1 px = 1 dp (873×393) | 2,5 px = 1 dp (2183×983) |
| ipuçları | **açık** (katılım katmanı da ölçülüyor) | **kapalı** (şerit salonun üstüne biniyordu) |
| an | 1. gün, servis başı | **40. gün, günün ortası** |

İpucu şeridi ölçüm turunda görünmeli — oyunun yeni oyuncuya gösterdiği hâli o.
Mağazada ise listeye öğreticinin kendisi çıkıyordu.

**Anın seçimi önemliydi.** İlk hâli 1. günü çekiyordu: dört masa, "Ciro 0",
"Memnuniyet 0,0" ve çerçevenin yarısı boş gökyüzü — oyunu olduğundan küçük
gösteriyordu. Üstelik tur o zaman **hiç genişlemiyordu**, yani mağaza için
büyümüş bir restoran zaten yoktu.

İkisi birden düzeldi: tur artık kampanya boyunca genişliyor (`Buyu()`, bir
kademe ve ancak bedelin üç katı kasada varsa) ve görüntü kırkıncı günün
ortasında alınıyor. Sonuç: **14 masa, 10'u dolu, Ciro 1.188, Memnuniyet 78,5,
itibar 96,8/100.** Hâlâ oynanan oyun — hızlandırılmış ama uydurulmuş değil.

**Üçüncü bir koşul daha gerekti: salonun üstü açık olmalı.** Bir koşuda üç
bildirim balonu ("Soruldu ama yok: ...") görüntünün tam ortasına yığıldı ve
restoran görünmez oldu; bir öncekinde hiç balon yoktu. Yakalama artık
`NoticeCount == 0` olan bir kare bekliyor ve bunu ölçüyor — yoksa mağaza
görseli koşudan koşuya değişiyordu.

Turun genişlemesi ayrıca bir **ölçüm boşluğunu** kapattı: genişleme
kampanyanın ana ilerleme yolu ve tur onu hiç koşmuyordu (kademe geçişinde
sahnenin yeniden kurulması, kadro tavanının büyümesi, kiranın artması —
hiçbiri sınanmıyordu).

**Kalan görsel işi kullanıcıda:** öne çıkan görsel (1024×500) bir kapak
tasarımı ve ad kararına bağlı; mağaza simgesi zaten hazır
(`Art/Simge/magaza-simgesi-512.png`).

---

## 5. Bunlar neyi kapatmıyor

[docs/21](21-is-ve-yayin.md)'in kullanıcı listesinden kapanan: **yok.** Bu belge
o listenin 8. maddesinin *metin* yarısını ve ekran görüntüsü üretimini
hazırlıyor; kararların hepsi yerinde duruyor:

1. Yükleme anahtarı (parola)
2. Ad kararı ve marka taraması — **bu belgedeki metinler ona bağlı**
3. Play Console hesabı ve 14 günlük kapalı test
4. Gizlilik politikası URL'si — metin hazır, barındırma gerekiyor
5. Veri Güvenliği formu — cevap hazır ("veri toplanmıyor")
6. IARC yaş derecelendirmesi
7. Para modeli — satın alma kodu **yok**, oyun bugün ücretsiz ve iki mutfak
   açık olarak yayınlanabilir; bu geri dönüşsüz bir karar
8. Öne çıkan görsel
