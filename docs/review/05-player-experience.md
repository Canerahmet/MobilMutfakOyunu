# Değerlendirme 5: Oyuncu Deneyimi

**Bakış açısı:** Mobil oyunlarda erişilebilirlik ve küçük ekran etkileşimi geçmişi olan UX lideri ve onboarding uzmanı
**Sorumlu kalemler:** A9, A10, A12
**Tarih:** 9 Eylül 2026

---

## Genel değerlendirme

Plan mobil için doğru refleksleri taşıyor: sayaç ve enerji yok, her an duraklatma, 44pt hedef, okumaya zorlayan modal yasağı, "her sesin görsel ikizi" kuralı. Ama kâğıttaki dört aşama altı inç telefonda üç yerde kırılıyor: Hal aşaması tek başına dokunuş bütçesini yakıyor ve 02 §2 ilke 2'deki "malzeme siparişi otomatikleşir" ile 02 §3'teki "ikinci döngü" hâlâ çelişiyor; servisin en kritik uyarısı yalnızca kırmızı renkle ve yalnızca sahne içinde yaşıyor, üstelik ücretsiz mutfağın paleti kırmızı-sarı (10); öğreticinin ilk personel dersi kapasite modeliyle (14) ve ekonomi tablosuyla (12 §6) çelişiyor. Süre hesabı da tam tutmuyor: aşama alt sınırlarının toplamı 3 dk 45 sn, üst sınırların 6 dk 30 sn, ve işe alım ile yükseltme ekranları (16, ekran 11-17) bunun dışında. Üç kalem de yapı olarak sağlam, ayrıntı düzeltilmeden onaylanmamalı.

## Bir günün dokunuş sayımı

Senaryo: üçüncü hafta, altı yemeklik menü, üç personel, üç müdahale, rapor, bir yükseltme. Dokümanlar bileşen düzeyini tanımlamadığı için varsayımlarım: miktar ve fiyat artı/eksi düğmesiyle ortalama iki dokunuş, kalite (02 §3 Aşama 1) kalem başına bir dokunuş, sürükleme bir dokunuş, altı yemek için yaklaşık on malzeme.

| Aşama | İşlem | Ham akış | Kalıcı varsayılanlarla* |
|---|---|---|---|
| Hal | 10 malzeme × (seç 1 + kalite 1 + miktar 2) | 40 | 6 |
| Tezgâh | 6 yemek seç, 6 fiyat × 2, 3 personel sürükle, başla | 22 | 5 |
| Servis | 3 müdahale × (masa 1 + seçenek 1), duraklat ve devam 2, kamera 2 | 10 | 10 |
| Hesap | rapor ileri, yorum kaydır, yükseltme ekranı, kategori, ürün, satın al, geri, yarın | 8 | 8 |
| **Toplam** | | **80** | **29** |

*Kalıcı varsayılan: dünkü menü, fiyatlar ve istasyon ataması aynen gelir; Hal'de "dünkü siparişi tekrarla" sepeti tek dokunuşla doldurur, oyuncu iki kalemi düzeltir. Bunların hiçbiri 16'da yazılı değil.

Sonuç: ham akış bütçeyi yüzde otuz üç aşıyor ve patlama noktası Hal. İkinci patlama Tezgâh: 32 yemeklik havuz (09) ve sekiz fiyat kaydırıcısıyla üçüncü mevsimde tek başına otuzu geçer. Bütçe ancak "dün ne yaptıysan bugün de o" varsayılanıyla tutuyor; bu prototipte ölçülecek bir şey değil, şimdi verilecek bir tasarım kararı. Ek not: fiyat için kaydırıcı değil piyasa ±%5 adımlı düğme şart; 12 §5.3'te yüzde on ile yüzde yirmi üstü arasındaki fark memnuniyette on puan, altı inçte kaydırıcı o hassasiyeti tutmaz.

## En riskli beş sorun

### 1. Hal bütçeyi tek başına yakıyor, "otomatik sipariş" çelişkisi açık

**Sorun.** 02 §2 ilke 2 "malzeme siparişi otomatikleşir" diyor; 02 §3 Aşama 1 aynı şeyi fiyat dalgalanması, kalite, bozulma ve tedarikçi ilişkisiyle günlük ana ekran yapıyor. 16 bütçeyi 40-60 diye sabitliyor ama Hal'in bileşenlerini hiç tanımlamıyor.

