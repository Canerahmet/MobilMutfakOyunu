# Ekonomi Sayıları ve Müşteri Formülleri

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** A4 ekonomi sayıları, A7 müşteri formülleri
**Durum:** Parti A kapandı. Bu dosyadaki bütün sayılar `tools/balance/model.py` tarafından formülden türetiliyor ve on yedi tutarlılık testinden geçiyor. Elle yazılmış tablo kalmadı.

---

## Bu sayılar nasıl üretiliyor

**Aşağıdaki tabloları elle değiştirmeyin.** `<!-- ÜRETİLEN -->` işaretçileri arasındaki her şey `tools/balance/render.py` çalıştırıldığında silinip yeniden yazılır. Bir sayıyı değiştirmek istiyorsanız `tools/balance/model.py` içindeki parametreyi değiştirip yazıcıyı çalıştırın.

```
cd tools/balance
python model.py --check     # 17 tutarlılık testi
python solve.py             # parametreleri hedeflerden arar
python render.py            # tabloları bu dosyaya yazar
```

### Gerçekleşme oranı

**Bu dosyadaki ciro, talep değil gerçekleşen cirodur.** Model talebin tamamının ağırlandığını varsayar; simülasyon aynı genişleme takviminde bunun bir kısmını üretir. Sabrı biten müşteri, tükenen stok, dolan masa.

**Güncel değer `tools/balance/model.py` içindeki `REALISATION_BP`, kiralar da `content/economy.json` içindeki `staffing.tiers`.** Buraya sayı yazılmıyor: bu satır bir kez sayı taşıdı ve bir kalibrasyon kuşağı geride kaldı — belge 7000 ve 850/1.950/2.900/5.000 anlatırken içerikte 6500 ve 650/1.550/2.250/4.000 vardı. Tasarımın referans belgesi var olmayan bir ekonomiyi anlatıyordu. Aşağıdaki bütün tablolar üretiliyor (`python tools/balance/export.py`), elle düzenlenmiyor.

Bu sayı ölçülen bir değer gibi görünüyordu ama değil: **sabit noktadır.** 10 Eylül 2026'da mutfak modeli düzeltilince aynı ölçüm %65'ten %93'e fırladı, ama o ölçümü doğrudan uygulamak genişlemeyi tuzağa çevirdi: %93 ESKİ ucuz kiralarla ölçülmüştü ve yeni kiralarla aynı strateji genişleyemedi. **Oran parametreleri, parametreler oranı belirliyor.**

Bu yüzden tek atışlık ölçüm yerine `python tools/balance/calibrate.py` çalıştırılır: aday oranları tek tek koşar (solve → model → export → simülasyon) ve tasarım hedeflerine göre puanlar. Ayrıntı [32-ekipman-ve-yeniden-denge.md](32-ekipman-ve-yeniden-denge.md).

### Değerlendirmenin bulduğu üç hata

Beş ajanlı değerlendirme (bkz. [review/00-sentez.md](review/00-sentez.md)) bu dosyanın sayılarının kendi formüllerinden türemediğini buldu. Model kurulunca üç ayrı hata çıktı:

| Hata | Neydi | Sonucu |
|---|---|---|
| **Patron çoğaltılmıştı** | Kapasite tablosunda patron aynı anda garson, kasiyer ve bulaşıkçı sütunlarında sayılıyordu | Tek kişi 36 müşterilik kapasite veriyordu, gerçek katkısı 12'ydi |
| **Kadro ortalamaya kuruluyordu** | Kadro hafta ortalamasına göre hesaplanmıştı | Hafta sonu %25 yoğun; kadro zirveye kurulup yedi gün ödenmeli |
| **Tablo formülle çelişiyordu** | Bölüm 5.1 formülü 14 masa/itibar 85 için 76 müşteri veriyor, bölüm 6 tablosu aynı satırda 58 yazıyordu | Bütün büyüme eğrisi yanlış talebe dayanıyordu |
| **Yuvarlama kuralı tutarsızdı** | 10 Eylül 2026'da C# çekirdeğiyle karşılaştırınca bulundu. Python'un `round()` fonksiyonu bankacı yuvarlaması yapıyor: altıncı haftanın hafta sonu talebi tam 62,5 ve Python 62, C# 63 veriyordu | Ayrık kararlar artık iki tarafta da tamsayı aritmetiğiyle. Bkz. [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §2.3 |

Üçüncü hafta zararı da bu yüzden vardı. Düzeltilmiş modelde genişleme haftaları hâlâ zarar ediyor, ama artık aritmetik kaza değil, çözülen kira ve kadro yükünün sonucu.

