# Kayıt Sistemi

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** A11
**Durum:** Yazıldı, karar bekliyor

---

## Neden bu kadar önemli

Üç karar bu sistemi zorunlu kıldı:

1. **Mutfak kilidi.** Her kayıt bir mutfağa kilitli ve oyun bitene kadar değişmiyor.
2. **Birden fazla yuva şartı.** Tek yuva olsaydı ikinci mutfağı satın alan oyuncu mevcut oyununu silmek zorunda kalırdı. İade ve kötü yorum üretirdi.
3. **Mobil oturum.** Oyun her an kapanabilir. Telefon çalar, uygulama arka plana atılır, pil biter. Oyuncu hiçbir zaman ilerleme kaybetmemeli.

---

## Dört yuva

| Kural | Karar |
|---|---|
| Yuva sayısı | 4 |
| Yuva başına mutfak | Bir tane, oluşturulurken seçilir, sonra değişmez |
| Yuvalar arası ilişki | Yok, tamamen bağımsız |
| Silme | Serbest, ama onay ister |
| Aynı mutfaktan birden fazla yuva | Serbest |

Yuva ekranında her yuva şunları gösterir: mutfak, gün, kasa, itibar ve varsa kazanılmış plaket.

### Kilit ne zaman kesinleşiyor

Mutfak seçimi **üçüncü günün sonuna kadar** serbestçe sıfırlanabilir. Oyuncu yanlış seçim yaptığını yirmi dakika sonra anlarsa kapana kısılmıyor.

Üçüncü günden sonra kilit kalıcı. Değiştirmek için yeni yuva açmak gerekiyor.

---

## Ne kaydediliyor

Kayıt iki parçadan oluşuyor: **durum** ve **tohum**.

### Durum

| Grup | İçerik |
|---|---|
| Zaman | Gün numarası, mevsim, günün hangi aşaması |
| Para | Kasa, kredi bakiyesi, kalan taksit sayısı |
| İtibar | Mevcut değer, son yedi günün geçmişi |
| Mekân | Genişleme kademesi, yerleşim düzeni, sahip olunan ekipman ve yükseltmeler |
| Personel | Her çalışanın kimliği, rolü, huyları, morali, deneyimi, istasyonu |
| Stok | Malzeme miktarları, kalite kademeleri, tazelik sayaçları |
| Menü | Bugünkü menü, her yemeğin fiyatı, açılmış yemekler |
| Müşteriler | Düzenli müşteri ilerlemeleri, hikaye sahneleri, veresiye defteri |
| Batma | Merdivenin hangi kademesinde olduğu, varsa ültimatom geri sayımı |
| Servis | Servis ortasındaysa masaların ve siparişlerin anlık durumu |

### Tohum

Rastgelelik tohumu ve tüketilen adım sayısı. Çekirdek deterministik olduğu için **durum artı tohum, günü birebir yeniden üretmeye yeter.**

Bunun getirileri:
- Kayıt dosyası küçük kalıyor
- Hata ayıklarken bir gün tekrar oynatılabiliyor
- Platformlar arasında taşınabilir

---

## Ne zaman kaydediliyor

**Kural: her aşama geçişinde ve her anlamlı karardan sonra.**

| An | Kayıt |
|---|---|
| Hal aşaması bitti | Evet |
| Tezgâh aşaması bitti | Evet |
| Servis başladı | Evet |
| Servis sırasında, her 10 saniyede | Evet |
| Gün sonu hesabı kapandı | Evet |
| Satın alma yapıldı | Evet |
| Uygulama arka plana atıldı | Evet, hemen |

Servis sırasındaki periyodik kayıt sayesinde uygulama kapansa bile oyuncu en fazla on saniye kaybediyor.

**Manuel kayıt yok.** Oyuncu kaydetmeyi düşünmek zorunda kalmamalı. Mobilde manuel kayıt bir tasarım hatasıdır.

---

## Bozulmaya karşı koruma

Kayıt yazarken elektrik kesilmesi veya uygulamanın öldürülmesi gerçek bir risk. Üç önlem:

1. **Atomik yazma.** Önce geçici dosyaya yazılır, tamamlandığı doğrulanır, sonra asıl dosyanın üstüne taşınır. Yarım dosya asla oluşmaz.
2. **Bir önceki kaydın yedeği.** Her yuva iki dosya tutar: güncel ve bir önceki. Güncel okunamazsa öncekine dönülür ve oyuncu bilgilendirilir.
3. **Sağlama toplamı.** Her dosyanın sonunda içeriğin sağlaması bulunur. Uyuşmazsa dosya bozuk kabul edilir.

Kurtarma sırası: güncel dosya, sonra yedek, sonra hata mesajı. **Sessizce sıfırlanmış bir oyuna asla dönülmez.** Oyuncuya ne olduğu açıkça söylenir.

---

## Sürüm göçü

Oyun güncellendiğinde eski kayıtlar açılabilmeli. Bir mobil oyunda bu pazarlık konusu değil, çünkü oyuncu güncellemeyi seçmiyor.

```json
{
  "saveVersion": 3,
  "gameVersion": "0.4.1",
  "cuisine": "turk",
  "state": { }
}
```

**Kural:** her sürüm artışı için bir göç fonksiyonu yazılır. Göçler sırayla uygulanır, yani sürüm 1'den gelen kayıt 1→2 ve 2→3 fonksiyonlarından geçer.

**Göç fonksiyonları asla silinmez.** Beş sürüm sonra bile ilk sürümden gelen bir kayıt açılabilmeli.

**Yeni alanların varsayılanı olmalı.** Eski kayıtta olmayan bir alan eklenirse, göç fonksiyonu ona makul bir değer verir. Örneğin veresiye defteri eklendiğinde eski kayıtlar boş defterle devam eder.

---

## Bulut kaydı

Bulut kaydı bir port arayüzünün arkasında. Mobilde iCloud ve Google Play, Steam'de Steam Cloud.

| Kural | Karar |
|---|---|
| Ne zaman yüklenir | Gün sonunda ve uygulama arka plana atıldığında |
| Çakışma | Oyuncuya sorulur, iki kaydın günü ve kasası gösterilir |
| Otomatik birleştirme | Yok. Yönetim oyununda birleştirme yanlış sonuç üretir |
| Bulut kapalıysa | Oyun normal çalışır, sadece yerel kayıt |

Çakışmada otomatik seçim yapmamak bilinçli. Oyuncunun iki cihazda oynadığı senaryoda hangi ilerlemenin kaybedileceğine oyun karar vermemeli.

---

## Mimari yeri

Kayıt sistemi **çekirdeğe ait değil.** Çekirdek durumu üretir ve okur, ama dosyayı yazan uygulama katmanıdır. Dosyanın nereye yazıldığı ise port arayüzünün arkasındadır.

```
Çekirdek        durumu üretir, serileştirilebilir tutar
Uygulama        ne zaman kaydedileceğine karar verir, göçü uygular
ISaveStore      dosyayı nereye yazacağını bilir
ICloudSave      buluta yükler
```

Bu ayrım sayesinde denge aracı da kayıt yükleyip belli bir günden simülasyon başlatabilir.

---

## Karar bekleyen ayrıntılar

1. Dört yuva yeterli mi
2. Mutfak kilidinin serbest sıfırlanma süresi üç gün mü olmalı
3. Servis sırasındaki kayıt aralığı on saniye mi olmalı
4. Bulut çakışmasında oyuncuya sormak yerine daha ileri olanı seçmek daha mı iyi olur