**Oyuncu ne yaşar.** Her sabah on kalemde küçük artı/eksi düğmelerine otuz kırk dokunuş. Onuncu günde Hal, Good Pizza'nın çok malzemeli siparişi gibi korku kaynağı olur; ilke 6'nın önlemek için yazıldığı şey.

**Düzeltme.** Sepet dünkü siparişle dolu gelsin, "eksiği tamamla" tek dokunuş olsun. Sipariş malzeme değil yemek porsiyonu üstünden verilsin, oyun malzemeye çevirsin. Kalite günlük tek seçim olsun. Sepet Hal'den çıkana kadar düzenlenebilsin. Hedef Hal için on dokunuş; 02 §2 ile §3 arasındaki çelişki metinde kapatılsın.

### 2. Sabır uyarısı yalnızca renk, yalnızca sahne içinde

**Sorun.** 17 "Olay sesleri": sabır kritik → "masa kenarı kırmızıya dönüyor". 10 "Dört mutfağın kimliği": fast food paleti "yüksek doygunluk, kırmızı ve sarı". Karakter 60-120 piksel, ekranda 6-20 kişi (10). Kamera kaydırma ve yakınlaştırma var (19 B7), yani masa ekran dışına çıkabilir. 16 üst çubuğunda ne müdahale hakkı ne uyarı var.