**Para birimi:** isimsiz sikke ikonu, aşağıda ayrı bölüm var. Buradaki sayılar birimsizdir.

---

## 1. Başlangıç durumu

| Değer | Miktar |
|---|---|
| Başlangıç sermayesi | 8.000 |
| Masa | 4 |
| Personel | 1 aşçı |
| İtibar | 30 / 100 |
| Açık yemek | 6 |

---

## 2. Sabit giderler

Kira ve maaşlar **haftalık** ödenir, yedinci günün sonunda tek seferde.

### Kira ve genişleme, kademeye göre

<!-- ÜRETİLEN: kira -->
| Kademe | Masa | Haftalık kira | Bu kademeye geçiş bedeli |
|---|---|---|---|
| Başlangıç | 4 | 850 | — |
| İkinci | 7 | 1.950 | 2.500 |
| Üçüncü | 10 | 2.900 | 4.500 |
| Dördüncü | 14 | 5.000 | 8.000 |
<!-- /ÜRETİLEN: kira -->

Kiralar tahmin değil: her kademenin **olgun haftası** için hedeflenen net marjdan (%5, %9, %14, %20) geriye doğru çözüldü. `tools/balance/solve.py` bu işi yapıyor.

**Kural:** her genişleme kirayı da büyütür. Bu pazarlık konusu değil. Tavern Master'ın ekonomisinin çökme sebebi büyümenin sabit gideri artırmamasıydı.

### Maaşlar

| Rol | Günlük | Haftalık |
|---|---|---|
| Aşçı | 140 | 980 |
| Garson | 110 | 770 |
| Kasiyer | 100 | 700 |
| Bulaşıkçı | 90 | 630 |

Deneyimli personel yüzde 30'a kadar daha pahalı. Huylar ücreti etkilemiyor, sadece performansı.

### Genişleme maliyetleri

Yukarıdaki tabloda. Bedeller model tarafından, "her genişleme haftası zarar etmeli ve son genişlemeden sonra kasa 3.000'in altına inmeli" kısıtından çözüldü.

Son genişleme bilinçli olarak pahalı: 10.400 ödeyip kasayı 2.958'e indiriyorsunuz. Batma merdivenine bir adım kalıyor.

---

## 3. Malzeme ve yemek ekonomisi

**Temel kural: malzeme maliyeti satış fiyatının yaklaşık yüzde 32'si.** Yani brüt marj yüzde 68 civarında. Bu, gerçek restoran işletmeciliğine yakın bir oran.

### Fast food örnek kalemler

| Yemek | Satış | Malzeme | Marj |
|---|---|---|---|
| Hamburger | 45 | 15 | %67 |
| Patates Kızartması | 20 | 5 | %75 |
| Gazoz | 15 | 3 | %80 |
| Nugget | 25 | 8 | %68 |
| Dondurma | 18 | 5 | %72 |

**Kombo:** hamburger artı patates artı gazoz tek tek 80 eder. Kombo fiyatı 65. Malzeme 23.

- Tek satış kârı: 80 - 23 = 57, ama müşteri genelde sadece burger alır, yani 30.
- Kombo kârı: 65 - 23 = 42.

Kombo, ortalama fiş tutarını 45'ten 65'e çıkarıyor. Karşılığında mutfak üç kalem hazırlıyor, yani yük artıyor. İmza mekaniğinin takası bu.

### Türk mutfağı örnek kalemler

| Yemek | Satış | Malzeme | Marj |
|---|---|---|---|
| Kuru Fasulye | 55 | 18 | %67 |
| Pirinç Pilavı | 25 | 6 | %76 |
| Mercimek Çorbası | 30 | 8 | %73 |
| Köfte | 70 | 26 | %63 |
| Ayran | 15 | 5 | %67 |
| Sütlaç | 25 | 8 | %68 |

**Tipik sipariş:** sulu yemek artı pilav artı ayran. Satış 95, malzeme 29, kâr 66.

**Günün yemeği:** o günkü sulu yemek 55 yerine 45'e satılır. Marj düşer ama düzenli müşteri sadakati artar.

**Çay ikramı:** porsiyon başına 2 maliyet. Bedava verilir. Karşılığında sadakat ve veresiye geri dönüş oranı yükselir.

### Fiyat dalgalanması

- Malzeme fiyatları her gün taban fiyatın **yüzde 25 altı ile üstü** arasında dalgalanır.
- Mevsim kayması: bazı malzemeler bir mevsim boyunca yüzde 20 ucuz veya pahalı olur.
- Erken alım avantajı yok, stok bozuluyor. Ucuz güne denk gelmek şans değil, takip meselesi.

