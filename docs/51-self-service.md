# 51 — Fast food self servis oldu

*15 Eylül 2026.* Kullanıcının önerisi:

> *"Fast food ve diğer mutfakları ayıran en büyük ayrım garson olabilir. Çünkü
> fast food restoranlarda genellikle self servis olur. Fast food için garson
> kısmını kaldıralım, temizlikçi olsun; o da insanların yemek yedikten sonra
> masada bıraktığı tabakları toplasın."*

Ve ardından:

> *"Fast food tarafına gelen müşteri sayısını artırıp baskıyı artırabiliriz,
> ayrıca fast food birim başına daha az kazanç modeline sahip olabilir —
> sonuç olarak gerçekte de fast food zincirler daha ucuz olur."*

İkisi de uygulandı. Bu belge **neyin ölçüldüğünü** ve iki mutfağın artık
nerede ayrıştığını yazıyor.

---

## 1. Akış: iki adım düştü

Müşteri durumu makinesi masa servisine göre kuruluydu:

```
masa bekliyor → sipariş bekliyor → yemek bekliyor → yiyor → ödeme bekliyor
```

Self serviste iki adım **personel işi olmaktan çıkıyor**:

| adım | masa servisi | self servis |
|---|---|---|
| sipariş | garson masaya gelir | tezgâhta (kasiyer) |
| **servis** | garson yemeği getirir | müşteri tepsisini taşır — **düştü** |
| **ödeme** | garson masadan alır | tezgâhta peşin — **düştü** |
| toplama | garson toplar | **temizlikçinin ana işi** |

**Muhasebeye dokunulmadı.** `CompletePayment` hâlâ çağrılıyor: memnuniyet,
itibar, müdavim kaydı, ciro ve **masanın kirli bırakılması** orada. Yani
temizlikçinin toplayacağı tepsi masada duruyor. Değişen tek şey, oyuncunun
bir garsonu masaya göndermesinin *gerekmemesi*.

## 2. Sayı: garson ücreti de düştü

Akışı değiştirmek tek başına **tutarsız** olurdu. `StaffingModel.Required`
küresel salon yükünü kullanıyordu (garson + bulaşıkçı + kasiyer = 73.581
mikro/müşteri), yani oyuncu **işi olmayan bir garsonun ücretini** ödemeye
devam ederdi ve oyun yanlış sebepten kolaylaşırdı.

Salon rolleri artık mutfağa ait (`cuisines/*.json: salonRoles`):

| | salon havuzu | mikro/müşteri |
|---|---|---:|
| fast food | kasiyer + bulaşıkçı | **35.119** |
| Türk | garson + bulaşıkçı + kasiyer | 73.581 |

Sonuç: `makul` botunun kadrosu **4,0 → 2,0**, maaşı **25.617 → 18.881**.

Ekranda da adı değişti: fast food'da salon çalışanı **"Temizlikçi"**.
Anahtarı `role.*` değil `ui.*` ailesinde — `role.*` adları içerikten geliyor
(`staff-roles.json: nameKey`) ve orada temizlikçi diye bir rol yok; seçilen
şey rolün kendisi değil oyuncuya **gösterilen ad**.

---

## 3. Hacim ve marj: vaat sayılarda yoktu

[44](44-store-texts.md) "fast food: düşük fiş, kalabalık" diye satıyor.
Ölçüm **tersini** gösterdi:

| | grup | fiş | brüt marj |
|---|---:|---:|---:|
| fastfood | 1945 | 56,7 | **%64** |
| turk | 1819 | 67,5 | %56 |

Yani ucuz diye satılan mutfak hem daha kârlıydı hem hacmi aynıydı.

İki kol da bağlandı:

- **Hacim** — `customerMultiplierBp` mutfağa ait ve **tek kapıdan** geçiyor
  (`ExpectedCustomers`), yani kadro önerisi, hal önerisi ve geliş planı aynı
  sayıyı görüyor. Fast food 13000 (+%30).
- **Marj** — `gen_dishes.py`'deki malzeme oranı hedefi fast food'da bandın
  üst ucuna çekildi. [12](12-economy.md) bandı **%28–36** yazıyor; uydurulan
  bir sayı yok, yazılı sınırın içinde kalındı.

Sonuç:

| | grup | fiş | kadro | kaybedilen |
|---|---:|---:|---:|---:|
| fastfood | **2597** | **52,5** | 5,0 | **16** |
| turk | 1819 | 67,5 | 4,0 | 6 |

Hacim +%43, fiş −%22, kaybedilen müşteri neredeyse üç katı. "Kalabalık,
düşük fiş, daha çok baskı" ilk kez sayılarda.

---

## 4. İki kez yanlış yaptım, ikisini de sayı yakaladı

**Sorulmamış enflasyon.** Bandı ikiye açarken Türk'ün malzeme oranını da
düşürdüm, yani marjını *yükselttim*: `makul` 17.351 → 22.257. İstenen şey
fast food'un birim kazancının düşmesiydi; Türk'e dokunulması istenmemişti.

**Paylaşılan grup.** Geri alınca Türk eski değerine **dönmedi** (16.009).
Sebep: iki mutfak `icecek` ve `tatli` gruplarını **paylaşıyor**, yani grup
başına tek bir hedef ikisini birden kaydırıyor. Hedef artık **mutfak + grup**
anahtarlı.

