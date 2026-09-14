# 46 — Gönderdiğimiz ikili hiç koşmadı

*14 Eylül 2026.* Soru "oyunu simüle etmek için Android emülatörü kuralım mı"
diye başladı. Cevap hayır çıktı, ama sorunun **altındaki** boşluk gerçekti ve
emülatörün kapatacağından daha büyüktü.

---

## 1. Emülatör neden işe yaramıyor

Bugünkü APK bir emülatöre **kurulamaz**. Emülatör sistem görüntüleri x86_64;
kurulum `INSTALL_FAILED_NO_MATCHING_ABIS` ile düşer.

Bu, yapı betiğinin ne yazdığından değil **paketin kendisinden** okundu —
niyet ile çıktı bu projede daha önce ayrışmıştı:

```
$ aapt2 dump badging build/android/Lokanta.apk
package: name='com.ahmetakar.lokanta' versionCode='1' versionName='0.1.0'
minSdkVersion:'29'  targetSdkVersion:'36'
native-code: 'arm64-v8a'
```

`lib/` altında tek ABI var: `arm64-v8a`. Aynı döküm iki şeyi daha
doğruluyor: minSdk 29 ([19](19-teknik-kurulum.md)'un yazdığı taban) ve
izin listesinde **INTERNET yok** — yani [44](44-magaza-metinleri.md)'teki
gizlilik metninin "internet izni bile istemiyor" cümlesi bugünkü paket için
de doğru.

Yani emülatör kullanmak, **yayınlamadığımız ikinci bir yapıyı** derleyip onu
test etmek demek. Bu projenin tekrar eden dersinin tam da yasakladığı şey:
*yeşil bir tik yanlış şeyi ölçüyor olabilir.* Test edilen ikili gönderilen
ikili değilse, tik en baştan yanlış şeyi ölçüyor.

Emülatörün yapısal olarak veremediği üç şey daha var ve üçü de bu oyunda
tam isabet:

| | emülatör | gerçek telefon |
|---|---|---|
| ARM64 ikili | kurulmaz | çalışır |
| kare süresi / GPU | anlamsız | gerçek |
| ısınma (60 günlük kampanya) | yok | gerçek |
| iki parmak kamera | fare taklidi | gerçek |

`adb` zaten kurulu — Unity'nin Android modülüyle geliyor
(`…/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`). Yani gerçek
cihaz yolu **hiçbir indirme gerektirmiyor**, emülatör yolu ~3 GB istiyordu.

[tools/android/cihaz.ps1](../tools/android/cihaz.ps1) yazıldı: adb'yi sürümden
bağımsız buluyor, cihazın ABI'sini **kurulumdan önce** kontrol ediyor (o hata
mesajı sebebini söylemiyor), APK'yı kuruyor, çalıştırıyor, günlüğü ve ekran
görüntüsünü alıyor.

İki tuzak betiğin içine yazıldı:

- Günlük **temizleniyor** (`logcat -c`); yoksa eski koşunun çökmesi yeni
  koşununki gibi görünüyor.
- Hiç Unity satırı yoksa sonuç `OLCULEMEDI` — "hata yok" ile "uygulama hiç
  açılmadı" dışarıdan aynı görünüyor. Ekran görüntüsü de bunun için: süreç
  siyah ekranda da yaşıyor.

---

## 2. Asıl boşluk: dokunma değil, budayıcı

Emülatör sorusunu araştırırken `unity/Assets/link.xml` ortaya çıktı. Kendi
yorumu şunu yazıyordu:

> Editörde ve Windows yapısında (Mono, budama yok) hiçbir şey görünmez — yani
> bu dosyanın koruduğu hata, projedeki **HİÇBİR testin ulaşamadığı** tek
> yapılandırmada yaşıyor.

Koruduğu şey şu: Android `ManagedStrippingLevel.High` ile derleniyor, içerik
yükleme ise tamamen yansıma (`JsonConvert.DeserializeObject<T>`). Yüksek budama,
yalnızca yansımayla çağrılan üyeleri "kullanılmıyor" sayıp silebilir.

Yorumun **tahmini** şuydu: budanan şey DTO yazıcıları olur, belirti de çökme
değil sessiz varsayılan — bütün alanlar sıfır/null döner, doğrulama reddeder,
oyun açılışta hata ekranına düşer. (§4'te ölçüldü ve ikisi de yanlış çıktı;
tehlike gerçekti ama mekanizma başkaydı.)

Yorum doğruydu ve tam da bu yüzden sorunluydu: koruma **akıl yürütmeyle
yazılmış, hiç koşturulmamıştı.** Üstelik 13 Eylül'de alınan APK'yı da o güne
kadar kimse açmamıştı. *Koşmayan bir kontrol, geçen bir kontrolle aynı
görünüyor.*

---

## 3. Cihaz olmadan budayıcıyı koşturmak

Cihaz yokken de budayıcı koşturulabiliyor: Windows yapısı, Android'in
**derleyicisi ve budayıcısıyla** — IL2CPP + High.