### Bozulma

| Mutfak | Bozulabilir kalem oranı |
|---|---|
| Fast food | %20 |
| Türk mutfağı | %60 |
| İtalyan | %70 |
| Japon | %85 |

Bozulabilir malzeme günü kapatınca değerinin tamamını kaybeder. Bu oran, mutfakların risk profilini ayıran şeylerden biri.

---

## 4. Kredi

| Tutar | Geri ödeme | Haftalık taksit | Süre |
|---|---|---|---|
| 5.000 | 6.750 | 844 | 8 hafta |
| 10.000 | 13.500 | 1.688 | 8 hafta |
| 20.000 | 27.000 | 3.375 | 8 hafta |

Geri ödeme toplamı anaparanın 1,35 katı. Kredi hızlı büyümeyi mümkün kılıyor ama haftalık gideri kalıcı olarak artırıyor.

Taksit ödenemezse batma merdiveni işlemeye başlar.

---

## 5. Müşteri formülleri

### 5.1 Günlük müşteri sayısı

```
temel     = masa_sayısı × 4
müşteri   = temel × (0,5 + itibar / 100) × gün_katsayısı
```

<!-- ÜRETİLEN: talep -->
| Durum | Hesap | Hafta içi | Hafta sonu |
|---|---|---|---|
| 4 masa, itibar 35 | 4 × 4 × 0.85 | 14 | 17 |
| 7 masa, itibar 52 | 7 × 4 × 1.02 | 29 | 36 |
| 10 masa, itibar 68 | 10 × 4 × 1.18 | 47 | 59 |
| 14 masa, itibar 88 | 14 × 4 × 1.38 | 77 | 97 |
<!-- /ÜRETİLEN: talep -->

`gün_katsayısı` hafta içi 1,0 ve hafta sonu 1,25. Mevsim de hafif oynatır.

**Kadro hafta sonu sütununa göre kurulur, ücret yedi gün ödenir.** Bu, personel giderinin neden ciroya göre yüksek durduğunu açıklıyor ve bilinçli: zirveyi karşılayamayan restoran itibar kaybediyor.

**İtibar hem tavanı hem tabanı belirliyor.** Mekânı büyütüp itibarı ihmal etmek masaları boş bırakıyor.

### 5.2 Sabır

Sabır saniye cinsinden ve servis süresi içinde tükeniyor. Bir servis yaklaşık 120 saniye sürüyor.

| Arketip örneği | Sabır |
|---|---|
| Kurye | 8 sn |
| Aceleci öğrenci | 10 sn |
| Öğle molası çalışanı | 12 sn |
| Ofis grubu | 18 sn |
| Aile | 30 sn |
| Emekli | 40 sn |

Sabır; oturmayı, siparişin alınmasını ve yemeğin gelmesini beklerken azalır. Sıfıra inerse müşteri çıkıp gider ve itibarı sert düşürür.

### 5.3 Fiyat duyarlılığı

```
fiyat_cezası = (fiyat / piyasa_fiyatı - 1) × 100 × duyarlılık
```

`duyarlılık` katsayısı arketipe göre 0,4 ile 2,5 arasında.

| Fiyat | Toleranslı (0,4) | Ortalama (1,0) | Pazarlıkçı (2,5) |
|---|---|---|---|
| Piyasa fiyatı | 0 | 0 | 0 |
| %10 üstü | -4 | -10 | -25 |
| %20 üstü | -8 | -20 | -50 |
| %30 üstü | -12 | -30 | -75 |

Piyasanın yüzde 15'inden fazla altına inmek de işe yaramıyor: memnuniyet artmıyor, sadece marj eriyor.

### 5.4 Memnuniyet

Her müşteri 100 puanla başlar.

| Etken | Değişim |
|---|---|
| Bekleme | `- (beklenen / sabır) × 60` |
| Fiyat | Yukarıdaki formül |
| Düşük kaliteli malzeme | -15 |
| Yüksek kaliteli malzeme | +10 |
| İstediği yemek tükendi | -30 |
| Çay veya özür ikramı | +15 |
| Patron bizzat ilgilendi | +20 |

Memnuniyet 60'ın üstündeyse müşteri memnun ayrılır, altındaysa şikayet eder.

### 5.5 İtibar

```
günlük_değişim = Σ (memnuniyet - 60) × itibar_ağırlığı / 100
```

| Örnek | Sonuç |
|---|---|
| 40 müşteri, ortalama 80 memnuniyet | +8 |
| 40 müşteri, ortalama 50 memnuniyet | -4 |
| Yemek eleştirmeni, 90 memnuniyet, ağırlık 8 | +2,4 tek başına |