Doğrulama ölçütü harness sayısı değil, **dosyanın kendisi**:
`content/dishes/turk.json` değişmemiş dosyalar arasında — yalnızca
`fastfood.json` değişti.

---

## 5. Hacim, toplam kasa için bir kol değil

Çarpanı düşürünce fast food **zenginleşti**:

| çarpan | kasa | kadro | masa | kaybedilen |
|---:|---:|---:|---:|---:|
| 11500 | 23.386 | 3,3 | 7,8 | 10 |
| 12000 | 24.318 | 3,8 | 8,1 | 11 |
| **13000** | 22.473 | 5,0 | 10,3 | **16** |

Sebep: yüksek hacimde bot genişliyor, kira ve maaş artıyor, müşteri
kaybediyor; düşük hacimde dükkân yalın kalıp daha çok kâr ediyor. Yani
hacim **şekli** değiştiriyor, toplamı değil. En ayrışmış şekli verdiği için
13000'de kalındı.

---

## 6. Bir testin premisi çöktü — ve haklıydı

`PlateTests.Bulasikci_salonu_lavabodan_kurtariyor` fast food koşuyordu ve iki
kolu **birebir aynı** çıktı (32/32, tabaksız bekleme 0): self serviste salonun
işi yarıya indiği için tabak darboğazı artık oluşmuyor.

Testin kendi yorumu eskiden de böyle boş ölçtüğünü ve bunun bir kez
düzeltildiğini yazıyordu; bu değişiklik aynı boşluğu geri getirmişti.

Çare testi zayıflatmak değil **doğru yere taşımak** oldu: soru, garsonun hem
masaya hem lavaboya koştuğu **masa servisli** mutfakta anlamlı. Türk
içeriğine taşındı ve geçiyor.

---

## 7. Açık kalan denge sorusu

Fast food toplamda hâlâ önde: `makul` 22.473'e karşı Türk 17.351. Sebep
yapısal ve gerçekçi — fast food'un **zayiatı üçte bir** (5.487'ye 14.121) ve
self servis salon maliyetini yarıya indirdi. Gerçek hayatta da zincirler bu
yüzden ucuz işletilir.

Ama oyun ekonomisinde bu şu anlama geliyor: **ücretsiz/başlangıç mutfağı,
ücretli olandan daha çok kazandırıyor.** Ödül sıralaması ters.

Üç yol var ve hangisinin doğru olduğu bir tasarım kararı:

1. **Kabul et** — fast food kolay başlangıç, Türk farklı bir oyun (imza
   mekaniği, yüksek fiş, daha az baskı).
2. **Kirayı ayır** — gerçekte de zincirler yüksek trafikli, pahalı yerlerde
   oturur. Kira şu an kademeye bağlı ve mutfaktan bağımsız.
3. **Türk'ü güçlendir** — ama bu, §4'te geri aldığım sorulmamış enflasyonun
   kendisi olur; ancak istenirse yapılmalı.

Sayılar burada; karar kullanıcının.

---

## 8. Mekanik doğruydu, ekran yanlıştı — iki kez

Simülasyon değişikliği bittikten sonra **görünür tarafta iki gerçek kusur**
kaldı. İkisini de tur yakaladı ve ikisi de aynı sınıftan: *oyun doğru
çalışıyor, oyuncunun gördüğü yanlış.*

### "Temizlikçi" hiçbir yerde yazmıyordu

Personel kartı **adı** gösteriyor; rol adı yalnızca ad yoksa yedek olarak
çıkıyordu. Personelin adı olduğu için (Sevgi, Nurten…) "Temizlikçi" hiç
görünmüyordu — yani oyuncu salondaki kişinin garson **değil** temizlikçi
olduğunu öğrenemiyordu.

"Ekranda Temizlikçi yazıyor" diye yazmıştım ve **görmemiştim**. Rol adı artık
her kartta, huyun yanında: *"Temizlikçi · Huysuz"*.

### Bulaşık cümlesi olmayan bir rolden bahsediyordu

```
ui.staff.sink_none = "Kimse lavaboda değil — bulaşık birikince garson geçer"
```

Fast food'da garson yok. İki dilde de role-nötr yapıldı ("salondan biri" /
"someone from the floor").

**Bunu bulan şey, kontrolü iki yönlü yazmaktı:** doğru etiketin varlığını *ve*
yanlış olanın yokluğunu arıyor. Yalnızca birincisini sorsaydı geçerdi; ikincisi
olduğu için rol adıyla ilgisi olmayan bir cümlede saklanan tutarsızlığı
yakaladı.

---

## 9. Mağaza görüntüsü: barı düşürmemek

Self servis masa devrini hızlandırıyor (servis ve ödeme beklemesi yok), yani
aynı anda dolu masa sayısı düşüyor. Mağaza görüntüsünün kalite kapısı
("masaların yarısı dolu") bir koşuda **4/14**'te süreye takıldı.

Barı düşürmek cazipti ve **yanlış olurdu**: mağaza görselinin işi dolu bir
lokanta göstermek; "self serviste zaten boş olur" demek, görüntüyü oyunun en
sakin anına razı etmek olurdu.

Üstelik eşik ulaşılabilirdi — aynı yapıda başka koşular 8/14 ve 13/14 gördü.
Eksik olan bar değil **sabır**: arama 30 → 75 saniyeye çıkarıldı ve sonuç
**13/14** oldu.