```
.\tools\unity\tur.ps1 -Yapi windows-il2cpp
```

`BuildPlayer.WindowsIl2cpp` bunu kuruyor ve ayarı `finally` içinde geri
alıyor. Geri alma isteğe bağlı değil: ayar projede kalsaydı her günlük tur
pahalanır ve bunu kimse fark etmeden aylarca ödeyebilirdik.

Aynı olan ve önemli olan: aynı `link.xml`, aynı `Lokanta.Content` ve
`Newtonsoft.Json` derlemeleri, aynı yansımalı yükleme — yani **kendi
derlemelerimizin yönetilen budaması.** Aynı olmayan: motor modüllerinin
budanması platforma göre değişiyor. Bu bir Android testi değil, **budama
testi**.

### Yapının gerçekten IL2CPP olduğu doğrulandı

105 saniye IL2CPP için hızlıydı ve "koştu mu" sorusu tam da bu belgenin
konusu. Kanıt dosyalarda:

| | windows (Mono) | windows-il2cpp |
|---|---|---|
| `GameAssembly.dll` | yok | **43 MB** |
| `Lokanta_Data/il2cpp_data` | yok | **var** |
| `Lokanta_Data/Managed/` | dolu | **boş** |
| `MonoBleedingEdge/` | var | yok |

Budamanın High koştuğu ayrıca `ProjectSettings.asset`'ten okunuyor:
`managedStrippingLevel: Android: 3` (= High), ve geri alma sonrası
`Standalone: 0` (= kapalı) — yani `finally` gerçekten çalıştı.

---

## 4. Korumanın gerçekten yük taşıdığı ölçüldü

Tur geçti — ama bu tek başına hiçbir şey kanıtlamıyor. Budama koruma olmadan
da sorun çıkarmasaydı tur yine geçerdi ve `link.xml` gereksiz bir dosya
olurdu. İkisi dışarıdan aynı görünüyor.

Tek dürüst sınav **mutasyon**: `link.xml` geçici kaldırıldı, aynı yapı aynı
turla koşturuldu, ve kırılması **beklendi**.

| | `link.xml` var | `link.xml` yok |
|---|---|---|
| tur | **129 geçti, 0 kaldı** | özet bile üretemedi |
| çıkış | 0 | çökme |

Kök sebep oyuncu günlüğünün 31. satırında:

```
Icerik yuklenemedi: JsonSerializationException: Unable to find a constructor
to use for type Lokanta.Content.EconomyDto
```

Yani `link.xml` gerçekten yük taşıyor. O dosya olmasaydı Android yapısı,
**içeriğini yükleyemeyen** bir oyun olarak mağazaya giderdi ve bunu hiçbir
test yakalayamazdı.

### Ölçüm yorumu düzeltti

Beklenen belirti "sessiz varsayılan: bütün alanlar sıfır/null döner" idi.
Gerçek belirti daha sert: budanan şey özellik **yazıcıları** değil,
DTO'ların **kurucuları** — Newtonsoft nesneyi hiç kuramıyor.

Sonuç da tahminden kötü: oyun hata ekranına **varamıyor**. İçerik
yüklenemeyince yarım bir durumda devam ediyor, sekiz kontrol boyunca
"çalışmadı" yazıyor ve `NullReferenceException` ile ölüyor. Yani oyuncunun
göreceği şey açıklayıcı bir hata değil, açılışta donan bir uygulama.

Tehlike doğru tarif edilmişti, mekanizma yanlış. Yorum ölçülene uyduruldu —
tersi değil.

### Yan bulgu: çıkış çökmesi Mono'ya özgü olabilir

Mono turları uzun süredir tamamlandıktan **sonra** `0xC0000005` ile
kapanıyordu (yönetilen kodun dışında, tur sonucunu etkilemiyor). IL2CPP
yapısının iki tam koşusunun ikisi de **çıkış kodu 0** verdi.

İki koşu kanıt değil, ama ilk kez bir ipucu var: hata Mono çalışma zamanının
kapanışında olabilir — yani gönderilen ikiliyi (IL2CPP) hiç ilgilendirmiyor
olabilir. Daha fazlasını iddia etmek için daha çok koşu gerekiyor.

---

## 5. Bu neyi kapatmıyor

Budama artık ölçülüyor, ama **ARM64 ikilisi hâlâ hiç koşmadı**. Kalan ve
yalnızca gerçek cihazın verebileceği şeyler:

- ARM64 kod üretimi (IL2CPP'nin x86_64 ve ARM64 çıktısı aynı değil)
- kare süresi ve termal kısılma — 60 günlük bir kampanya uzun bir oturum
- dokunma: iki parmak kamera, 48 dp hedefler, gerçek DPI ve çentik
- Android yaşam döngüsü: arka plana atılıp öldürülen uygulamada kayıt
  bütünlüğü

Bunların hepsi `tools/android/cihaz.ps1` + bir telefon ile tek komut.