İtibar 0 ile 100 arasında, 30'dan başlıyor. Her gün doğal olarak 0,3 puan düşüyor, yani ihmal ederseniz eriyor.

### 5.6 Arketiplerin saat dağılımı

Servis günü dört dilime ayrılıyor. Mutfakların ritmi burada somutlaşıyor.

| Mutfak | Açılış | Öğle zirvesi | Öğleden sonra | Akşam |
|---|---|---|---|---|
| Fast food | %15 | %35 | %15 | %35 |
| Türk mutfağı | %10 | %60 | %20 | %10 |
| İtalyan | %5 | %20 | %10 | %65 |
| Japon ramen | %15 | %50 | %15 | %20 |

Lokantada müşterilerin yüzde altmışı tek dilimde geliyor. Bu, öğle zirvesini gerçek bir kriz anına çeviriyor ve akşamı boş bırakıyor. İtalyan'da tam tersi.

---

## 6. Hedeflenen büyüme eğrisi

Bu, iyi oynayan bir oyuncunun izlemesi beklenen yol. **Doğrulanmadı.**

<!-- ÜRETİLEN: buyume -->
| Hafta | Masa | Kadro | Tavan | İtibar | Müşteri/gün (içi / sonu) | Ort. fiş | Ciro | Malzeme | Maaş | Kira | Genişleme | Haftalık net | Kasa |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 4 | 1 | 3 | 35 | 14 / 17 | 50 | 3.640 | −1.165 | −980 | −850 | — | **+645** | 8.645 |
| 2 | 4 | 2 | 3 | 45 | 15 / 19 | 52 | 4.113 | −1.316 | −1.734 | −850 | — | **+213** | 8.858 |
| 3 | 7 | 4 | 5 | 52 | 29 / 36 | 56 | 8.506 | −2.722 | −3.544 | −1.950 | −2.500 | **−2.210** | 6.648 |
| 4 | 7 | 4 | 5 | 60 | 31 / 39 | 58 | 9.460 | −3.027 | −3.622 | −1.950 | — | **+860** | 7.508 |
| 5 | 10 | 6 | 8 | 68 | 47 / 59 | 63 | 15.567 | −4.981 | −5.266 | −2.900 | −4.500 | **−2.081** | 5.427 |
| 6 | 10 | 7 | 8 | 75 | 50 / 63 | 66 | 17.371 | −5.559 | −6.475 | −2.900 | — | **+2.438** | 7.865 |
| 7 | 14 | 10 | 12 | 82 | 74 / 92 | 70 | 27.146 | −8.687 | −9.367 | −5.000 | −8.000 | **−3.908** | 3.957 |
| 8 | 14 | 10 | 12 | 88 | 77 / 97 | 75 | 30.398 | −9.727 | −9.573 | −5.000 | — | **+6.097** | 10.054 |
<!-- /ÜRETİLEN: buyume -->

**Personel sayıları kapasite modelinden geliyor**, tahmin değil. Bkz. [14-personel-sistemi.md](14-personel-sistemi.md).

| Hafta | Ne oluyor |
|---|---|
| 1 | Tek aşçı, patron salonda. Marj %11,7 — oyunun tek rahat haftası ve öğreticinin olumlu doruğu |
| 2 | İlk işe alım. Marj %5,3'e düşüyor. Ders: personel bedava değil |
| 3 | Birinci genişleme. Kadro ikiye katlanıyor, hafta 2.881 zararla kapanıyor |
| 4 | Kadro oturuyor, marj %9 |
| 5 | İkinci genişleme. En sert hafta: 3.912 zarar, kasa 3.342'ye iniyor |
| 6 | Nefes. Marj %14 |
| 7 | Son genişleme. Kasa 2.958, batma merdivenine bir adım |
| 8 | Marj %20. Büyümenin karşılığı burada alınıyor |

**Ortalama fiş tutarı da büyüyor:** 50'den 75'e. Menü genişledikçe pahalı yemekler açılıyor ve kombo gibi mekanikler devreye giriyor. Gizli bir büyüme kaldıracı.

**Tasarım niyeti:** net marj birinci haftada %11,7, sekizinci haftada %20,0. Büyümek kazandırıyor ama iki kat, on bir kat değil. Kadro ve kira aynı anda büyüdüğü için gelir artışının çoğu geri gidiyor.

**Üç genişleme haftasının üçü de zarar ediyor.** −2.881, −3.912, −3.846. Genişlerken kira ve kadro hemen büyüyor, itibar ise henüz yetişmemiş oluyor. Büyümek anında ödüllendirmiyor, önce bedelini ödetiyor. Bu artık aritmetik kaza değil, modele kısıt olarak yazıldı.

