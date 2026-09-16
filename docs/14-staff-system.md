# Personel Sistemi

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** A8
**Durum:** Parti A kapandı. Kapasite ve kadro tabloları `tools/balance/model.py` tarafından üretiliyor.

---

## Neden önemli

Araştırmada Tavern Keeper'ın en çok övülen tarafı personelin karakterli olmasıydı. Cat Cafe Manager ise personel yapay zekâsının aptallığı ve "beceriler önemsiz" hissi yüzünden battı.

İki ders: **personel karakterli olmalı, ve becerileri gerçekten fark yaratmalı.**

---

## Dört rol

| Rol | İş | Günlük ücret | İstasyonlar |
|---|---|---|---|
| Aşçı | Yemek pişirir | 140 | Ocak, ızgara, fırın |
| Garson | Sipariş alır, servis yapar | 110 | Salon |
| Kasiyer | Ödeme alır | 100 | Kasa |
| Bulaşıkçı | Tabak döngüsü, temizlik | 90 | Bulaşık |

Roller bütün mutfaklarda ortak. Değişen şey istasyon tipleri.

**Bulaşıkçı neden var:** tabak biterse servis durur. Görünmeyen ama tıkanınca fark edilen bir darboğaz. Oyuncuya "önemsiz görünen şeyi ihmal etme" dersi veriyor.

---

## On iki huy

Her personele havuzdan **iki huy** düşer. Çakışan huylar birlikte gelmez.

| Huy | Etki |
|---|---|
| Hızlı ama dağınık | Hız +%18, temizlik -%15 |
| Yavaş ama titiz | Hız -%12, yemek kalitesi +%20 |
| Kalabalıkta panikleyen | Yoğun dilimlerde hız -%25 |
| Sakin | Yoğunluk cezası yok |
| Müşteriyle iyi anlaşan | Servis ettiği masada memnuniyet +8 |
| Suratsız | Servis ettiği masada memnuniyet -6 |
| Çabuk yorulan | Günün son çeyreğinde hız -%20 |
| Dayanıklı | Yorulma yok |
| Ekip moralini yükselten | Diğer personelin morali +10 |
| Huysuz | Diğer personelin morali -8 |
| Çırak | Ücret -%25, hız -%15, deneyim kazanımı iki katı |
| Tecrübeli | Ücret +%30, hız +%15, deneyim kazanmaz |

**Çakışmalar:** hızlı ama dağınık ile yavaş ama titiz, kalabalıkta panikleyen ile sakin, çabuk yorulan ile dayanıklı, müşteriyle iyi anlaşan ile suratsız, ekip moralini yükselten ile huysuz, çırak ile tecrübeli.

**Tasarım niyeti:** hiçbir huy saf iyi veya saf kötü değil. Çırak ucuz ama yavaş, tecrübeli hızlı ama pahalı. Doğru huy doğru istasyona bağlı. Kalabalıkta panikleyen biri kasada felaket, bulaşıkta sorun değil.

---

## Moral

0 ile 100 arası. Yeni personel 70 ile başlar.

| Olay | Değişim |
|---|---|
| Maaş zamanında ödendi | +5 |
| Maaş gecikti | -25 |
| Üst üste yoğun gün | Günde -3 |
| İzin günü verildi | +10 |
| Zam yapıldı | +15 |
| Ekip moralini yükselten biri var | +10 |
| Huysuz biri var | -8 |
| Yıl sonu değerlendirmesi iyi geçti | +20 |

### Moral eşikleri

| Moral | Sonuç |
|---|---|
| 70 üstü | Normal çalışıyor |
| 30-70 | Hız kaybı yok ama hata şansı hafif artıyor |
| 30 altı | Hız -%20, hata şansı belirgin artıyor |
| 15 altı | Her gün %10 istifa riski |

**Batma merdiveniyle bağlantı:** maaş gecikmesi merdivenin üçüncü kademesi. Moral çöküşü ve istifa, batmanın somut yüzü oluyor. Sayı kaybetmek soyut, adını bildiğin bir çalışanın istifa etmesi somut.

---

## Deneyim ve seviye

- Çalışılan her gün **1 deneyim puanı**. Çırak huyu varsa 2.
- **30 puanda seviye atlar.** Maksimum 3 seviye.
- Her seviye hız +%10 getirir.
- Seviye atlayınca **maaş talebi +%15**. Kabul etmezsen moral -20.

