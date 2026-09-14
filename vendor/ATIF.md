# Üçüncü taraf varlıklar — kaynak ve lisans kaydı

Bu dosya **zorunlu**. Oyuna giren her dış dosyanın nereden geldiği ve
hangi lisansla kullanıldığı burada yazılı olmalı.

Sebebi ticari: yayın öncesi lisans gözden geçirmesi ancak böyle
yapılabilir. "Bunu nereden indirmiştik?" sorusunun cevabı olmayan bir
dosya, yayınlanamaz bir dosyadır.

## Kural

1. Her indirilen paket önce `vendor/` altına, **zip hâliyle** iner.
2. Zip'in içindeki `License.txt` **silinmez** ve projeye birlikte taşınır.
3. Bu tabloya bir satır eklenir: ne, nereden, hangi lisans, ne zaman.
4. Lisans CC0 / CC-BY / MIT dışında bir şeyse **kullanılmaz**, önce sorulur.

## Tablo

| Paket | Sürüm | Kaynak | Lisans | İndirme | Kullanım |
|---|---|---|---|---|---|
| Kenney Food Kit | 2.0 | https://kenney.nl/assets/food-kit | CC0 1.0 | 2026-09-11 | Tabak, yemek ve mutfak nesneleri |
| Kenney Furniture Kit | 2.0 | https://kenney.nl/assets/furniture-kit | CC0 1.0 | 2026-09-11 | Masa, sandalye, dolap, tezgâh |
| Kenney Mini Characters | 1.0 | https://kenney.nl/assets/mini-characters | CC0 1.0 | 2026-09-11 | Müşteri ve personel figürleri **ve 32 animasyon klibi** |
| Kenney Modular Characters | 1.0 | https://kenney.nl/assets/modular-characters | CC0 1.0 | 2026-09-11 | **KULLANILMIYOR** — paket 2B sprite çıktı, 3B gardırop değil |

## Yapıya motorun kendisinin soktuğu bileşenler

Bunlar indirilmedi; Unity yapı alırken APK'ya koyuyor. Yine de **dağıtılan
yazılım** oldukları için defterde yerleri var — denetimde bu tablo yoktu ve
APK'nın içindeki hiçbir üçüncü taraf bileşen kayıtlı değildi.

| Bileşen | Lisans | Nereden |
|---|---|---|
| Newtonsoft.Json | MIT (James Newton-King) | `com.unity.nuget.newtonsoft-json` |
| AndroidX (28 modül) | Apache-2.0 | Unity Android oynatıcısı |
| Kotlin stdlib + kotlinx-coroutines | Apache-2.0 | AndroidX bağımlılığı |
| libc++_shared | Apache-2.0 with LLVM Exception | Android NDK |
| Swappy (libswappywrapper) | Apache-2.0 | Unity kare hızı eşitleme |
| Unity çalışma zamanı (libunity, libil2cpp) | Unity Companion / EULA | Motor |

**Kural güncellendi:** yukarıdaki 1. maddede "CC0/CC-BY/MIT dışında bir şey
kullanılmaz" yazıyordu; Apache-2.0 zaten kaçınılmaz olarak içeride ve
ticari kullanıma açık. Kural artık **CC0 / CC-BY / MIT / Apache-2.0 /
SIL OFL** dışındaki lisanslar için geçerli.

## Animasyon

Karakter animasyonları **ayrı bir indirme değil** — Mini Characters
paketinin FBX'lerinin içinde geliyor. Her figürde 32 klip var; oyun
beşini kullanıyor: `idle`, `walk`, `sit`, `interact-right` (aşçı tezgâhta),
`holding-both` (garson tabak taşıyor).

İskelet bütün figürlerde aynı olduğu için klipler tek bir denetleyiciden
(`Art/Animator/Karakter.controller`) sürülüyor. Lisans paketin
kendisiyle aynı: CC0.

Quaternius veya Mixamo'ya **gerek kalmadı**; dışarıdan klip
indirilmedi.

## CC0 ne demek

Creative Commons Zero: kamu malına bırakılmış. Kişisel, eğitim ve
**ticari** kullanım serbest, atıf **zorunlu değil**. Kenney'in kendi
lisans metninden: *"You can use this content for personal, educational,
and commercial purposes."*

Atıf zorunlu olmasa da **yapılacak** — yapımcı ekranında Kenney'in adı
geçiyor. Zorunlu olmayan bir teşekkürü atlamak ucuzluk olur.

## Kendi ürettiklerimiz

Aşağıdakiler kodla üretiliyor, dış kaynak yok, lisans sorunu yok:

| Ne | Nerede |
|---|---|
| Bütün ses efektleri | `unity/Assets/Lokanta/Game/Sfx.cs` — dalga biçimi kodda sentezleniyor |
| Müzik | `unity/Assets/Lokanta/Game/Music.cs` — sürekli sentez, dosya yok |
| Kat planı ve oda geometrisi | `unity/Assets/Lokanta/Game/RoomPlan.cs` |
| Uygulama simgesi | `unity/Assets/Lokanta/Editor/IconShot.cs` — oyunun kendi sahnesinden render ediliyor (Kenney CC0 modelleri) |
| Açılış ekranı zemini | `IconShot.Apply()` — düz renk, görsel yok |
| Bütün arayüz | `unity/Assets/Lokanta/Game/Ui/` — UI Toolkit, kodla kuruluyor |
| Bütün içerik (yemek, malzeme, arketip, müşteri) | `tools/content/`, `tools/balance/` |

## Yazı tipi

| Paket | Sürüm | Kaynak | Lisans | İndirme | Kullanım |
|---|---|---|---|---|---|
| Rubik | değişken (wght) | https://github.com/google/fonts/tree/main/ofl/rubik | SIL OFL 1.1 | 2026-09-11 | Bütün arayüz yazısı |

**Telif satırı:** *Rubik — Copyright 2015 The Rubik Project Authors (https://github.com/googlefonts/rubik), SIL Open Font License 1.1.*
OFL hem lisans adını hem telif bildirimini istiyor; oyun içindeki
**Açık kaynak lisansları** ekranı ikisini de gösteriyor.

SIL Open Font License 1.1 ticari kullanıma açık; tek şart yazı tipini
**tek başına satmamak** ve türetilmiş bir yazı tipini aynı lisansla
dağıtmak. Oyunun içinde gömülü kullanım serbest.
Lisans metni: `unity/Assets/Lokanta/Art/Yazi/License.txt`.

**Neden ayrı bir yazı tipi indirildi:** Unity'nin varsayılan çalışma
zamanı teması editörde çalışıyor ama **yapıda yazı tipini çözemedi** —
ilk masaüstü yapısında düğmeler çiziliyor, üzerlerinde hiçbir yazı
görünmüyordu.

**Kapsama denetleniyor:** `python tools/art/check_font.py` oyunun
gösterebileceği her karakteri yazı tipiyle karşılaştırıyor. İlk
koşuda dört eksik buldu (₺, →, ≡, ★); üçü çizilen öğeye
dönüştürüldü, para işareti genel para işareti (¤) oldu.