**Son genişleme bilinçli bir kumar.** 10.400 ödeyip kasayı 2.958'e indiriyorsun. Karşılığında yıl sonunda çok daha yüksek puan alıyorsun.

**Kadro 1'den 11'e çıkıyor**, müşteri ise 14'ten 77'ye. Kadro müşteriden hızlı büyüyor. Restoranı büyütmenin bedeli bu.

---

## 7. Ekonominin önemsizleşmemesi için

Araştırmadaki en büyük ikinci ölüm sebebi buydu. Üç önlem:

1. **Her genişleme kirayı büyütüyor.** Gelir artıyor ama gider de artıyor.
2. **Son kademe ekipmanlar pahalı.** 8.000 ile 12.000 arası. Sekizinci haftada bile bir şey için biriktiriyorsun.
3. **Yıl sonu puanı net varlığa bakıyor.** Para biriktirmenin her zaman bir sebebi var.

Denge aracının ölçmesi gereken ilk şey şu: **kaçıncı haftada oyuncu "artık para sorun değil" diyor?** O hafta 8'den önceyse ekonomi çökmüş demektir.

---

## 7.5 Para birimi: karar bekliyor

Gerçek para birimi kullanmayacağız. Üç sebebi var ve üçüncüsü belirleyici.

**1. Enflasyon çapası.** Bugün makul görünen bir fiyat iki yıl sonra saçma görünür. Oyun eskir.

**2. Yerelleştirme.** ₺ Türkçe sürümde doğru, İngilizce sürümde yabancı. $ tersi. Her ikisi de bir kesimi dışarıda bırakır.

**3. Gerçek parayla karışma riski.** Belirleyici olan bu. Oyunda gerçek parayla yapılan satın almalar var, mutfak kilidi açma. Oyun içi para gerçek bir para birimi sembolü taşırsa oyuncu ikisini karıştırır.

Bu teorik bir risk değil. Araştırmamızda Good Pizza, Great Pizza tam olarak bundan eleştirilmişti: oyun içi para banknot ikonuyla gösteriliyor, mağaza satın almaları ise dolar işareti taşımıyor, ve oyuncular ilk başta hangisinin gerçek para olduğunu ayırt edemiyor. Mağaza kuralları da sanal paranın gerçek paradan net ayrılmasını istiyor.

### Diğer oyunlar ne yapıyor

| Yaklaşım | Örnekler | Değerlendirme |
|---|---|---|
| **Kurgusal isim** | Animal Crossing "Bell", The Sims "Simoleon", Zelda "Rupee" | Akılda kalır, marka değeri taşır, enflasyona bağışık |
| **Jenerik altın veya jeton** | Stardew Valley "g", PlateUp jeton | Görünmez, sürtünmesiz, herkes anlar |
| **Gerçek para birimi** | Supermarket Simulator, çoğu gerçekçi simülasyon | Daldırıcı ama eskir ve karışır |

Kurgusal para birimlerinin bir kuralı var: **çevrilmezler.** Bell her dilde Bell kalır. İsim seçilirse Türkçe ve İngilizce sürümde aynı kalmalı.

### Bizim avantajımız: tek para birimi

Çoğu mobil oyunda iki para birimi vardır. Yumuşak para oyunla kazanılır, sert para gerçek parayla alınır. Karışıklık çoğunlukla buradan doğar.

**Bizde sert para yok.** Gelir modeli tek seferlik mutfak satın alması, yani doğrudan satın alma. Oyun içinde tek bir para birimi var ve hiçbir zaman gerçek parayla satılmıyor.

Bu tek başına Good Pizza'nın düştüğü tuzağı ortadan kaldırıyor ve "enerji yok, sayaç yok, ikinci para birimi yok" mesajına bir madde daha ekliyor.

### Dolar neden çözüm değil

"Küresel oyun yapıyoruz, o zaman dolar kullanalım" mantığı sezgisel olarak doğru duruyor ama iki yerde ters çalışıyor.

**1. Dolar küresel değil, Amerikan.**

App Store ve Google Play, gerçek fiyatları oyuncunun kendi para biriminde gösterir. Alman oyuncu €, Japon ¥, Türk ₺ görür. Oyun içinde $ yazarsak, oyuncunun gerçekte ödediği para biriminden farklı bir sembol göstermiş oluruz.

Yani dolar, dünyanın çoğunluğu için zaten yanlış sembol. Anlaşılırlık kazanmıyoruz.

