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

## 5. Kalan: `SetDishwashers`

Bulaşıkçı ataması arayüzde ve testlerde var, ama **hiçbir denge botu
kullanmıyor** — yani tabak darboğazının kararı ölçülmemiş durumda. Bu
belgede kapatılmadı; kuyruğa yazıldı.