Bu, oyuncuya gerçek bir karar veriyor: iyi çalışanı elde tutmak pahalılaşıyor. Yeni ucuz birini alıp baştan eğitmek mi, yoksa zam verip devam etmek mi?

---

## İşe alım

- İşe alım ekranında aynı anda **üç aday** görünür.
- Adaylar üretilir: rol, iki huy, görünüm, isim.
- Aday havuzu **her üç günde bir** yenilenir. Beğenmediğin adayı reddedebilirsin ama yenisi hemen gelmez.
- İşe alım ücreti yok. Sadece maaş.
- Adayın huyları **işe almadan önce görünür.** Gizli bilgi yok, çünkü bu bir yönetim oyunu, kumar değil.

### Kaç kişi çalıştırabilirsin

| Masa | Azami personel |
|---|---|
| 4 | 2 |
| 7 | 4 |
| 10 | 6 |
| 14 | 8 |

Sınır mekân kademesine bağlı. Küçük restorana çok personel almanın anlamı yok, zaten sığmıyorlar.

---

## İşten çıkarma

- Tazminat: **bir haftalık maaş.**
- Çıkarılan kişi o gün gider.
- Diğer personelin morali **-10**. Ekipten biri gittiğinde herkes etkileniyor.

Tazminat ve moral cezası birlikte, "beğenmediğini at yenisini al" davranışını pahalı hale getiriyor.

---

## İstasyon ataması

Her sabah tezgâh aşamasında personeli istasyonlara dağıtırsın.

- Bir istasyonda birden fazla kişi çalışabilir, ama verim azalarak artar. İkinci kişi %70 katkı, üçüncü %45.
- Rolü dışında bir istasyona atanan personel **%50 verimle** çalışır. Garson ocağa geçebilir ama iyi olmaz.
- Atama gün içinde değiştirilemez. Sabah verdiğin karar günü belirler.

**Bu son kural önemli.** Servis sırasında istasyon değiştirilebilseydi, sabah kararının anlamı kalmazdı. Oyunun ana fikri hazırlığın doğruluğu.

---

## Kapasite modeli: kaç müşteriye kaç çalışan

Bu bölüm oyunun büyüme baskısının kalbi. Restoran büyüyünce müşteri artıyor, müşteri artınca personel gerekiyor, personel artınca maaş artıyor.

### Günlük kapasite

Her rolün bir günlük kapasitesi var. Temel değerler, birinci seviye ve huysuz bir personel için:

<!-- ÜRETİLEN: kapasite -->
| Rol | Günlük kapasite | Günlük ücret | Müşteri başına iş | Salon yükü payı |
|---|---|---|---|---|
| Aşçı | 30 müşteri | 140 | 0.0333 iş-günü | ayrı havuz |
| Garson | 26 müşteri | 110 | 0.0385 iş-günü | %52 |
| Bulaşıkçı | 48 müşteri | 90 | 0.0208 iş-günü | %28 |
| Kasiyer | 70 müşteri | 100 | 0.0143 iş-günü | %19 |
<!-- /ÜRETİLEN: kapasite -->

**Garson ilk tıkanan yer.** Salon iş yükünün yüzde 52'sini o taşıyor. Bu bilinçli: oyuncunun ilk fark ettiği darboğaz salonda olmalı, çünkü orası görünür.

### İki havuz, tek patron

Değerlendirme, eski modelin patronu aynı anda üç sütunda saydığını buldu. Düzeltilmiş model iki havuz kullanıyor:

**Aşçı havuzu.** Ayrı hesaplanır. Patron pişiremez — oyuncu patron, şef değil. Gereken aşçı = zirve müşteri ÷ 28, yukarı yuvarlanır.

**Salon havuzu.** Garson, bulaşıkçı ve kasiyer tek bir iş havuzu. Küçük lokantada aynı kişi hem servis yapar hem kasaya bakar hem tabak toplar. Kapasite hesabı istasyon başına değil, **iş-günü** üzerinden yapılır:

```
salon_iş_yükü = zirve_müşteri × 0,0769 iş-günü
gereken_salon = tavan(salon_iş_yükü − patron_katkısı)
```