**2. Dolar, karışma riskini azaltmıyor, en üst seviyeye çıkarıyor.**

Sorun anlaşılırlık değildi, gerçek parayla karışmaktı. Ve bu risk dolarda ₺'den daha büyük.

Oyun kasasında "$8.000" yazarken mağazada Türk mutfağı "$4,99" ise, aynı sembol tamamen farklı iki şeyi gösteriyor demektir. ₺ kullansaydık en azından Türkçe konuşmayan oyuncu için uyumsuzluk görünür olurdu. Dolarda uyumsuzluk görünmez ve tamdır.

Mağaza kuralları da sanal paranın gerçek paradan net ayrılmasını istiyor. Gerçek işlemin sembolünü sanal paraya vermek, bu ayrımı yapmanın en zor yolu.

### Asıl küresel olan şey: sikke ikonu

Sıfır dil, sıfır para birimi, herkes okur. Stardew Valley'nin "g"si ve Animal Crossing'in çan ikonu bu yüzden var.

Anlaşılırlık hiçbir zaman engel olmadı. Oyunlar otuz yıldır kurgusal para birimleriyle küresel olarak satılıyor. Kimse Bell'in ne olduğunu sormuyor, iki dakikada öğreniyor.

### Seçenekler

| Aday | Küresel okunabilirlik | Kimlik | Karışma riski |
|---|---|---|---|
| **İsimsiz sikke ikonu** | En yüksek | Yok | Yok |
| **Mangır** artı sikke ikonu | Yüksek | En yüksek, esnaf lokantasına çok uygun | Yok |
| **Akçe** artı sikke ikonu | Yüksek | Orta, nötr tınlıyor | Yok |
| Jeton | Yüksek | Düşük | Yok |
| $ veya ₺ | Yanıltıcı | Yok | **Yüksek** |

**Ek karar:** hangi isim seçilirse seçilsin, dar arayüz alanlarında metin yerine küçük bir sikke ikonu kullanılacak. İsim tam haliyle ipuçlarında ve gün sonu hesabında görünecek.

### ✅ Karar: isimsiz sikke ikonu

9 Eylül 2026'da karar verildi. Para birimi adlandırılmıyor, gerçek para birimi sembolü kullanılmıyor. Ekranda küçük bir sikke ikonu ve yanında sayı görünüyor.

**İkon şablonu: B, düz sikke yığını.** ✅ Karar verildi 9 Eylül 2026. Yedi aday çizildi ve gerçek boyutlarda karşılaştırıldı: https://claude.ai/code/artifact/a90e6a88-de82-429b-b03c-f5552451a8f1

| Aday | Değerlendirme |
|---|---|
| A · Düz sikke | Sade ve net ama daire tek başına "para" demiyor |
| **B · Sikke yığını, düz** | ✅ **Seçildi.** Basamaklı siluet küçük boyutta okunuyor, en sade form |
| C · Eğik sikke | Low-poly hacim hissiyle uyumlu |
| D · Sikke artı çatal | 16 pikselde çatal kayboluyor. Önerilmiyor |
| E · Sikke yığını, izometrik | Referansın izometrik tarzı. Değerlendirildi, düz olan tercih edildi |
| F · Banknot destesi, altın | Referansın dolardan arındırılmış hali. 16 pikselde zayıf |
| G · Referansın birebir hali | Önerilmiyor. Dolar çağrışımı ve renk çakışması |

**Referans görseli değerlendirmesi.** Kullanıcının paylaştığı görsel yeşil banknot destesiydi. Tarzı doğru: kalın form, düz renk, izometrik hacim. Ama iki sorunu var:

1. **Dolar çağrışımı.** Yeşil banknot ve oval portre penceresi Amerikan parası demek. Gerçek para sembollerinden kaçınma kararımızla çelişiyor.
2. **Renk çakışması.** Yeşil bizim ana arayüz rengimiz. Para yeşil olursa arayüze karışıyor, öne çıkmıyor.

Ek olarak dikdörtgen siluet 16 pikselde yatay bir lekeye dönüşüyor, yuvarlak siluet ise ayakta kalıyor.

**Çözüm:** tarzı koru, nesneyi değiştir. E adayı bunu yapıyor.

### İkon şartnamesi

