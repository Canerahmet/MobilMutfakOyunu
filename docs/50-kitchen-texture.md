# 50 — Mutfağın dokusu: duvar, zemin, dışarısı

*15 Eylül 2026.* İstek: *"restoranın genel dokusu ve arka planı, duvarlar vs.
üzerine çalışalım… internetten tipik bir fast food ve diğer mutfakların arka
plan görüntüsünü al, bunları bizim oyuna uygula."*

---

## 1. İnternetten görsel indirilmedi — ve indirilmemeliydi

İki sebep, ikisi de bağlayıcı:

**Lisans.** Kullanıcının kendi kuralı "varlık lisansları ticari yayına izin
vermeli" ve `tools/check_licenses.py` her varlığı atıf defterinde arıyor.
Telifli bir fotoğraf oyuna girseydi denetim kırmızı yanardı — doğru olarak.

**Teknik.** Low-poly bir sahnede fotoğraf dokusu yanlış durur: 40 derecelik
sabit kamerada duvar birkaç yüz piksel ve perspektifi tutmayan bir fotoğraf
"yapıştırılmış" görünür.

Üçüncü ve pratik bir sınır daha var: web aramasından **metin** dönüyor.
Görüntüyü gözle göremiyorum, dolayısıyla "şu görseli uygula" diyemem.

Bunun yerine internet **referans** olarak kullanıldı: o mekânların
karakteristik malzemeleri araştırıldı, görünüş kendi geometrimizle kuruldu.

### Araştırmanın verdiği ayrım

| | karakteristik yüzey |
|---|---|
| **Fast food** | dayanıklı ve silinir: paslanmaz çelik, laminat, seramik; nötr zemin üstünde cesur sıcak vurgu |
| **Esnaf lokantası** | *"ahşap lambri, sade duvar fayansı ve basit dekorasyon"*; rustik — ahşap, taş, metal aksesuar |

---

## 2. Paletin kimliğine dokunulmadı

Fast food'un duvarı **koyu** ve bu ilk bakışta araştırmayla çelişiyor (QSR'ler
açık nötr duvar kullanır). Koda bakınca sebebi çıktı — palet kullanıcının
kendi getirdiği referans görsellerden türetilmiş:

> *"Referansın birinci karesi tam olarak bu: kırmızı tabela, paslanmaz
> tezgâh, siyah-kırmızı zemin."*

Yani bu bilinçli bir karar, modern bir burger dükkânı. **Değiştirilmedi.**
Araştırma bir tasarım kararını bozmak için değil, eksik olanı tamamlamak için
kullanıldı.

---

## 3. Eksik olan: duvarın yüzeyi

Duvar tek düz kutuydu — gövde + süpürgelik + korniş. İki mutfak **aynı
duvarı farklı renkte** gösteriyordu, yani "başka bir yere girdim" hissinin
yarısı eksikti.

| | Türk lokantası | Fast food |
|---|---|---|
| yüzey | **ahşap lambri** 1,05 m | panel + çelik bant 1,15 m |
| üst kuşak | bakır | paslanmaz |
| düşey derz | 0,85 m (tahta) | 1,15 m (levha) |

Lambri yüksekliği keyfî değil: **oturan kişinin sırtı o hizada.** Lambri
gerçek hayatta da sandalye yüksekliğini korumak için var, süs değil.

Hepsi `Modeler` kutusu — yeni varlık yok, indirilen doku yok, atıf defterine
eklenen bir şey yok.

## 4. Dışarısı da mutfağa ait oldu

Arka plan gökyüzü yerine geçiyor ([19](19-technical-setup.md) doldurma
bütçesi) ve **iki mutfakta birebir aynıydı** — yani ayrım salonun dört
duvarında bitiyordu.

Artık mutfağın tonu karışıyor: fast food soğuk ve şehirli, Türk sıcak.
Ölçüldü, göz kararı değil:

```
Turk: (93,123,157) -> (116,130,144)     mavi dustu, kirmizi cikti
```

**Gece dokunulmadı.** Gecenin neredeyse siyah olması, "açık bir lokanta"
görüntüsünün karşıtlığını taşıyan şey; onu ton yüzünden aydınlatmak bütün
geceyi bozardı.

---

## 5. Tur mutfağı sabit seçiyordu

`Autopilot` mutfak seçim ekranında **her zaman 1'e** basıyordu (Türk). Yani
fast food'un duvarı, paleti, zemin deseni, dışarısının rengi — ve **kombo
düğmesi** — turun hiç görmediği şeylerdi.

`-Mutfak fastfood` bayrağı eklendi (varsayılan `turk`, yani mevcut koşular
birebir aynı kalıyor). İlk fast food koşusu anında kırmızı yaktı:

```
HATA: kirpilan yazi (gun 40): 2 - Hızlandır | Kombo kapalı
```

---

## 6. Kırpılma ölçümü: üç deneme, bir ders

Kareye bakınca "Kombo kapalı" **iki satıra sarmış ve eksiksiz okunuyordu**.
Yani hata oyunda değil, kontrolün kendisindeydi.

| deneme | sonuç |
|---|---|
| "saran etiketi yükseklikle ölç" | 1 → **7** yanlış alarm |
| "iki satıra sardıysa muaf" | 7 → 1 |
| **"yalnızca gerçekten kırpan kutuyu ölç"** | 0 |

Birinci deneme neden ters tepti: UI Toolkit'te sarma **varsayılan olarak
açık**, yani bütün etiketler yeni dala düştü ve "×4", "8.000 ¤" gibi apaçık
sığan yazılar kırmızı yandı.

Üçüncüde yamamayı bırakıp asıl soruyu sordum ve cevap şuydu: **UI Toolkit'te
`overflow` varsayılanı görünür** — yazı kutusunu aşsa bile çizilir, kesilmez.
Bütün arayüzde `Overflow.Hidden` yalnızca iki yerde var (bir simge kutusu, bir
ilerleme çubuğu) ve hiçbir yazı etiketinde yok.

Yani ölçüm, bu arayüzde **var olmayan bir hata biçimini** arıyordu ve
yalnızca yanlış alarm üretebilirdi. Türkçe etiketler tesadüfen dar kutulara
sığdığı için yıllarca patlamadı.

Kontrol artık yalnızca gerçekten kırpan bir kutunun içini ölçüyor: bugün
sessiz, ama biri bir yazı kabına `Overflow.Hidden` koyduğu gün konuşur. Asıl
koruma zaten iki komşu ölçümde — **üst üste binen düğme** (Türk mutfağındaki
gerçek hatayı o yakalamıştı) ve ekranın dışına taşan öge.

*Bir kontrolü üç kez yamamak, onun neyi ölçtüğünü hiç sormamış olmanın
bedelidir.*

### Yeşil bir özet yeterli değil

Ara koşulardan biri "140 geçti, 0 kaldı" dedi ve **hiçbir şey kanıtlamıyordu**:
fast food derlenememişti (`IResolvedStyle`'da `overflow` yok) ve Türk koşusu
`-SkipBuild` ile **eski ikiliyi** çalıştırmıştı. Özetin sonuna değil, çıktının
başındaki derleme satırına bakmak kurtardı.
