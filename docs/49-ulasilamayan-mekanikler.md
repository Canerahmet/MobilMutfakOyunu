# 49 — Oyuncuya ulaşmayan mekanikler

*15 Eylül 2026.* Bu turun en verimli damarı "hiç ateşlenmeyen şey" aramak
oldu ([48](48-gunun-sivriligi.md)). Aynı taramayı **sistematik** yaptım:
yirmi komutun her biri dört eksende kontrol edildi — denge botu, otomatik
tur, birim testi, ve **arayüz**.

| komut | harness | tur | test | arayüz |
|---|:--:|:--:|:--:|:--:|
| `SetQuality` | var | — | var | **YOK** |
| `CollectCredit` | — | — | — | var |
| `SetDishwashers` | — | — | var | var |

Üçü de aynı cümlenin farklı yüzü: *bir mekanik kodda eksiksiz olabilir ve
oyunda yok sayılabilir.*

---

## 1. `SetQuality`: üç kademeli bir karar ekseni, hiçbir ekranda yoktu

Malzeme kalitesi çekirdekte eksiksiz yazılıydı, denge botu onu ölçüyordu
(`ucuz_malzeme`), testleri vardı — ve **hiçbir ekranda düğmesi yoktu.**
Veresiye tahsilatı da tıpatıp aynı şekilde bulunmuştu
([45](45-tasarim-incelemesi.md)).

Üç kademe, ve etkisi malzemenin pahalılığına göre büyüyor:

| kademe | fiyat | memnuniyet |
|---|---|---|
| Düşük | %15-25 ucuz | **−5,0 … −20,0** |
| Standart | — | 0 |
| Yüksek | %15-35 pahalı | **+3,0 … +15,0** |

Ve mekanik ölü değil, aksine sertleşmiş: gün sivriltildikten sonra
`ucuz_malzeme` botu 17.179 / itibar 44 → **7.447 / itibar 8,1**. İki baskı
(bekleme ve düşük memnuniyet) birleşince ucuz malzeme neredeyse ölümcül.

**Seçici hal ekranına kondu** çünkü seçim o ekranın kendisini değiştiriyor:
`IngredientPriceToday` zaten `_quality` ile hesapladığı için kademeye
basınca aşağıdaki bütün fiyatlar anında oynuyor. Sonucu başka bir yerde
anlatmak gerekmiyor — görülüyor.

Görünmeyen taraf (memnuniyet) yazıyla, ve **uydurma bir ortalama değil**:
malzeme başına etki üç ayrı kalıba ayrılıyor, tek ortalama yanıltırdı. Onun
yerine oyuncunun **kendi menüsündeki** yemeklerin gerçek etkisi
hesaplanıyor — simülasyonun kendi `DishQualityCentiOf` ölçüsüyle, yani
ekranın yazdığı sayı servisin kullandığı sayının ta kendisi.

Tur da düğmenin varlığını değil **komutun simülasyona geçtiğini**
doğruluyor (1 → 2 → geri). Bu projede "düğme var ama komut gitmiyor" iki
kez çıktı; varlık testi o hatayı göremezdi.

---

## 2. `CollectCredit`: düğme vardı, kimse basmıyordu

`LedgerScreen`'de "Şimdi kovala" düğmesi duruyordu ve tur onun **ekranda
olduğunu** doğruluyordu — ama hiçbir şey ona basmıyordu. Varlık testi,
"düğme var ama komut gitmiyor" hatasını göremez.

Tur artık basıyor ve ölçütü **hesap sayısı**: kovalamak hesabı tahsil
edilse de edilmese de kapatıyor, oysa tutar değişimi zara bağlı. Kontrolü
zara bağlamak, koşudan koşuya renk değiştiren bir tik üretirdi.

---

## 3. Erken tahsilat ölçüldü: tuzakmış

Hiçbir bot `CollectCredit` göndermiyordu, yani tasarımın "tahsilat güvene
bağlıdır" diye sattığı kararın **ikinci yarısı hiç ölçülmemişti**.

`erken_tahsilat` botu yazıldı: her akşam bütün defteri kovalıyor.

| strateji | son kasa | itibar | ilk borç |
|---|---:|---:|---:|
| `imzaci` (sabırlı) | **15.079** | 71,6 | — |
| `erken_tahsilat` | 9.655 | 48,9 | 56. gün |