| Kural | Karar |
|---|---|
| İsim | Yok. Hiçbir yerde adlandırılmıyor, çeviri gerekmiyor |
| Sembol | Gerçek para birimi sembolü yok. $, ₺, € kullanılmıyor |
| Renk | Sıcak altın. Arayüzün ana paletinden ayrı, sadece paraya ait |
| Sayı biçimi | Eş genişlikli rakam, binlik ayracı nokta. 8.240 gibi |
| Yerleşim | İkon solda, sayı sağda. Sıralama hiç değişmiyor |
| En küçük boyut | 16 piksel. Altında ikon kullanılmıyor, sadece sayı |
| Gerçek para | Mağaza ekranında bu ikon asla kullanılmıyor. Gerçek fiyatlar farklı renk ve yerleşimde |
| Üretim | Vektör tek dosya, Unity'de UI sprite |

---

### Yine de para birimi sembolü istenirse

Karar sembol yönünde olursa şu üç önlem zorunlu hale gelir:

1. Mağaza ekranında gerçek fiyatlar **hiçbir zaman** oyun içi para birimiyle aynı görsel dilde gösterilmez. Farklı renk, farklı ikon, farklı yerleşim.
2. Satın alma ekranında "gerçek para" ibaresi açıkça yazılır.
3. Kullanım şartlarında sanal paranın gerçek paraya çevrilemeyeceği belirtilir.

Bunlar zaten iyi uygulamalar ama sembol kullanılırsa pazarlık konusu olmaktan çıkarlar.

---

## 8. Denge aracının test edeceği sorular

1. Hiç müdahale etmeyen bir oyuncu kaçıncı günde batar
2. İyi oynayan bir oyuncu yılı hangi net varlıkla bitirir
3. Ekonomi kaçıncı haftada önemsizleşiyor
4. Fiyatı sürekli piyasa üstü tutan strateji kazanıyor mu
5. Hiç genişlemeyen bir oyuncu ne kadar kazanıyor
6. Kredi çekmek işe yarıyor mu, yoksa tuzak mı
7. Personel ne zaman kâra geçiyor
8. Altmış gün doğru uzunluk mu

---

## 8b. "Makul oyuncu 60 günde ne kazanmalı" — değerlendirildi, **değiştirilmedi**

Bir ekonomi incelemesi somut bir hedef bant önerdi: makul oyuncu 60. günü
**32.000–40.000** sikke ve **en az 10 masa** ile bitirsin, ve `calibrate.py`
bunu bir kontrol olarak koşsun. Ölçülen değerler 22–25.000 ve 7,2 masa;
`plancı` aynı ekonomide 13,7 masaya çıkıyor ve **benzer** kasayla bitiriyor.

**Öneri uygulanmadı ve sebebi şu: para bu oyunun hedefi değil.** [08](08-oyun-sonu.md)
kampanyayı **yedi eksenli bir plaketle** kapatıyor; servet onlardan yalnızca
biri. `makul` ile `plancı` aynı parayla bitiyor ama `plancı` **iki kat**
büyüklükte bir dükkân ve 99 itibar taşıyor — yıl sonu puanında aradaki fark
**mekân** ve **itibar** eksenlerinde görünüyor, kasada değil. Temkinli oynamak
"daha az para" değil "daha küçük plaket" demeli, ve öyle.

Kasaya mutlak bir bant koymak, puanlama sistemini kurarken bilinçli olarak
reddedilen şeyi geri getirirdi: tek eksenli bir başarı ölçüsü.

Önerinin **doğru** olan yarısı ayrıca uygulandı: yıl sonu **servet** ekseninin
paydası (`RemainingPurchaseCostTotal`) soğuk hava merdivenini saymıyordu, oysa
aynı soruyu soran öteki fonksiyon (`RemainingPurchaseCost`) sayıyordu — iki
fonksiyon "geriye ne satın alınacak kaldı" sorusuna iki farklı cevap veriyordu.
Şimdi ikisi de sayıyor.

---

## 8c. Açık kalan tek denge hedefi: geç kampanyada para sinki

`calibrate.py` **7,3. haftada** "Türk mutfağında plancı için para önemsizleşiyor"
diyor; hedef **8,0**. 32 tohumla doğrulandı, yani gürültü değil. Diğer bütün
kontroller ve `makul` oyuncu iki mutfakta da temiz.