İstasyon ataması oyuncuya görünür ve performansı etkiler, ama kadro büyüklüğünü iş yükü belirler. Bu, "0,4 garson" saçmalığını ortadan kaldırıyor.

### Patronun katkısı

Oyuncu da çalışıyor. Patron salonda **1,4 iş-günü** katkı veriyor, yani bir personelden fazla. Kendi işi olduğu için daha çok yükleniyor.

**Ama aynı anda tek yerde olabiliyor.** Eski modelin hatası buydu: patron üç sütunda birden sayılınca tek kişi 36 müşterilik kapasite veriyordu.

Bu katkı ilk haftayı tek aşçıyla çıkarmaya yetiyor: 17 müşterilik zirvenin salon yükü 1,31 iş-günü, patron 1,4 taşıyor. İkinci hafta 1,46'ya çıkıyor ve ilk garson gerekiyor. **Oyunun ilk işe alımı buradan geliyor, senaryodan değil.**

### Kapasite nasıl büyüyor

| Kaynak | Etki |
|---|---|
| Deneyim seviyesi | Seviye başına +%10, azami +%30 |
| Huylar | Hızlı ama dağınık +%18, çırak −%15 gibi |
| Ekipman | İstasyon sayısını artırıyor, aşçı tavanını yükseltiyor |

### Gereken kadro

<!-- ÜRETİLEN: kadro -->
| Hafta | Zirve müşteri/gün | Aşçı | Salon | Toplam kadro | Tavan | Salon iş yükü | Patron düşülünce |
|---|---|---|---|---|---|---|---|
| 1 | 17 | 1 | 0 | **1** | 3 | 1.25 | 0.00 |
| 2 | 19 | 1 | 1 | **2** | 3 | 1.40 | 0.10 |
| 3 | 36 | 2 | 2 | **4** | 5 | 2.65 | 1.35 |
| 4 | 39 | 2 | 2 | **4** | 5 | 2.87 | 1.57 |
| 5 | 59 | 2 | 4 | **6** | 8 | 4.34 | 3.04 |
| 6 | 63 | 3 | 4 | **7** | 8 | 4.64 | 3.34 |
| 7 | 92 | 4 | 6 | **10** | 12 | 6.77 | 5.47 |
| 8 | 97 | 4 | 6 | **10** | 12 | 7.14 | 5.84 |
<!-- /ÜRETİLEN: kadro -->

Kadro **hafta sonu zirvesine** kurulur, ücreti yedi gün ödenir. Zirveyi karşılayamayan restoran itibar kaybeder, fazla kadro ise boş gün maaşı öder.

Deneyim kazandıkça maaş talebi arttığı için haftalık %2,2 birikimli zam uygulanıyor. Sekizinci haftada bu, taban maaşın üstüne yaklaşık %16 ekliyor.

### Kadro tavanı

| Masa | Tavan | O kademede gereken azami | Boşluk |
|---|---|---|---|
| 4 | 3 | 2 | 1 |
| 7 | 5 | 4 | 1 |
| 10 | 8 | 7 | 1 |
| 14 | 12 | 11 | 1 |

**Tavan her kademede gerekenden bir fazla.** Oyuncu hata payına sahip ama kadro şişiremiyor. Fazladan alınan kişi doğrudan zarar yazıyor.

---

## Eksik kadronun cezası

Kapasite yetmezse oyun ceza vermiyor, **kendi kendine cezalandırıyor.** Döngü şu:

1. Personel yetmiyor
2. Bekleme süreleri uzuyor
3. Sabır tükeniyor, müşteriler çıkıp gidiyor
4. Memnuniyet düşüyor
5. İtibar düşüyor
6. Ertesi gün daha az müşteri geliyor

Altıncı adım önemli: eksik kadro yarını da vuruyor. Tek günlük bir hata değil, kendini besleyen bir çöküş.

**Fazla kadronun cezası** ise daha basit: maaş müşteri gelsin gelmesin ödeniyor. Dört masalık restorana üç kişi almak doğrudan zarar.

İki ceza birlikte, kadro kararını gerçek bir karar yapıyor. Ne eksik ne fazla.

---

## Büyümenin ekonomisi

Restoranı büyütmek üç şeyi aynı anda yapıyor:

| Etki | Yön |
|---|---|
| Daha çok masa, daha çok müşteri | Gelir artıyor |
| Daha çok müşteri, daha çok personel | Maaş artıyor |
| Daha büyük mekân | Kira artıyor |

Sonuç, kâr marjının **yavaşça** iyileşmesi.

<!-- ÜRETİLEN: marj -->
| Hafta | Malzeme | Maaş | Kira | Genişleme | Net marj |
|---|---|---|---|---|---|
| 1 | %32 | %27 | %23 | %0 | **%17.7** |
| 2 | %32 | %42 | %21 | %0 | **%5.2** |
| 3 | %32 | %42 | %23 | %29 | **−%26.0** |
| 4 | %32 | %38 | %21 | %0 | **%9.1** |
| 5 | %32 | %34 | %19 | %29 | **−%13.4** |
| 6 | %32 | %37 | %17 | %0 | **%14.0** |
| 7 | %32 | %35 | %18 | %29 | **−%14.4** |
| 8 | %32 | %31 | %16 | %0 | **%20.1** |
<!-- /ÜRETİLEN: marj -->

**Üç genişleme haftasının üçü de eksiye düşüyor.** Kira ve kadro aynı anda büyüyor ama itibar henüz yetişmemiş oluyor. Bu bilinçli ve modele kısıt olarak yazıldı.

Sekizinci haftada net marj yüzde 20'ye çıkıyor. Büyümek kazandırıyor, ama on bir kat değil, iki kat. Araştırmadaki "harcayamayacağın kadar para" tuzağından bizi koruyan şey bu eğri.

**Maaş payı düşüyor, kira payı da.** Yüzde 30'dan 24'e, yüzde 33'ten 24'e. Ölçek ekonomisi çalışıyor ama yavaş. Marjın iyileşmesi büyümenin ödülü.

**Ortalama fiş tutarı da büyüyor:** birinci haftada 50, sekizinci haftada 75. Menü genişledikçe pahalı yemekler açılıyor ve kombo gibi mekanikler devreye giriyor. Bu gizli bir büyüme kaldıracı.

---

## Üç isimli personel

Mutfak başına üç personel elle tasarlanır. Adı, yüzü, huyları ve küçük bir hikayesi olur.

- Bunlar aday havuzunda **garantili olarak** belirli günlerde çıkar.
- Huyları sabittir, rastgele değil.
- Kendi hikaye anları vardır: neden bu işi arıyor, ne yapmak istiyor.
- Uzun süre çalıştırılırsa ek sahneler açılır.

Üretilen personelin de adı ve huyları var, yani kimse isimsiz değil. Fark, elle yazılmış hikayenin olup olmaması.

---

## Personel yapay zekâsı: basit tutulacak

Cat Cafe Manager ve Tavern Keeper'ın ortak yarası buydu. Yol bulamayan, boş boş bekleyen çalışanlar.

**Karar: karmaşık yol bulma yok.**

- Personel istasyonuna sabitlenir, salonda dolaşmaz.
- Garson masaya gider ama yolu önceden hesaplanmış, kısa ve öngörülebilir.
- Sıra mantığı basit: en uzun bekleyen masa önce.
- Çakışma yok, personel birbirinin içinden geçebilir. Görsel olarak fark edilmiyor ve bütün bir hata sınıfını ortadan kaldırıyor.

Basit ve doğru çalışan bir sistem, karmaşık ve bozuk çalışan bir sistemden iyidir. Oyuncu yol bulma algoritmasını fark etmez, ama takılan çalışanı hemen fark eder.

---

## Karar bekleyen ayrıntılar

1. Azami seviye üç mü kalmalı
2. Aday havuzu üç günde bir mi yenilenmeli
3. İzin günü mekaniği olacak mı, yoksa moral sadece maaşa mı bağlı olsun
4. Personel yorulması gün içinde görünür olmalı mı

**Kapanan sorular.** "Garson kapasitesi 16 doğru mu" ve "patronun 12 müşterilik katkısı makul mü" soruları Parti A'da kapandı. Kapasiteler 28/25/46/66, patron katkısı 1,4 iş-günü. İkisi de tahmin değil, `tools/balance/solve.py` tarafından tasarım hedeflerinden arandı.