Şansı yarıya indirmek, parayı yedi gün erken almanın değerinden pahalı.
Mekanikte gerçek bir karar var ve **sabır kazanıyor**.

---

## 4. Botun kendisi sessizce hiçbir şey yapmıyordu

`erken_tahsilat` ilk koşusunda `makul` ile **bayt bayt aynı** sonucu verdi:
122.724 ciro, defter yok, imza ekseni 0. Bot koşuyor görünüyordu.

Sebep: `DuringService` **arayüzde değildi**. `Program.cs` onu bir tür
kontrolü zinciriyle dağıtıyordu:

```csharp
if (strategy is SignaturePlayer sp) sp.DuringService(sim);
else if (strategy is PickyCreditor pc) pc.DuringService(sim);
else if (strategy is PeakCloser pk) pk.DuringService(sim);
```

Zincirde olmayan yeni bir strateji **sessizce** hiçbir şey yapmıyordu — ve
bunu hiçbir şey söylemiyordu. Ölçüm aracının kendi ölçümü boş dönüyordu.

Zincire bir satır daha eklemek yerine sınıf kapatıldı: `DuringService`
artık arayüzde, **varsayılan gövdesi boş**. Yeni bir strateji yalnızca
metodu yazarak katılıyor; unutulabilecek bir kayıt yeri kalmadı. Mevcut
stratejilerin bütün sayıları birebir aynı kaldı — düzeltme davranışı
değiştirmedi, yalnızca yeni davranışın kaybolmasını engelledi.

*Bir ölçüm aracına yeni bir kol eklemek, o kolun koştuğunu kanıtlamadan
tamamlanmış sayılmaz.*

---

## 5. `SetDishwashers`: soru artık sorulabiliyor, cevap karışık

Bulaşıkçı ataması arayüzde ve testlerde vardı ama **hiçbir denge botu
kullanmıyordu** — tabak darboğazının kararı ölçülmemişti. `bulasikci` botu
yazıldı.

**İki kez yanlış ölçtüm, ikisini de aynı imza yakaladı.**

İlk eşik "salon ≥ 2" idi ve küçük dükkânda da ayırıyordu; ölçülen şey
"bulaşıkçı kazandırıyor mu" değil "erken ayırmak kaybettiriyor mu" oluyordu.
Eşiği 4'e çıkardım — bu kez sonuç `makul` ile **bayt bayt aynı** çıktı,
çünkü `ReasonablePlayer` dört salon çalışanına hiç ulaşmıyor. Eşik hiç
tetiklenmedi ve bunu yalnızca iki satırın aynı olması söyledi (geçen tur
`PeakCloser`'da tıpatıp aynısı olmuştu).

Bot `PlannerSchedule`'ın üstüne taşındı — adanmış bulaşıkçı zaten **büyük
dükkânın** sorusu.

| | son kasa | net | masa | tabaksız bekleme | en çok kirli |
|---|---:|---:|---:|---:|---:|
| `planci` | 23.375 | 15.375 | 12,0 | 263 | 14,6 |
| `bulasikci` | **24.850** | **16.850** | 11,5 | **349** | 13,6 |

**Kasa lehte ama ölçüm temiz değil ve asıl ölçüt ters yönde.** Bulaşıkçı
ayıran kol daha küçük bir dükkânla bitiriyor (11,5 masa) ve daha az yatırım
yapıyor — kasadaki fark oradan da gelebilir. Üstelik tabaksız bekleme
**artıyor** (263 → 349), yani mekanik kendi amacını gerçekleştirmiyor.

### Sebep kodda ve bir tasarım sorusu

```csharp
private bool WashNeeded()
{
    if (_platesDirty <= 0) return false;
    if (_dishwashers > 0) return false;      // bulasikci varsa salon karismaz
```

Adanmış bir bulaşıkçı, salonun geri kalanının **gerektiğinde yıkamasını
kapatıyor** — yani kapasiteye eklenmiyor, onun *yerine geçiyor*. Bir kişi,
"herkes gerektiğinde koşar" davranışından daha az yıkıyor.

Kuralın yorumu bunun bilinçli olduğunu yazıyor ("bulaşıkçı alınca herkes
kendi işini yapar" — kullanıcının kendi cümlesi). O yüzden **değiştirilmedi**:
bu bir ölçüm sonucu değil, bir tasarım kararı. Soru artık sorulabilir ve
sayıları var; kararı kullanıcının.