**Sebebi biliniyor ve bilinçli bir düzeltmenin yan etkisi.** Ölçü şu:
`Cash > RemainingPurchaseCost()` — kasadaki para, geriye kalan bütün satın
alınabilirleri tek seferde ödeyebiliyor mu. Soğuk hava merdiveni üç
basamaktan ikiye indi ve katalog **8.000 sikke** küçüldü. Ama o basamak
**hiçbir koşuda satın alınamıyordu** (§8'deki ölçüm): eşik 8,0, alınamayan
bir kalemin üzerine kurulmuştu. Katalog küçülünce ölçü gerçeği gösterdi.

**Kapatmanın doğru yolu fiyat yükseltmek değil, satın alınacak şey eklemek.**
Fiyatları şişirmek ölçüyü yeşile çevirir ama oynanışta hiçbir şeyi
değiştirmez — plancı yine her şeyi alır, sadece bir hafta geç alır.

Eksik olan şey zaten adı konmuş: **iş yükseltmeleri**
(`content/upgrades.json`, [13](13-veri-semalari.md); erteleme gerekçesi
[06](06-plan-durumu.md)). Bugün bütün satın almalar **kapasite** satıyor —
ekipman, masa, depo — ve hiçbiri bir müşteriyi daha değerli yapmıyor. Tabela
gibi bir yükseltme hem o boşluğu doldurur hem de geç kampanyaya biriktirilecek
bir hedef koyar.

**Şu an kapatılmadı**, çünkü altıncı bir harcama ekseni eklemek dengeyi baştan
kalibre etmek demek ve oyun bu eksen olmadan **oynanabilir ve dengeli**. Açık
bir hedef ıskası olarak burada duruyor; kapatılınca `calibrate.py` kendiliğinden
yeşile döner.

---

## 8d. Ölçüm aracının referansı kendi eliyle sakat: kredi kapısı

Denge aracının bütün hedefleri `makul` oyuncuya göre ayarlı. O oyuncunun
genişleme kuralı şu:

```csharp
if (sim.Cash > cost * 2 && sim.ReputationCenti > 4500 && canServe
    && !sim.HasLoan)                      // <-- borcu varken HİÇ genişlemiyor
```

11 Eylül'de `kredisiz` stratejisi yazıldı ([12](12-ekonomi.md) §8'in altıncı
sorusunu ilk kez ölçmek için) ve şunu gösterdi:

| strateji | son kasa | masa | itibar |
|---|---:|---:|---:|
| `makul` | 25.424 | 7,4 | 75,8 |
| **`kredisiz`** (tek fark: kredi çekmiyor) | **27.853** | **14,0** | **100,0** |

Yani **kredi çekmek büyüme eğrisini sekiz hafta kapatıyor** — ve bunu yapan
ekonomi değil, botun kendi kuralı. `makul`, aracın "iyi oynayan oyuncu"
referansı; o referans kendi eliyle yarı boyutta kalıyor.

### Denendi, ölçüldü, **uygulanmadı**

Kapı "borcu var mı"dan "karşılayabiliyor mu"ya çevrildi:

| pay | sonuç |
|---|---|
| 4 taksit | `makul` 14 masa, **43.746** — `plancı`'yı (36.289) geçiyor |
| kalan borcun tamamı | `makul` 14 masa, **39.877** — yine geçiyor |
| kalibrasyon | ceza **9 → 32**, dört hedef daha kırılıyor |

Kırılanlar: `imzacı/makul` 0,84 ve 0,82 (taban 0,90), Türk büyüme çarpanı 4,20
(tavan 4,0), ve para artık `makul`'de de önemsizleşiyor.

**Sebebi açık: hedefler sakat referansa göre demirlenmiş.** Referansı düzeltmek
sabit noktayı, kiraları ve hedef bantlarını yeniden türetmeyi gerektiriyor —
yani tek satırlık bir düzeltme değil, tam bir yeniden ayarlama. Yarım ayarlanmış
bir denge, belgelenmiş bir kusurdan kötüdür; o yüzden kural **olduğu gibi
bırakıldı** ve gerekçesi `Strategies.cs`'te kuralın yanında duruyor.

### Kapatma sırası

1. Kapıyı "karşılayabiliyor mu"ya çevir (`cost * 2 + kalan borç`).
2. `calibrate.py` sabit noktayı yeniden arasın — `makul` artık daha güçlü,
   yani gerçekleşme oranı ve kiralar yukarı gidecek.
3. `imzacı/makul` bandı yeniden türetilsin: `makul` güçlenince imza mekaniği
   **tabanın** altına düşüyor, yani bant da o referansa aitti.
4. §8c'deki para sinki bu değişiklikle **daha da** belirginleşiyor (düzgün
   büyüyen oyuncu kataloğu daha erken bitiriyor) — ikisi birlikte ele alınmalı.

---

## 9. Kalan boşluk

- Ekipman ve yükseltme fiyat listesi tam yazılmadı
- Bahşiş sistemi olacak mı, karar verilmedi
- İtalyan ve Japon mutfaklarının kalem fiyatları yazılmadı, o mutfaklar güncelleme olarak geleceği için ertelendi
- Mevsim katsayılarının kesin değerleri yok
