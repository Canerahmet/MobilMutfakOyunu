# Ses dosyaları buraya

`Sfx.Init` bu klasöre **önce** bakıyor: burada `tik.ogg` varsa onu çalıyor,
yoksa `Sfx.cs` içindeki sentezlenmiş tona düşüyor. Yani dosya eklemek kod
değişikliği istemiyor ve klasör boşken oyun eksiksiz çalışıyor.

Dosya **adı** önemli, uzantı değil. Beklenen on ad:

    tik  onay  iptal  para  kapi-zili
    cizirti  dokme  kizgin  seviye  gun-donumu

Hangi sesin ne olması gerektiği ve lisans kuralı: `Art/ATIF.md` "Ses"
bölümü. Kısaca: yalnızca **CC0** ya da ticari kullanıma açık net lisanslı
kaynak, ve lisans metni hem `Art/<klasör>/License.txt` hem
`Resources/lisans/` altına.

Bu dosya derlemeye girmiyor (`.md` bir `AudioClip` değil), duruyor olması
zararsız.