**Oyuncu ne yaşar.** Sessiz oynayan (17'nin kendi varsayımı) ve kırmızı-yeşil ayrımı zayıf oyuncu kırmızı-sarı dükkânda kırmızı masa kenarını görmez; kurye sekiz saniyede (12 §5.2) çıkar, itibar sert düşer, oyuncu neden kaybettiğini anlamaz. Ayrıca 16 ve 02 §7'deki "memnuniyet yüz ifadesi", 10'un "yüz o ölçekte okunmuyor" tespitiyle çelişiyor.

**Düzeltme.** Üç kanallı uyarı: masada renk artı şekil artı hareket (daralan halka), ekran kenarında ok, ve üst çubuğun altında en kötü üç masayı gösteren dokunulabilir "sabır kuyruğu" çipleri. Çipe dokunmak 19'daki Intervene eylemini tetiklesin; on dört masalık salonda 44pt hedef sorunu da böyle çözülür. Uyarı rengi mutfak paletine göre, paletteki en uzak ton olarak seçilsin. Memnuniyet göstergesi modelin yüzü değil, 24 piksel üstü bir arayüz glifi olsun ve yalnızca eşik altındaki masalarda görünsün. Titreşim ikizi eklensin (A12).

### 3. İlk personel dersi ekonomiyle çelişiyor; ilk on dakika baştan sona ceza

**Sorun.** 16 "Dakika dakika": 7-10. dakikada, yani dördüncü beşinci günde ilk personel. 14 "Gereken kadro": on üç müşteri için bir aşçı artı patron yeter. 12 §6: ilk iki hafta tek personel. 02 §6: personel bölümü 30-90 dakika. 09: istasyon dizilimi ikinci mevsim. Dört doküman dört zaman söylüyor. 8.000 sermayeyle (12 §1) yedinci gündeki 2.780'lik ilk fatura sınav değil; 12 §6'ya göre ilk gerçek sıkışma üçüncü hafta. "İlk üç gün batma yok" (16 kural 3) hiçbir şeyi korumuyor, çünkü merdivenin ilk kademesi haftalık ödemeye bağlı (02 §5) ve yedinci günden önce tetiklenemez.

**Oyuncu ne yaşar.** Oyun "personel al" dediği için garson alır, yedinci günde eksi 770 görür, kapasite kazanımını göremez çünkü müşteri zaten sığıyordu. Öğrendiği beş şeyin beşi de bedel: fiyat kızdırır, stok biter, personel para götürür, kira gelir. İlk on dakikada tek olumlu doruk yok. D1 riski tam burada; 21'in "1. gün tutundurma: öğretici işliyor mu" metriği bunu ölçecek ama geç.

**Düzeltme.** Dört dokümanı tek takvime bağlayın. İlk işe alımı oyuncunun gözle gördüğü ilk bekleme kaybının ertesi sabahına koyun (üçüncü gün stok dersinin kalıbı: önce acı, sonra araç) ve adayı çırak yapın (14: ücret eksi yüzde 25). Yedinci günü "ödeyebildiğin ilk fatura", üçüncü haftayı "ilk sınav" olarak yeniden adlandırın. Beşinci dakikadan önce bir olumlu olay ekleyin: yeni yemek açılışı (17'de kutlama sesi hazır) veya isimli düzenli müşterinin ilk gelişi. Batma korumasını kaldırın ya da "ilk kira gününe kadar" yapın. "Duraklat, bak, müdahale et" ikinci günde açıkça öğretilsin; tabloda hiç yok.

### 4. Servisin 120 saniyesi iki modlu; hız, sayaç ve süre göstergesi yok

**Sorun.** 02 §3 Aşama 3 günde 3-5 müdahale veriyor; 14 istasyon değişimini yasaklıyor, doğru. 12 §5.6'da Türk mutfağında müşterilerin yüzde altmışı tek dilimde geliyor. 16 üst çubuğunda kalan müdahale, günün neresinde olunduğu ve zirvenin ne zaman geleceği yok; hız kontrolü hiçbir dokümanda yok.

**Oyuncu ne yaşar.** İlk hafta on üç müşteri iki dakikaya yayılır: doksan saniye tek aşçıyı izler. Zirvede otuz saniyede üç masa aynı anda kızarır, müdahaleler biter, sonra yine izler. Ne "yapacak bir şey yok" ne "yetişemiyorum" tek başına; ikisi ardı ardına.

**Düzeltme.** 1x/2x hız düğmesi, atlama değil. Dört dilimi işaretleyen gün ilerleme çubuğu, zirve önceden görünsün. Üst çubukta müdahale hakkı noktaları. Sakin dilimlerde ücretsiz bir fiil: 12 §5.4'teki "patron bizzat ilgilendi +20" zaten formülde, servis fiili olarak görünür olsun. Servis ortasında kapanan uygulama duraklatılmış halde ve bir "devam" katmanıyla açılsın; 15 on saniyede bir kaydediyor ama dönüşün nasıl açıldığını yazmıyor, sekiz saniyelik sabıra dönmek adil değil.

### 5. Geri alma yok, geri tuşu ölü, dün yok

**Sorun.** 16 "Akış kuralları": aşamalar arasında geri yok, Hal kararı bağlayıcı, Android geri tuşu dört aşamada "hiçbir şey yapmaz". Modal yalnızca üç durumda; malzeme veya yükseltme harcaması bunlardan değil. 17 numaralı ekran yalnızca Hesap'tan açılıyor; müşterinin neden gittiği hiçbir yerde raporlanmıyor.

**Oyuncu ne yaşar.** 44pt düğmeye yanlış dokunuş, bozulacak yirmi balık demek (12 §3: bozulan malzeme değerinin tamamını kaybeder). Yanlış "Tezgâha geç" günü kilitler. Geri tuşunda hiçbir şey olmaması "bozuk" hissi verir. Ertesi gün açınca dünkü rapor yok, kaybedilen müşterinin sebebi yok. İlke 1, "her kararın görünür sonucu", sebep gösterilmeden yarım kalıyor.

**Düzeltme.** Aşama içinde onaylayana kadar düzenleme ve tek dokunuşla geri alma; bu aşama arası geri değil, kurala uyumlu. Aşama çıkış düğmesi toplamı göstersin, bir saniyelik modal olmayan "geri al" bildirimi versin. Geri tuşu her aşamada duraklatma ve ayarlar katmanını açsın, hiçbir zaman "hiçbir şey" yapmasın. Hal'de tek dokunuşla "Dün" kartı; 17 numaralı ekran 15 gibi "her yerden". Raporda kayıp müşteri satırı: bekleme, tükenme, fiyat.

## Kalem kararları

| Kalem | Karar | Gerekçe |
|---|---|---|
| A9 Ekran listesi ve akış | DÜZELT | On sekiz ekran ve modal yasağı doğru. Eksikler: aşama içi geri alma, geri tuşunun "hiçbir şey yapmaz" davranışı, "Dün" erişimi, aşamaya göre değişen üst çubuk (serviste müdahale hakkı ve gün ilerlemesi), kayıp sebebi. Listede olmayan yüzeyler: duraklatma menüsü, kredi (02 §5 kademe 2), ev sahibi mesajı ve ültimatom (02 §5 kademe 1 ve 4), tedarikçi ilişkisi (02 §3). |
| A10 Öğretici ve ilk on dakika | DÜZELT | Tempo doğru: günde bir kavram, tek cümle, yaparak. İçerik yanlış: ilk personel zamanı dört dokümanla çelişiyor, yedinci gün sınav değil, ilk üç gün koruması ölü kural, olumlu doruk yok, duraklatma öğretilmiyor. "İkinci oyunda öğretici gelmez" (16 kural 6) cihazı paylaşan veya haftalar sonra dönen oyuncu için yanlış; varsayılan kapalı ama yuva açılışında tek dokunuşla açılabilir olmalı. |
| A12 Ses tasarımı | DÜZELT | Beş katman, "her sesin işi var", katmanlı servis müziği ve sessizlik kullanımı doğru; dört parça ve 60-90 saniyelik döngü maliyeti makul. Düzeltme küçük ama zorunlu: görsel ikiz "renkten bağımsız ikiz" olmalı; kural sürekli katmanlara da uygulanmalı, kalabalık uğultusunun görsel ikizi yok; iki kritik olaya titreşim ikizi eklenmeli; "çoğu sessiz oynuyor" iddiası research/01'de geçmiyor, 21'in metrik listesine girmeli. |

## Cevapsız sorular

1. **Yatay mod kesin mi (16 soru 1)?** Lehine: mekân genişletme ve yerleşim düzenleme genişlik ister (02 §7), Steam ile aynı etkileşim (19 B7), tek kişilik ekip için tek yön. Aleyhine: üç dakikalık cep oturumu dikey ve tek el alışkanlığıdır, yatayda tek el neredeyse imkânsız, ve research/01'de yön konusunda hiç veri yok. Öneri: v1 yatay kalsın, ama Hal, Tezgâh ve Hesap'ta birincil eylemler sağ alt başparmak bölgesine sabitlensin, sol el ayarıyla aynalansın, ve ilk açılıştaki döndürme terk oranı 21'in metrik listesine eklensin. Çekirdek portların arkasında olduğu için (04) üç yönetim ekranı ileride dikeye taşınabilir; bu kapı kapatılmasın.
2. **Erişilebilirlik ayarları neyi kapsıyor?** 16'da ekran 5 "erişilebilirlik" diyor, hiçbir doküman içeriğini yazmıyor. Asgari liste: yazı boyutu ölçeği (20 C5 yalnızca aile ve Türkçe glifleri tanımlıyor, en küçük gövde boyutu yok), renk körü paleti, hareket azaltma (stok çubuğunun yanıp sönmesi ve parlama efektleri için statik alternatif, yanıp sönme üç hertz altı), titreşim aç/kapa, sol el aynalama, ve "sabır kritik olunca otomatik duraklat" seçeneği. Ekran okuyucu desteği Unity arayüzünde tek kişiyle v1'de gerçekçi değil; vaat etmek yerine "renkle tek başına bilgi yok, zamana bağlı tek girdi yok" tabanı yazılı hedef olmalı.
3. **Üst çubukta dört bilgi fazla mı (16 soru 4)?** Fazla değil, serviste yanlış. Kasa ve "Kiraya N gün" sabit; "Gün" ve "İtibar" yalnızca Hal, Tezgâh ve Hesap'ta; serviste yerlerini müdahale hakkı ve gün ilerlemesi alsın. Sikke "sıcak altın" (12 §7.5), Türk mutfağı paleti "pirinç" (10); üst çubuk sahnenin üstünde değil kendi opak plakasında durmalı.
4. **İşe alım havuzu üç günde bir yenileniyor (14).** Oyuncuya bunu kim söylüyor? Tezgâh'ta rozet yoksa oyuncu havuzu unutur.
5. **Hal "ikinci döngü" mü, otomasyon mu?** 02 §2 ilke 2 ile §3 Aşama 1 arasındaki bu cevap verilmeden dokunuş bütçesi hesaplanamaz.
