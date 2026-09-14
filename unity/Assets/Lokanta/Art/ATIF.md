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

İki kayıt var ve **ikisi de doğru olmalı**: bu dosya ile `vendor/ATIF.md`. Bir süre ayrıştılar — burası Furniture Kit'i 1.0 diyordu (gerçek: 2.0, `Art/Mobilya/License.txt`) ve Modular Characters'ı kullanılıyor gösteriyordu, oysa o paket 2B sprite çıktığı için hiç kullanılmadı. Yayın öncesi varlık lisans denetimi ([21](../../../../docs/21-is-ve-yayin.md) D6) bu tabloya bakıyor; yanlış bir satır denetimi geçersiz kılar.

## CC0 ne demek

Creative Commons Zero: kamu malına bırakılmış. Kişisel, eğitim ve
**ticari** kullanım serbest, atıf **zorunlu değil**. Kenney'in kendi
lisans metninden: *"You can use this content for personal, educational,
and commercial purposes."*

Atıf zorunlu olmasa da **yapılacak** — yapımcı ekranında Kenney'in adı
geçiyor. Zorunlu olmayan bir teşekkürü atlamak ucuzluk olur.

## Ses: dosya yok, sentez var — ve yeri hazır

Bugün **hiç ses dosyası yok**; on ses efekti de `Sfx.cs` içinde
sentezleniyor. Bu bilinçli bir başlangıç ama **bitmiş hâli değil**: basit
dalga biçimleri bir lokanta oyununda ucuz duyuluyor, ve kapı zili, mutfak
cızırtısı, kalabalık uğultusu gibi sesler sentezle inandırıcı olmuyor.

`Sfx.Init` artık **önce dosyaya bakıyor**: `Resources/ses/<ad>` altında bir
klip varsa onu çalıyor, yoksa sentezlenmiş tona düşüyor. Yani ses dosyası
eklemek **kod değişikliği istemiyor** — klasöre koymak yetiyor, ve
konulmadığı sürece oyun eksiksiz çalışıyor.

Konulacak dosyalar (klasör: `unity/Assets/Lokanta/Resources/ses/`):

| Dosya adı | Ne zaman çalıyor | Ne aranıyor |
|---|---|---|
| `tik` | Her düğme | Çok kısa, yumuşak arayüz tıkı |
| `onay` | Servis açma, satın alma | Kısa olumlu iki ton |
| `iptal` | Reddedilen komut | Kısa olumsuz ton |
| `para` | Hesap ödendi | Kasa / madenî para |
| `kapi-zili` | Müşteri girdi | Dükkân kapı zili |
| `cizirti` | Mutfakta iş başladı | Izgara cızırtısı |
| `dokme` | İçecek hazırlandı | Sıvı doldurma |
| `kizgin` | Müşteri kızgın çıktı | Homurtu / olumsuz vurgu |
| `seviye` | Personel seviye atladı | Kısa başarı cümlesi |
| `gun-donumu` | Gün kapandı | Yumuşak, alçak geçiş |

Unity `.ogg`, `.wav` ve `.mp3` okuyor; **`.ogg` tercih edilmeli** (APK'da en
küçüğü). Uzantı önemli değil, dosya **adı** önemli.

**Lisans kuralı değişmiyor:** yalnızca **CC0** ya da ticari kullanıma açık,
atıf gerektirse bile net lisanslı kaynaklar. Görsellerde Kenney kullanıldı
ve Kenney'in **CC0 ses paketleri de var** (Interface Sounds, UI Audio,
Impact Sounds) — aynı lisans, aynı kaynak, aynı tutarlı üslup. Başka bir
kaynak kullanılırsa lisans metni `Art/<klasör>/License.txt` olarak eklenmeli
**ve** `Resources/lisans/` altına kopyalanmalı; oyun içi lisans ekranı
metnin kendisini gösteriyor, Kenney ve Rubik için yapıldığı gibi.

Müziğin henüz dosya yolu **yok**; aynı kalıp müziğe de uygulanabilir ama
önce ses efektleri.


## Kendi ürettiklerimiz

Aşağıdakiler kodla üretiliyor, dış kaynak yok, lisans sorunu yok:

| Ne | Nerede |
|---|---|
| Ses efektleri (dosya konulmadığı sürece) | `unity/Assets/Lokanta/Game/Sfx.cs` — dalga biçimi kodda sentezleniyor |
| Müzik | `unity/Assets/Lokanta/Game/Music.cs` — sürekli sentez, dosya yok |
| Kat planı ve oda geometrisi | `unity/Assets/Lokanta/Game/RoomPlan.cs` |
| Bütün arayüz | `unity/Assets/Lokanta/Game/Ui/` — UI Toolkit, kodla kuruluyor |
| Bütün içerik (yemek, malzeme, arketip, müşteri) | `tools/content/`, `tools/balance/` |

## Yazı tipi

**Rubik**, SIL Open Font License 1.1 — `Art/Yazi/Rubik.ttf`. Ticari
kullanıma açık ve Türkçe karakterleri tam (`tools/art/check_font.py`
oyundaki her metni tarayarak doğruluyor).

OFL, lisans metninin **ürünle birlikte dağıtılmasını** istiyor: metin
`Resources/lisans/rubik-ofl.txt` olarak derlemeye giriyor ve oyun içi lisans
ekranında **tam metin** okunabiliyor — "Rubik — SIL OFL 1.1" yazmak lisansı
karşılamıyor.

Bu bölüm bir süre "Unity'nin varsayılan teması, Liberation Sans, ayrı bir
yazı tipi indirilmedi" diyordu ve **yanlıştı**.

## Klasör eşlemesi (makine okur)

`tools/check_lisans.py` bu tabloyu okuyor. Art/ altındaki her varlık
klasörü burada bir satıra sahip olmalı; olmayan klasör denetimi kırmızıya
düşürür. "Gözle bakıldı" bir kez doğrudur — yeni bir klasör açıldığında
kimse yeniden bakmaz.

| Klasör | Paket | Lisans |
|---|---|---|
| Karakter | Kenney Mini Characters 1.0 | CC0 1.0 |
| Mobilya | Kenney Furniture Kit 2.0 | CC0 1.0 |
| Yemek | Kenney Food Kit 2.0 | CC0 1.0 |
| Yazi | Rubik (Hubert & Fischer) | SIL OFL 1.1 |
| Simge | Projenin kendi üretimi (Editor/IconShot.cs) | — |
