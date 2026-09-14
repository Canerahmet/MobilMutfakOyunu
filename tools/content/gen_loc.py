# -*- coding: utf-8 -*-
"""
content/loc/tr.json uretir: oyunun metin TABLOSU.

"Oyundaki butun metin" DEGIL ve oyle iddia etmemeli: yirmi bes kadar
arayuz dizesi hala kodun icinde gomulu ("Muedavimler", "Yukselt - ",
"Bekleyen masa yok", yapimci ekrani...). Onlar tek dilli bir surumde
calisiyor ama tabloda olmadiklari icin tek yerden gozden gecirilemiyor.

Font kapsamasi acisindan RISK YOK: tools/art/check_font.py
unity/Assets/Lokanta/Game altindaki .cs dosyalarini da tariyor, yani
kodda yazilan bir karakter de denetleniyor.

Iki kaynak birlestiriliyor:

  1. Icerigin istedigi anahtarlar. Her yemegin, malzemenin, arketipin,
     huyun, istasyonun bir nameKey'i var ve duzenli musterilerin ustune
     jobKey ve hikaye sahneleri geliyor. Bu arac icerigi TARAYIP hangi
     anahtarlarin gerektigini kendisi buluyor - elle liste tutmak,
     icerik degisince sessizce eksik metin birakir.

  2. Arayuz metinleri. Elle yazilmis, cunku onlar icerikte yok.

Dogrulama sert: icerigin istedigi bir anahtar eksikse ya da tabloda
icerigin istemedigi bir anahtar varsa arac HATA veriyor. Eksik metin,
oyunda "dish.hamburger" yazan bir dugme demek.

Calistirma:
    python tools/content/gen_loc.py
"""
from __future__ import print_function

import io
import json
import os
import sys

import loc_tarama

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.dirname(os.path.dirname(HERE))
CONTENT = os.path.join(ROOT, "content")
OUT = os.path.join(CONTENT, "loc", "tr.json")
OUT_EN = os.path.join(CONTENT, "loc", "en.json")

# Ingilizce tablo ayri bir modulde ama AYNI URETECTEN geciyor: asagidaki
# dogrulama iki tablonun anahtarlarinin ve bicimleme yer tutucularinin
# ayni oldugunu sart kosuyor. Ayri bir arac olsaydi iki tablo sessizce
# ayrisirdi - ve metinde ayrisma "[ui.staff.hire]" yazan bir dugme demek.
import loc_en  # noqa: E402

# ---------------------------------------------------------------------------
# Malzeme adlari
# ---------------------------------------------------------------------------
INGREDIENTS = {
    "tuz": "Tuz", "karabiber": "Karabiber", "zeytinyagi": "Zeytinyağı",
    "aycicek_yagi": "Ayçiçek Yağı", "un": "Un", "sogan": "Soğan",
    "sarimsak": "Sarımsak", "domates": "Domates", "seker": "Şeker",
    "sut": "Süt", "yumurta": "Yumurta", "tereyagi": "Tereyağı",
    "kiyma": "Kıyma", "tavuk_gogus": "Tavuk Göğsü", "tavuk_kanat": "Tavuk Kanadı",
    "doner_eti": "Döner Eti", "balik_filetosu": "Balık Filetosu",
    "sosis": "Sosis", "dana_kusbasi": "Dana Kuşbaşı",
    "kuzu_kusbasi": "Kuzu Kuşbaşı", "kuzu_pirzola_et": "Kuzu Pirzola",
    "iskembe": "İşkembe", "burger_ekmek": "Burger Ekmeği",
    "hotdog_ekmek": "Hot Dog Ekmeği", "tost_ekmegi": "Tost Ekmeği",
    "lavas": "Lavaş", "yufka": "Yufka", "makarna": "Makarna",
    "galeta_unu": "Galeta Unu", "misir_nisastasi": "Mısır Nişastası",
    "kabartma_tozu": "Kabartma Tozu", "maya": "Maya", "irmik": "İrmik",
    "kasar": "Kaşar", "mozzarella": "Mozzarella", "yogurt": "Yoğurt",
    "beyaz_peynir": "Beyaz Peynir", "dondurma_karisimi": "Dondurma Karışımı",
    "patates": "Patates", "marul": "Marul", "lahana": "Lahana",
    "havuc": "Havuç", "jalapeno": "Jalapeño", "tursu": "Turşu",
    "patlican": "Patlıcan", "yesil_biber": "Yeşil Biber", "kabak": "Kabak",
    "bamya": "Bamya", "taze_fasulye": "Taze Fasulye", "salatalik": "Salatalık",
    "maydanoz": "Maydanoz", "kuru_fasulye_tane": "Kuru Fasulye",
    "nohut_tane": "Nohut", "mercimek": "Mercimek", "bulgur": "Bulgur",
    "pirinc": "Pirinç", "burger_sos": "Burger Sosu", "acili_sos": "Acılı Sos",
    "ketcap": "Ketçap", "mayonez": "Mayonez", "salca": "Salça",
    "sirke": "Sirke", "baharat_karisimi": "Baharat Karışımı",
    "kirmizi_biber": "Kırmızı Biber", "kimyon": "Kimyon", "nane": "Nane",
    "tarcin": "Tarçın", "gazoz_surubu": "Gazoz Şurubu",
    "kola_surubu": "Kola Şurubu", "cay": "Çay", "limon": "Limon",
    "elma": "Elma", "cikolata": "Çikolata", "kakao": "Kakao",
    "ceviz": "Ceviz", "kadayif_tel": "Tel Kadayıf",
    "vejetaryen_kofte": "Vejetaryen Köfte",
}

# ---------------------------------------------------------------------------
# Yemek adlari
# ---------------------------------------------------------------------------
DISHES = {
    # fast food
    "hamburger": "Hamburger", "hot_dog": "Hot Dog", "cizburger": "Çizburger",
    "kasarli_tost": "Kaşarlı Tost", "tavuk_burger": "Tavuk Burger",
    "duble_burger": "Duble Burger", "acili_burger": "Acılı Burger",
    "crispy_tavuk": "Crispy Tavuk", "tavuk_durum": "Tavuk Dürüm",
    "balik_burger": "Balık Burger", "vejetaryen_burger": "Vejetaryen Burger",
    "et_durum": "Et Dürüm", "patates_kizartma": "Patates Kızartması",
    "nugget": "Nugget", "baharatli_patates": "Baharatlı Patates",
    "yesil_salata": "Yeşil Salata", "sogan_halkasi": "Soğan Halkası",
    "acili_kanat": "Acılı Kanat", "mozzarella_cubuk": "Mozzarella Çubuk",
    "coleslaw": "Coleslaw", "gazoz": "Gazoz", "kola": "Kola",
    "limonata": "Limonata", "milkshake": "Milkshake", "ayran": "Ayran",
    "buzlu_cay": "Buzlu Çay", "dondurma": "Dondurma",
    "elmali_turta": "Elmalı Turta", "cikolatali_kek": "Çikolatalı Kek",
    "donut": "Donut", "brownie": "Brownie", "waffle": "Waffle",
    # turk
    "kuru_fasulye": "Kuru Fasulye", "nohut": "Nohut",
    "etli_turlu": "Etli Türlü", "karniyarik": "Karnıyarık",
    "taze_fasulye": "Taze Fasulye", "imambayildi": "İmambayıldı",
    "musakka": "Musakka", "etli_bamya": "Etli Bamya",
    "patlican_kebabi": "Patlıcan Kebabı", "mercimek_corbasi": "Mercimek Çorbası",
    "ezogelin": "Ezogelin Çorbası", "yayla_corbasi": "Yayla Çorbası",
    "iskembe_corbasi": "İşkembe Çorbası", "pirinc_pilavi": "Pirinç Pilavı",
    "bulgur_pilavi": "Bulgur Pilavı", "borek": "Börek", "manti": "Mantı",
    "kofte": "Köfte", "tavuk_sis": "Tavuk Şiş", "adana": "Adana Kebap",
    "doner": "Döner", "iskender": "İskender", "kiymali_pide": "Kıymalı Pide",
    "lahmacun": "Lahmacun", "kuzu_pirzola": "Kuzu Pirzola",
    "coban_salata": "Çoban Salata", "cacik": "Cacık", "piyaz": "Piyaz",
    "sutlac": "Sütlaç", "kadayif": "Kadayıf", "revani": "Revani",
}

# ---------------------------------------------------------------------------
# Musteri arketipleri
# ---------------------------------------------------------------------------
ARCHETYPES = {
    "yalniz_musteri": "Yalnız Müşteri", "cift": "Çift", "aile": "Aile",
    "kurye": "Kurye", "cocuklu_ebeveyn": "Çocuklu Ebeveyn", "yolcu": "Yolcu",
    "paylasimci": "Paylaşımcı", "yemek_elestirmeni": "Yemek Eleştirmeni",
    "aceleci_ogrenci": "Aceleci Öğrenci", "ofis_grubu": "Ofis Grubu",
    "antrenman_sonrasi": "Antrenman Sonrası", "alisveris_molasi": "Alışveriş Molası",
    "pazarlikci": "Pazarlıkçı", "gec_saat_musterisi": "Geç Saat Müşterisi",
    "mac_grubu": "Maç Grubu", "diyet_yapan": "Diyet Yapan",
    "gece_vardiyasi": "Gece Vardiyası", "dogum_gunu_grubu": "Doğum Günü Grubu",
    "sikayetci_musteri": "Şikâyetçi Müşteri", "toplu_siparis": "Toplu Sipariş",
    "esnaf_komsu": "Esnaf Komşu", "ogle_molasi_calisani": "Öğle Molası Çalışanı",
    "insaat_iscisi": "İnşaat İşçisi", "memur": "Memur", "emekli": "Emekli",
    "ogrenci": "Öğrenci", "hafta_sonu_ailesi": "Hafta Sonu Ailesi",
    "uzun_yol_soforu": "Uzun Yol Şoförü", "titiz_musteri": "Titiz Müşteri",
    "mahalle_toplu_yemegi": "Mahalle Toplu Yemeği",
    "denetim_gorevlisi": "Denetim Görevlisi", "eski_musteri": "Eski Müşteri",
}

# ---------------------------------------------------------------------------
# Personel huylari, roller, istasyonlar
# ---------------------------------------------------------------------------
TRAITS = {
    "hizli_ama_daginik": "Hızlı ama Dağınık",
    "yavas_ama_titiz": "Yavaş ama Titiz",
    "kalabalikta_panikleyen": "Kalabalıkta Panikleyen",
    "sakin": "Sakin",
    "musteriyle_iyi_anlasan": "Müşteriyle İyi Anlaşan",
    "suratsiz": "Suratsız",
    "cabuk_yorulan": "Çabuk Yorulan",
    "dayanikli": "Dayanıklı",
    "ekip_moralini_yukselten": "Ekip Moralini Yükselten",
    "huysuz": "Huysuz",
    "cirak": "Çırak",
    "tecrubeli": "Tecrübeli",
}

# Huyun NE YAPTIGI. Ad tek basina karar verdirmiyor: "Sakin" bir
# adayin karti, hiza ve ucrete dokunmadigi icin "normal / normal"
# goruunuyordu ve oyuncu onu hicbir sey yapmayan bir adaydan
# ayirt edemiyordu.
#
# Cumleler SAYI DEGIL SONUC anlatiyor: "%18 hizli" oyuncuya bir sey
# soylemiyor, "yogun saatte yavaslamaz" soyluyor.
TRAIT_DESC = {
    "hizli_ama_daginik": "Çabuk yetiştirir, masaları geç toplar.",
    "yavas_ama_titiz": "Tabağı daha iyi çıkar, ama yavaştır.",
    "kalabalikta_panikleyen": "Günün en yoğun saatinde belirgin yavaşlar.",
    "sakin": "Yoğunluktan etkilenmez.",
    "musteriyle_iyi_anlasan": "Hesabı o alırsa müşteri daha memnun kalkar.",
    "suratsiz": "Hesabı o alırsa müşteri daha az memnun kalkar.",
    "cabuk_yorulan": "Günün son çeyreğinde yavaşlar.",
    "dayanikli": "Gün sonuna kadar aynı tempoda çalışır.",
    "ekip_moralini_yukselten": "Ekibin moralini yukarı çeker.",
    "huysuz": "Ekibin moralini aşağı çeker.",
    "cirak": "Ucuza çalışır, yavaştır, hızlı öğrenir.",
    "tecrubeli": "Pahalıdır, hızlıdır, daha fazla gelişmez.",
}

ROLES = {
    "asci": "Aşçı", "garson": "Garson",
    "bulasikci": "Bulaşıkçı", "kasiyer": "Kasiyer",
}

STATIONS = {
    "ocak": "Ocak", "izgara": "Izgara", "firin": "Fırın",
    "soguk": "Soğuk Tezgâh", "icecek": "İçecek", "tatli": "Tatlı",
    "milkshake_makinesi": "Milkshake Makinesi",
    "waffle_makinesi": "Waffle Makinesi",
    "tas_firin": "Taş Fırın", "doner_ocagi": "Döner Ocağı",
    "pide_firini": "Pide Fırını",
}

CUISINES = {"fastfood": "Fast Food", "turk": "Türk Lokantası"}
STORAGE = {"soguk_hava": "Soğuk Hava Deposu"}

# ---------------------------------------------------------------------------
# Isimli duzenli musteriler: ad, meslek ve UC SAHNE
#
# Sahneler kisa bilerek. docs/16 gunluk dokunus butcesi 40-60; uzun bir
# metin, oyuncunun okumadan gececegi bir metindir. Her sahne bir SEY
# soyluyor ve iliskinin bir adim ilerledigini gosteriyor.
# ---------------------------------------------------------------------------
REGULARS = {
    "hasan_usta": ("Hasan Usta", "Karşı sokakta tornacı", [
        "Kapıdan girer girmez mutfağa bakıyor. “Fasulye var mı?”",
        "Artık siparişini söylemiyor. Oturuyor, sen biliyorsun.",
        "“Oğlum askerden geldi, akşam onu da getireceğim.”",
    ]),
    "nazife_teyze": ("Nazife Teyze", "Üst kattaki komşu", [
        "Çorbayı tadıyor, bir şey demiyor. Yarın yine geliyor.",
        "“Benim mercimeğim de böyle olurdu, eskiden.”",
        "Kapıda duruyor: “Burası mahallenin yüzü oldu.”",
    ]),
    "selim_bey": ("Selim Bey", "Vergi dairesinde memur", [
        "Aynı masa, aynı saat. Bir dakika şaşmıyor.",
        "“Öğle molam kırk dakika. Sizde otuz beşte kalkıyorum.”",
        "Emekliliğini konuşuyor. “O zaman daha çok gelirim.”",
    ]),
    "rasim_amca": ("Rasim Amca", "Şantiye ustabaşı", [
        "Elleri kireçli. Oturmadan önce ceketini silkeliyor.",
        "“Çocuklara da söyledim, öğle yemeği burada.”",
        "Şantiye bitiyor. “Ama ben yine uğrarım, merak etme.”",
    ]),
    "guler_hanim": ("Güler Hanım", "Köşedeki kuaför", [
        "Ayaküstü geliyor, pilavı paket istiyor.",
        "“Müşterilerime de söylüyorum, karşıya gidin diye.”",
        "Dükkânını büyütüyor. “Sizinle beraber büyüdük yani.”",
    ]),
    "okan": ("Okan", "Üniversite öğrencisi", [
        "En ucuz ne varsa onu soruyor.",
        "“Burs yattı.” Bugün tatlı da alıyor.",
        "Staja başlıyor. “İlk maaşımda buradan ısmarlayacağım.”",
    ]),
    "nurten_abla": ("Nurten Abla", "Tekstil atölyesinde usta", [
        "Molası kısa. Cacığı hep yanında götürüyor.",
        "“Atölyeden üç kişi daha gelecek, yer ayırın.”",
        "Atölye kapanıyor. “Yeni yerim uzak ama yine gelirim.”",
    ]),
    "ismail_sofor": ("İsmail Şoför", "Uzun yol kamyon şoförü", [
        "Kamyonu köşeye çekiyor, hızlı yiyip çıkıyor.",
        "“Ankara dönüşü hep buraya uğruyorum artık.”",
        "Telsizde anlatmış: “İki şoför arkadaş daha soracak sizi.”",
    ]),
    "perihan_hanim": ("Perihan Hanım", "Emekli öğretmen", [
        "Çatalı ışığa tutuyor. Bir şey demiyor ama bakıyor.",
        "“Masa örtüsü bugün temiz. Fark ettim.”",
        "“Ben kolay beğenmem. Burayı beğendim.”",
    ]),
    "mehmet_dede": ("Mehmet Dede", "Eski lokanta müdavimi", [
        "Kapıda duraklıyor. “Burası eskiden başkasınındı.”",
        "“O zamanki nohut da böyleydi. Aynı.”",
        "Her gün geliyor artık. Sandalyesi belli oldu.",
    ]),
    "deniz": ("Deniz", "Lise öğrencisi", [
        "Servis çıkışı, sırt çantası omzunda, acelesi var.",
        "“Arkadaşlarla buluşma yeri burası oldu.”",
        "Sınavı kazanmış. “Kutlama burada, altı kişiyiz.”",
    ]),
    "burak": ("Burak", "Yazılımcı", [
        "Bilgisayarını açıyor, siparişi bekletmeden söylüyor.",
        "“Ekip öğle yemeğini buraya taşıdık.”",
        "“Uzaktan çalışacağım ama burası ofisim sayılır.”",
    ]),
    "elif": ("Elif", "Mağaza satış danışmanı", [
        "Alışveriş poşetleriyle geliyor, on beş dakikası var.",
        "“Vitrinden gördüm, yeni bir şey eklemişsiniz.”",
        "“Mağazadaki kızlara da söyledim. Sizden alıyoruz artık.”",
    ]),
    "kaan_hoca": ("Kaan Hoca", "Spor salonu antrenörü", [
        "Antrenman sonrası, protein soruyor.",
        "“Öğrencilerime buradan yemelerini söylüyorum.”",
        "“Salonun panosuna astım adresinizi. Umarım sorun olmaz.”",
    ]),
    "sevda": ("Sevda", "Diyetisyen", [
        "Salatayı soruyor: “Sosu ayrı olur mu?”",
        "“Danışanlarıma buradan öneri veriyorum.”",
        "“Menünüzü kliniğin duvarına astım.”",
    ]),
    "tolga": ("Tolga", "Gece vardiyası güvenlik", [
        "Gece yarısı geliyor. Kapının açık olmasına şaşırıyor.",
        "“Sabahın körü açık olan tek yer sizsiniz.”",
        "“Vardiya arkadaşları da gelmeye başladı, fark ettiniz mi?”",
    ]),
    "melis": ("Melis", "Serbest muhasebeci", [
        "Fiyat listesini baştan sona okuyor.",
        "“Hesabı ben tutarım, siz yemeği yapın.”",
        "“Kâr marjınızı merak ediyorum. Şaka değil.”",
    ]),
    "ozan": ("Ozan", "Amatör futbolcu", [
        "Maç sonrası, takımıyla birlikte, gürültülü.",
        "“Kazanınca buraya geliyoruz. Uğurlu oldunuz.”",
        "“Kupayı aldık. Formaya adınızı yazdıralım mı?”",
    ]),
    "yagmur": ("Yağmur", "Gece dersi öğretmeni", [
        "Geç saatte, yorgun. Tatlı soruyor.",
        "“Günün tek keyifli anı burası oluyor.”",
        "“Kursu bitirdim. Ama alışkanlık yaptı, geleceğim.”",
    ]),
    "cem_abi": ("Cem Abi", "Motokurye", [
        "Motoru kapının önünde, kaskı elinde.",
        "“Kuryeler grubuna yazdım sizi.”",
        "“Kendi dükkânımı açıyorum. Sizden öğrendim bu işi.”",
    ]),
}

# ---------------------------------------------------------------------------
# Arayuz metinleri. Icerikte yoklar, elle yaziliyorlar.
# ---------------------------------------------------------------------------
UI = {
    "ui.game.title": "Lokanta",
    "ui.menu.new": "Yeni Oyun",
    "ui.menu.continue": "Devam Et",
    "ui.menu.settings": "Ayarlar",
    "ui.menu.quit": "Çıkış",
    "ui.menu.credits": "Yapımcı",

    "ui.cuisine.pick": "Hangi mutfakla başlıyorsun?",
    "ui.cuisine.start": "Başla",
    "ui.cuisine.signature": "İmza mekaniği",
    "ui.cuisine.combo": "Kombo: üç yemeği birlikte menüde tutarsan fiş büyür, mutfak yorulur.",
    "ui.cuisine.credit": "Veresiye: nakit akışını bozar, sadakati büyütür.",

    # DISLI DUGMESI "Menü" DEGIL.
    #
    # ui.morning.menu de "Menü" idi (menu tahtasi) ve ikisi EN'de de
    # "Menu" oluyordu: iki farkli ekran, ayni ad. Disli oyunu
    # DURAKLATIYOR, menuyu acmiyor.
    "ui.hud.menu": "Duraklat",
    # KAMPANYANIN HEDEFI. 61. gune kadar oyuna "altmis gun" diyen
    # tek bir satir yoktu; oyuncu bitis tarihi olmayan bir dukkan
    # isletip sonunda hic duymadigi yedi eksende puanlaniyordu.
    "ui.hud.season": "{0} / {1}. gün",
    "ui.hud.free_play": "serbest oyun",
    "ui.slot.title": "Kayıt yuvası",
    "ui.slot.empty": "Boş",
    # Yuva bozuksa pasif dugme "Boş" diyordu, ust rozet ise
    # "bozuk kayıt" - aynı kartın iki yarısı farklı şey söylüyordu.
    "ui.slot.unloadable": "Yüklenemiyor",
    "ui.slot.day": "{0}. gün",
    "ui.slot.delete": "Sil",
    "ui.slot.delete_confirm": "Bu kayıt silinsin mi? Geri alınamıyor.",
    "ui.slot.overwrite": "Bu yuvanın üzerine yazılsın mı?",

    "ui.phase.morning": "Sabah",
    "ui.phase.service": "Servis",
    "ui.phase.evening": "Akşam",

    "ui.hud.cash": "Kasa",
    "ui.hud.reputation": "İtibar",
    "ui.hud.tables": "Masa",
    # ui.morning.staff ile AYNI kavram ("Personel"); TR'de iki ad
    # kullaniliyordu, EN'de ikisi de "Crew" idi - yani ayrisma yalnizca
    # Turkce'de vardi ve bir anlam tasimiyordu.
    "ui.hud.served": "Ağırlanan",
    # YENI ARAYUZ: kartlar ve asama dugmesi.
    #
    # "Bugün" karti servis sirasindaki uc sayiyi tasiyor; sabahin
    # kontrol listesi servisi acmadan once okunmasi gereken tek sey.
    # Asama dugmesinin alt satiri NE OLACAGINI soyluyor - geri donusu
    # olmayan bir karara basmadan once okunabilen tek cumle.
    "ui.hud.today": "Bugün",
    "ui.hud.satisfaction": "Memnuniyet",
    "ui.morning.checklist": "Açılış hazırlığı",
    "ui.morning.open_sub": "{0}. gün başlıyor",
    "ui.service.close_sub": "Servis bitti",
    "ui.service.running_sub": "Servis sürüyor",
    "ui.service.combo_on": "Kombo açık",
    "ui.service.combo_off": "Kombo kapalı",
    "ui.evening.next_sub": "{0}. güne geç",
    # TR "Kızgın" bir duygu durumu, EN "Walkouts" cikip giden sayisi:
    # AYNI sayi, iki farkli sey soyluyordu. Sayinin tanimi
    # "kizgin ayrilan grup" - iki dil de artik onu soyluyor.
    "ui.hud.angry": "Çıkıp giden",
    "ui.hud.back": "Geri",
    "ui.hud.pause": "Duraklat",
    "ui.hud.resume": "Devam",
    "ui.hud.speed": "Hız",

    "ui.morning.market": "Hal",
    "ui.morning.menu": "Menü",
    "ui.morning.staff": "Personel",
    "ui.morning.equipment": "Ekipman",
    "ui.morning.open": "Servisi aç",
    # Kredi. docs/12 4. Mekanik cekirdekte eksiksiz yaziliydi ve
    # hicbir ekranda dugmesi yoktu.
    "ui.morning.loan": "Kredi",
    "ui.loan.title": "Kredi",
    "ui.loan.weekly": "Haftalık sabit giderin {0}",
    "ui.loan.repay": "Toplam geri ödeme",
    "ui.loan.installment": "Haftalık taksit",
    "ui.loan.weeks_left": "Kalan hafta",
    "ui.loan.take": "Çek",
    "ui.loan.open": "Açık kredin var",
    "ui.loan.one_at_a_time": "Aynı anda tek kredi taşıyabilirsin.",
    "ui.loan.warning": "Taksit haftalık giderine eklenir ve ödenmezse dükkân küçülür.",

    # --- ilk bes dakika -----------------------------------------------------
    # Uc kural, uc cumle. Her biri BIR KEZ gorunuyor.
    #
    # docs/16 dakika dakika bir ogretici tarif ediyordu ve kodda tek
    # satir karsiligi yoktu. Yeni oyuncunun ilk gun gordugu sey bos bir
    # salon, dort gri dugme ve adi olmayan kirmizi bir sayiydi; tek
    # anlamli eylem "Servisi Ac" ve o da geri donusu olmayan bir kapi.
    #
    # Metin duvari degil: ilgili ekranda, ilk kez, tek cumle.
    "ui.hint.service": "Servisi açınca akşama kadar alışveriş yapamazsın. "
                    "Stok ve menü şimdi hazır olsun.",
    "ui.hint.menu": "Menüde yemek yoksa müşteri kapıdan döner. "
                 "Geçen gün kapıdan dönenleri gün raporunda görebilirsin.",
    "ui.hint.interventions": "Üç müdahalenin de hakkı aynı kesede: günde {0}. "
                          "Harcamadan gün biterse hakkın yanar.",
    "ui.hint.rent": "Yarın kira günü. Kasada kira kadar para yoksa borçlanırsın.",
    # Sonradan eklenen dordu, docs/16 119'un saydigi hedeflerden.
    #
    # Fiyat: kodun kendi yorumu "oyunun en patronca karari" diyor ve
    # kontrol Menu ekraninda bir kartin icinde gomulu; hicbir sey oraya
    # isaret etmiyordu.
    "ui.hint.price": "Fiyatı sen koyuyorsun. Piyasanın çok üstü "
                     "müşteriyi kaçırır, çok altı kârı yer.",
    # Personel: oyunun temel gerilimi "kadro kapasite AMA para" ve
    # ucret hicbir ekranda mutlak sayi olarak gorunmuyordu.
    "ui.hint.staff": "Personel kapasite verir ama her hafta maaş götürür. "
                     "İşe almadan önce kasaya bak.",
    # Masa secimi: kesfedilemez bir mekanikti. Genel gorunumde sekiz oda
    # var ve ilk gun yalnizca birinde masa; oyuncunun bu zinciri kendi
    # basina bulmasi beklenemez.
    "ui.hint.select": "Salona dokun, sonra bir masaya dokun: ikramlar "
                      "seçtiğin masaya gider.",

    # ITIBAR TAVANI. Oyunun en onemli ilerleme kurali ve hicbir yerde
    # yazmiyordu: iyi oynayan bir oyuncu yedi masada 75'e dayanip otuz
    # iki gun orada kaliyor - servisi ne kadar iyi yaparsa yapsin. Sayi
    # sebepsiz durunca oyun bozuk goruunuyor.
    "ui.hint.cap": "İtibarın bu masa sayısında en fazla {0} olabilir. "
                   "Daha yükseği için salonu büyütmen gerekiyor.",
    "ui.hint.ok": "Anladım",
    # Servis penceresi dolup salon bosalinca oyun sessizce duruyordu.
    "notice.service_done": "Servis bitti — günü kapatabilirsin.",
    # Kayit yazilamadi. Once tamamen sessizdi: depolamasi dolu bir
    # oyuncu otuz gun oynayip hicbir sey bulamiyordu.
    "notice.save_failed": "Kayıt yazılamadı — cihazın depolamasını kontrol et.",
    "ui.settings.hints_reset": "İpuçlarını yeniden göster",
    "ui.morning.buy_days": "{0} günlük — {1}",
    "ui.morning.no_storage": "Soğuk hava deposu olmadan bir günlükten fazlası gece gider.",
    "ui.morning.restock": "Önerilen stoku al",
    # "Yil ortalamasi +%9" ortalamanin kendisi %9 gibi okunuyordu.
    "ui.morning.average_over": "Ortalamanın %{0} üstünde",
    "ui.morning.average_under": "Ortalamanın %{0} altında",
    "ui.morning.average_same": "Ortalama fiyat",
    "ui.morning.cheap": "Bugün ucuz",
    "ui.morning.expensive": "Bugün pahalı",
    "ui.morning.stock": "Stok",
    "ui.morning.days_keep": "gün dayanır",
    "ui.morning.spoils_tonight": "gece gider",
    "ui.morning.never_spoils": "bozulmaz",

    "ui.menu.onmenu": "Menüde",
    # KRIZ SERIDI: sabri biten masalar. Once tek sinyal masa ustundeki
    # kucuk rozetti; oyuncu mudahale hakkini kime harcayacagini
    # goremiyordu.
    "ui.service.crisis": "SABRI BİTİYOR",
    "ui.service.table_n": "Masa {0}",
    # DUNE GORE fark. Bir sayinin tek basina anlami yok.
    "ui.evening.same": "dünküyle aynı",
    # HAZIRLIK OZETI: servisi acmak geri donusu olmayan tek karar ve
    # onaysizdi. Eksik olan kalem kirmizi yaziyor.
    "ui.morning.ready_menu": "Menüde {0} yemek",
    # STOK SATIRI GUNU OLCUYOR, YEMEK SAYISINI DEGIL.
    # "Stokta 6 yemek" dogruydu ama yaniltiyordu: alti yemegin
    # her birinden birer porsiyon da "6" yaziyordu. Simdi satir
    # gunun beklenen talebinin ne kadarinin karsilandigini soyluyor.
    "ui.morning.ready_stock_ok": "Stok bugüne yetiyor",
    "ui.morning.ready_stock_part": "Stok {0} / {1} kişiye yetiyor",
    "ui.morning.ready_cooks": "Mutfak {0} kişi",
    "ui.morning.ready_warn_menu": "Menüde hiç yemek yok. Yine de açmak için tekrar bas.",
    "ui.morning.ready_warn_cook": "Mutfakta aşçı yok. Yine de açmak için tekrar bas.",
    "ui.morning.ready_warn_stock": "Stok bugünü çıkarmaz. Yine de açmak için tekrar bas.",
    # Menu listesi GRUPLU: menude olanlar, acik olanlar, yakinda
    # acilacaklar, kilitliler. Once ham icerik sirasindaydi ve oyuncu
    # birinci gun art arda alti KILITLI satir goruyordu.
    "ui.menu.group_on": "MENÜDE",
    "ui.menu.group_open": "AÇIK — menüye ekleyebilirsin",
    "ui.menu.group_soon": "YAKINDA",
    "ui.menu.group_locked": "Kilitli",
    "ui.menu.locked": "Kilitli",
    "ui.menu.needs_reputation": "İtibar {0} gerekiyor",
    "ui.menu.needs_equipment": "{0} gerekiyor",
    "ui.menu.vs_market": "Piyasaya göre",
    "ui.menu.add": "Menüye ekle",
    "ui.menu.price": "Fiyat",
    "ui.menu.cost": "Maliyet",
    "ui.menu.margin": "Kâr marjı",
    "ui.menu.favourite_of": "{0} bunu seviyor",

    "ui.staff.hire": "İşe al",
    "ui.staff.fire": "Çıkar",
    "ui.staff.candidates": "Adaylar",
    "ui.staff.morale": "Moral",
    "ui.staff.level": "Seviye",
    "ui.staff.wage": "Maaş",
    "ui.staff.cook": "Mutfak",
    "ui.staff.salon": "Salon",
    "ui.staff.fire_confirm": "Bu kişi çıkarılsın mı? Deneyimi sıfırlanır.",
    "ui.staff.salon_none": "Salon kadron yok. Bugünkü masa sayısı için gerek de yok.",
    "ui.staff.salon_needed": "Salon kadron yok — bugün {0} kişi gerekiyor.",
    "ui.staff.cap": "Kadro tavanı",
    # KADRO SAYISI BIR TAVSIYE DEGIL BIR KAPASITE HESABI.
    #
    # "Bugün gereken" yaziyordu; denge araci o sayiyi tutturan oyuncunun
    # bir garson eksik calisandan 4.300 sikke daha az kazandigini olctu.
    # Sayi yanlis degil, ADI yanlisti: herkese yetismenin karsiligi.
    "ui.staff.to_serve_all": "Herkese yetişmek için",
    "ui.staff.weekend": "hafta sonu",
    # BULASIK NOBETI. docs/14 bulasikciyi ayri bir rol olarak tarif
    # ediyor ama salon havuzu "garson + bulasikci + kasiyer" - yani
    # bulasikci ayri bir kadro degil, lavaboya AYRILMIS bir salon
    # calisani. Metin de oyle konusuyor.
    "ui.staff.sink": "Bulaşık nöbeti",
    "ui.staff.sink_none": "Kimse lavaboda değil — bulaşık birikince garson geçer",
    "ui.staff.sink_n": "{0} kişi lavaboda",
    "ui.staff.sink_add": "Lavaboya ver",
    "ui.staff.sink_remove": "Salona al",
    "ui.staff.sink_hint": "Lavaboda duran kişi servis yapmaz. "
                          + "Kimse durmazsa temiz tabak bitince mutfak bekler.",
    "ui.hud.plates": "Temiz tabak",
    # TABAK BITTI. Sebep ve care ayni cumlede: oyuncu "neden durdu" ve
    # "ne yapmaliyim" sorularinin ikisini de bir satirda gormeli.
    "notice.plates_out": "Temiz tabak bitti — mutfak bekliyor. "
                         "Lavaboda {0} kirli tabak var.",
    "notice.plates_out_busy": "Temiz tabak bitti — bulaşıkçı yetişemiyor. "
                              "Lavaboda {0} kirli tabak var.",
    "ui.staff.no_candidate": "Aday kalmadı. Havuz üç günde bir yenileniyor.",
    # Huy kartinda "ücret / hız: normal" - kodda gomulu Turkce idi.
    "ui.staff.normal": "normal",

    "ui.service.close": "Günü kapat",
    # Yanindaki iki dugme secili masaya gidiyor, bu mutfaga gidiyor.
    # KISA TUTULUYOR. "Mutfagi hizlandir" serit butcesini asiyordu:
    # olculdu, uc mudahale dugmesinin ucu birden kirpiliyordu. Fiil tek
    # basina yeterli - dugme zaten mutfak kutusunun icinde.
    "ui.service.rush": "Hızlandır",
    "ui.service.tea": "Çay",
    "ui.service.attention": "İlgi",
    # TEK KELIME: simgeli dugmenin etiketi serit butcesini asiyordu
    # (on dort masali Turk lokantasinda olculdu). Simge zaten "defter"
    # diyor, etiketin isi onu adlandirmak.
    "ui.service.credit": "Veresiye",
    # Once yalnizca "Hak" yaziyordu ve neyin hakki oldugu belli degildi.
    # Secim yokken hedefi oyun seciyor; dugme bunu soylemeli.
    "ui.service.target_auto": "sabırsız",

    # SERVIS GERI BILDIRIMLERI.
    #
    # Bunlarin hepsi bir sure KODDA GOMULU TURKCE idi
    # (Toast("Sıkışan istasyon yok"), TargetToast("çay ikram edildi"),
    # (sel+1) + ". masaya ") ve oyunun ANA dongusundeki tek geri
    # bildirim kanali bunlar: Ingilizce oynayan biri dugmeye basip
    # "3. masaya çay ikram edildi" okuyordu. Ustelik ". masaya" bir
    # Turkce sira eki - cevrilse bile kalip calismazdi.
    "ui.service.none_station": "Sıkışan istasyon yok",
    "ui.service.none_table": "Bekleyen masa yok",
    "ui.service.none_credit": "Veresiye isteyen yok",
    "ui.service.done_rush": "{0} hızlandırıldı",
    "ui.service.done_tea": "{0}. masaya çay ikram edildi",
    "ui.service.done_tea_any": "Çay ikram edildi",
    # CAY ARTIK SALONA GIDIYOR.
    #
    # Tek masaya giden cay, uc fiilden birini OLU birakiyordu: ilgi
    # her eksende ustundu (2400 memnuniyet / x2 sabir / mutfagi one
    # alma) ve ustelik bedavaydi. Cay simdi BEKLEYEN HERKESE gidiyor;
    # "bir masa krizde" degil "salon sabirsiz" sorusunun cevabi.
    "ui.service.done_tea_room": "Salona çay ikram edildi",
    "ui.service.none_waiting": "Bekleyen masa yok",
    "ui.service.done_care": "{0}. masaya ilgi gösterildi",
    "ui.service.done_care_any": "İlgi gösterildi",
    "ui.service.done_credit": "{0} deftere yazıldı",
    "ui.service.guest": "Müşteri",

    "ui.evening.title": "Gün raporu",
    "ui.evening.revenue": "Ciro",
    "ui.evening.ingredients": "Malzeme",
    "ui.evening.profit": "Günün kârı",
    "ui.evening.gross": "Brüt kâr",
    "ui.evening.rent_in": "{0} gün sonra {1}",
    # "Kira odendi | 4 gun sonra 1.200" kendisiyle celisiyordu.
    "ui.evening.rent_next": "Sıradaki kira",
    "ui.evening.satisfaction": "Ortalama memnuniyet",
    "ui.story.continue": "Devam",
    "ui.evening.next": "Ertesi gün",
    "ui.evening.spoiled": "Bozulan",
    "ui.evening.wages": "Maaş ödendi",
    "ui.evening.rent": "Kira ödendi",

    "ui.end.continue": "Serbest oyuna devam",
    "ui.end.menu": "Ana Menü",

    "ui.settings.title": "Ayarlar",
    "ui.settings.sound": "Ses",
    "ui.settings.music": "Müzik",
    "ui.settings.language": "Dil",
    "ui.credits.licenses": "Açık kaynak lisansları",
    "ui.settings.back": "Geri",

    # --- bildirimler ---------------------------------------------------
    #
    # Cekirdek otuz uc tur olay uretiyor ve bunlar oyunun tek "haber"
    # kaynagi. Metinler KISA: bir serit bildirimi iki saniye duruyor ve
    # oyuncunun gozu salonda.
    #
    # Kizgin cikisin SEBEBI yaziliyor, kendisi degil: "musteri kizdi"
    # bilgi degil, "masa bulamadi" ertesi gun ne yapacagini soyluyor.
    "notice.angry": "Bir müşteri çıktı — {0}",
    "notice.why.table": "masa bulamadı",
    "notice.why.order": "siparişi alınmadı",
    "notice.why.food": "yemeği gelmedi",
    "notice.why.other": "bekleyemedi",

    # Metin bir yemek ADI TASIMIYOR: olay "su yemek bitti" degil,
    # "bu musteri menude yapabilecegi bir sey bulamadi".
    "notice.turned_away": "Bir grup kapıdan döndü — menüde yapabileceği bir şey yoktu",
    "notice.requested": "Soruldu ama yok: {0}",
    "notice.unlocked": "Yeni yemek açıldı: {0}",

    "notice.regular": "{0} geldi.",
    "notice.regular_upset": "{0} küstü — {1} gün gelmeyecek.",

    "notice.resigned": "{0} bıraktı.",
    "notice.level_up": "{0} seviye atladı: {1}",

    "notice.weekly": "Kira {0}, maaş {1} ödendi.",
    "notice.wages_late": "Maaş gecikti — {0} eksik.",
    "notice.debt": "Kasa eksiye düştü: {0}",

    "notice.credit_given": "Deftere yazıldı: {0}",
    "notice.credit_paid": "Veresiye tahsil edildi: {0}",
    "notice.credit_lost": "Veresiye battı: {0}",

    "notice.equipment": "{0} y\u00fckseltildi \u2014 kademe {1}",
    "notice.storage": "Soğuk hava deposu yükseltildi — kademe {0}",
    "notice.rushed": "{0} hızlandırıldı — {1} iş",

    "notice.equipment_sold": "Borç yüzünden satıldı: {0} › kademe {1}",
    "notice.downsized": "Dükkân küçüldü — {0} masa kaldı.",
    "notice.rejected": "Bu şu an yapılamıyor.",
    "notice.rejected_cash": "Kasada yeterli para yok.",
    # Gunluk komut hakki: 256. Onerilen stogu tek komuta indirmeden
    # once bu sinir bir sabahta dolabiliyordu ve oyuncu sebebini
    # hicbir yerde goremiyordu.
    "notice.rejected_budget": "Bugünlük bu kadar iş yeter — yarın devam.",
    "notice.rejected_book": "Defter dolu — önce tahsilat.",
    "notice.unknown": "bilinmeyen",

    # --- yil sonu degerlendirme eksenleri (docs/08) ---
    # --- yil sonu degerlendirmesi (docs/08) ---
    #
    # Manset PUANA gore degisiyor: semtin yemek elestirmeni yaziyor.
    # Ayni cumleyi herkese gostermek, altmis gunluk oyunu tek bir sabit
    # metne indirmek olurdu.
    "ui.end.plaque0": "Ayakta kalan lokanta",
    "ui.end.plaque1": "Semtin lokantası",
    "ui.end.plaque2": "Adı duyulan lokanta",
    "ui.end.plaque3": "Şehrin konuştuğu lokanta",
    "ui.end.lede": "{0} gün işlettin. Yıl sonu değerlendirmen: {1} / 100.",

    "score.wealth": "Varlık",
    "score.reputation": "İtibar",
    "score.regulars": "Düzenli müşteriler",
    "score.crew": "Ekip",
    "score.place": "Mekân",
    "score.resilience": "Sağlamlık",
    "score.signature": "Mutfak",

    "score.axis.combo": "Kombo payı",
    "score.axis.credit": "Veresiye tahsilatı",

    "ui.hud.rent_in": "Kiraya {0} gün — {1}",
    "ui.hud.rent_today": "Bugün kira — {0}",
    "ui.pause.resume": "Devam",
    "ui.pause.season": "Yıl Sonu Değerlendirmesi",
    "ui.pause.save_quit": "Kaydet ve çık",
    "ui.common.yes": "Evet",
    "ui.common.cancel": "Vazgeç",
    "ui.common.ok": "Tamam",
    # Para birimi GENEL PARA ISARETI (¤), lira degil.
    # Karar docs/12: oyunun parasi gercek bir para birimi degil,
    # bir jeton - oyun Turkiye disinda da satilacak ve gercek bir
    # kur, dengeyi tarihe bagli kilardi. ₺ ayrica yazi tipinde
    # yok (tools/art/check_font.py bunu yakaladi).
    "ui.common.coin": "¤",

    # --- KODDA GOMULU KALMIS METINLER -------------------------------------
    #
    # Bu otuz bes dize `Theme.Text/Btn/Head/Field/Title` cagrilarinin
    # ICINDE sabit Turkce olarak duruyordu. gen_loc.py'nin docstring'i
    # bunu itiraf ediyordu ("yirmi bes kadar arayuz dizesi hala kodun
    # icinde gomulu") ama hicbir denetim kirmiyordu ve sayi 40'a cikti.
    # Artik `loc_tarama.py` kiriyor.
    "ui.evening.money": "Para",
    "ui.evening.in_book": "Defterde",
    # VERESIYE DEFTERI. Mekanigin ikinci karari (erken tahsilat) kodda
    # UYGULANIYORDU ama hicbir ekran onu gondermiyordu; defter de
    # gorunmuyordu - yedi gunluk bekleyis tamamen edilgendi.
    "ui.ledger.title": "Veresiye defteri",
    "ui.ledger.total": "Defterde {0}",
    "ui.ledger.empty": "Defter temiz.",
    "ui.ledger.amount": "Tutar",
    "ui.ledger.due": "Vade",
    "ui.ledger.days": "{0} gün",
    "ui.ledger.today": "bugün",
    "ui.ledger.chance_wait": "Beklersen ödeme şansı",
    "ui.ledger.chance_now": "Şimdi kovalarsan",
    "ui.ledger.had_tea": "Hesap açılırken çay ikram edilmişti.",
    "ui.ledger.chase": "Şimdi kovala",
    "ui.ledger.note": "Kovalamak şansı yarıya indirir ve tutmazsa hesap orada kapanır.",
    "ui.evening.hall": "Salon",
    "ui.evening.turned_away": "Kapıdan dönen",
    # AYNI ETIKET UC FARKLI SAYIDA KULLANILIYORDU.
    #
    # "ui.hud.angry" hem servis kartinda (TOPLAM kayip), hem aksam
    # seridinde (TOPLAM), hem gun raporunda (yalnizca MASADAN kalkan)
    # geciyordu. Oyuncu ayni gun, ayni kelimenin altinda iki farkli
    # sayi goruyor ve birinin bozuk oldugunu dusunuyordu. Sayilar
    # zaten ayriydi; eksik olan ADLARIN ayrilmasiydi.
    "ui.evening.lost": "Kaybedilen",
    "ui.evening.left_table": "Masadan kalkan",
    "ui.evening.regulars": "Müdavimler",
    "ui.evening.away": "Bir süre uğramayacak.",
    "ui.evening.served_n": "{0} kişi / {1} masa",
    "ui.evening.visits": "{0} ziyaret",

    "ui.error.title": "Oyun açılamadı",
    "ui.error.content": "İçerik dosyaları okunamadı. Bu bir içerik hatası; "
                        "oyun bozuk bir dengeyle açılmaktansa hiç açılmıyor.",
    "ui.error.save": "Kayıt açılamadı.",
    "ui.menu.tagline": "Patronsun, aşçı değil.",
    "ui.slot.broken": "bozuk kayıt",

    "ui.credits.design": "Tasarım, kod ve içerik",
    "ui.credits.author": "Ahmet Caner Akar",
    "ui.credits.models": "3B modeller",
    "ui.credits.models_by": "Kenney (kenney.nl) — CC0",
    "ui.credits.audio": "Müzik ve ses",
    "ui.credits.audio_by": "Oyun içinde sentezleniyor",
    "ui.credits.font": "Yazı tipi",
    "ui.credits.font_by": "Rubik — SIL OFL 1.1",
    "ui.credits.font_copyright": "Copyright 2015 The Rubik Project Authors",

    "ui.cuisine.fastfood_desc": "Hızlı akış, düşük fiş, kalabalık. "
                                "Menü dar tutulur.",
    "ui.cuisine.turk_desc": "Sert öğle zirvesi, yüksek fiş, mahalle müşterisi.",

    "ui.staff.cap_full": "Kadro tavanı dolu",
    "ui.storage.desc": "Bozulabilir malzemenin raf ömrünün bir kısmını kazandırır.",
    "ui.common.tier": "Kademe",
    "ui.common.upgrade": "Yükselt — {0}",
    "ui.common.top_tier": "En üst kademe",
    "ui.station.cuisine_only": "mutfağa özel",
    "ui.station.slots": "Yuva",
    "ui.station.too_low": "Masa sayısı için yetersiz",
    "ui.expand.title": "Genişleme",
    "ui.expand.last": "Son kademedesin.",
    "ui.expand.tables": "Masa",
    "ui.expand.warn": "Kira da büyüyor — masa dolmazsa zarar.",
    "ui.expand.buy": "Genişle — {0}",
    "ui.evening.low_morale": "{0} kişinin morali düşük — istifa edebilirler",
    "ui.morning.storage_tier": "Soğuk hava kademe {0}",
    "ui.morning.storage_none": "Soğuk hava yok — bozulan her şey gece gidiyor",
    "ui.menu.subtitle": "Menüde duran her yemek için stok tutuluyor.",
    "ui.menu.unlock_day": "{0}. günde açılıyor",
    "ui.staff.days": "{0} ({1} gün)",
    "ui.staff.inherited": "Huysuz — devraldığın aşçı",
}


# ---------------------------------------------------------------------------
def SCREEN_KEY(k):
    """Icerigin istemedigi ama EKRANIN kullandigi anahtar mi.

    Dort aile: "ui." arayuz metinleri, "notice." olay bildirimleri,
    "score." yil sonu degerlendirme eksenleri, ve ".desc" ile biten
    ACIKLAMA metinleri. Bunlari icerik dosyalari istemiyor - kod
    istiyor. Denetci ikisini ayirmazsa ya bildirimleri "fazlalik"
    sayip siliyor ya da kod hicbir metin bulamiyor.

    ".desc": icerik bir huyun ADINI istiyor (nameKey), ne yaptigini
    degil. Aciklama yalnizca ise alim kartinda goruunuyor ve oyuncunun
    iki adayi ayirt edebilmesinin TEK yolu.
    """
    return (k.startswith("ui.") or k.startswith("notice.")
            or k.startswith("score.") or k.endswith(".desc"))


def required_keys():
    """Icerigin istedigi butun metin anahtarlari."""
    keys = set()

    def walk(o):
        if isinstance(o, dict):
            for k, v in o.items():
                if k.endswith("Key") and isinstance(v, str):
                    keys.add(v)
                walk(v)
        elif isinstance(o, list):
            for v in o:
                walk(v)

    for root, _, files in os.walk(CONTENT):
        if os.path.basename(root) == "loc":
            continue
        for f in files:
            if f.endswith(".json"):
                walk(json.load(io.open(os.path.join(root, f), encoding="utf-8")))
    return keys


def _no_duplicates():
    """Kaynaktaki YINELENEN sozluk anahtarlarini yakalar.

    Python bir sozlukte ayni anahtar iki kez yazilirsa SON degeri alir
    ve ilkini sessizce atar. Bu araç eksik anahtari, bos metni ve
    fazlaligi yakaliyordu ama yinelenmeyi goremiyordu - cunku dosya
    Python'a yuklendiginde yineleme zaten kaybolmus oluyor.

    Olculdu: `ui.evening.wages` ("Ucret" / "Maas odendi") ve
    `ui.evening.rent` ("Kira" / "Kira odendi") ikiser kez yazilmisti;
    ikisinin de ilk metni oyuna hic girmedi. Sessiz metin kaybi,
    eksik metinden daha tehlikeli: denetim yesil kaliyor.

    O yuzden sozluk degil KAYNAK taraniyor.

    KAPSAM: yineleme AYNI SOZLUK ICINDE aranıyor, dosyanin tamaminda
    degil. Once dosya geneli taraniyordu ve TRAIT_DESC eklenince on iki
    yanlis alarm verdi - `TRAITS["sakin"]` ile `TRAIT_DESC["sakin"]`
    ayri sozlukler ve ayri anahtarlara (`trait.sakin`,
    `trait.sakin.desc`) donusuyorlar. Yanlis alarm veren bir denetim,
    kapatilmaya davetiyedir; kapsam daraltildi.
    """
    import re as _re
    here = os.path.abspath(__file__)
    text = io.open(here, encoding="utf-8").read()

    dup = []
    # Sozluk sinirlari: sutun 0'da baslayan BUYUK_HARF = { ... }
    for blok in _re.finditer(r'^[A-Z_][A-Z0-9_]*\s*=\s*\{(.*?)^\}',
                             text, _re.M | _re.S):
        seen = set()
        for m in _re.finditer(r'^\s*"([a-z0-9_.]+)"\s*:', blok.group(1), _re.M):
            k = m.group(1)
            if k in seen:
                dup.append(k)
            seen.add(k)
    return dup


def build_en():
    """Ingilizce tablo. build() ile AYNI sekilde kuruluyor."""
    table = {}
    for k, v in loc_en.INGREDIENTS.items():
        table["ingredient." + k] = v
    for k, v in loc_en.DISHES.items():
        table["dish." + k] = v
    for k, v in loc_en.ARCHETYPES.items():
        table["archetype." + k] = v
    for k, v in loc_en.TRAITS.items():
        table["trait." + k] = v
    for k, v in loc_en.TRAIT_DESC.items():
        table["trait." + k + ".desc"] = v
    for k, v in loc_en.ROLES.items():
        table["role." + k] = v
    for k, v in loc_en.STATIONS.items():
        table["station." + k] = v
    for k, v in loc_en.CUISINES.items():
        table["cuisine." + k] = v
    for k, v in loc_en.STORAGE.items():
        table["storage." + k] = v
    for rid, (name, job, beats) in loc_en.REGULARS.items():
        table["regular." + rid + ".name"] = name
        table["regular." + rid + ".job"] = job
        for i, text in enumerate(beats):
            table["regular." + rid + ".beat" + str(i + 1)] = text
    table.update(loc_en.UI)
    return table


def _placeholders(text):
    """Metindeki {0}, {1}... kumesi."""
    out = set()
    i = 0
    while True:
        a = text.find("{", i)
        if a < 0:
            return out
        b = text.find("}", a)
        if b < 0:
            return out
        out.add(text[a:b + 1])
        i = b + 1


def compare(tr, en):
    """
    Iki tablonun AYRISMADIGINI dogrular.

    Iki sey sinaniyor ve ikisi de sessiz hata uretir:

      1. ANAHTAR farki - eksik anahtar oyunda "[ui.staff.hire]" yazan
         bir dugme demek.
      2. YER TUTUCU farki - Turkce'de {0} olup Ingilizce'de olmayan bir
         metin, bicimleme hatasi DEGIL sessizce eksik bir sayi veriyor
         ("Gun" yazip gun numarasini hic yazmamak gibi).
    """
    sorun = []

    eksik = sorted(k for k in tr if k not in en)
    fazla = sorted(k for k in en if k not in tr)
    for k in eksik:
        sorun.append("EN eksik: " + k)
    for k in fazla:
        sorun.append("EN fazla (TR'de yok): " + k)

    for k in sorted(set(tr) & set(en)):
        a, b = _placeholders(tr[k]), _placeholders(en[k])
        if a != b:
            sorun.append("yer tutucu farkli: %s  tr=%s  en=%s"
                         % (k, sorted(a) or "-", sorted(b) or "-"))

    bos = sorted(k for k, v in en.items() if not v or not v.strip())
    for k in bos:
        sorun.append("EN bos metin: " + k)
    return sorun


def build():
    dup = _no_duplicates()
    if dup:
        print("YINELENEN ANAHTAR (metin sessizce kayboluyor):")
        for k in sorted(set(dup)):
            print("  " + k)
        sys.exit(1)

    table = {}
    for k, v in INGREDIENTS.items():
        table["ingredient." + k] = v
    for k, v in DISHES.items():
        table["dish." + k] = v
    for k, v in ARCHETYPES.items():
        table["archetype." + k] = v
    for k, v in TRAITS.items():
        table["trait." + k] = v
    for k, v in TRAIT_DESC.items():
        table["trait." + k + ".desc"] = v
    for k, v in ROLES.items():
        table["role." + k] = v
    for k, v in STATIONS.items():
        table["station." + k] = v
    for k, v in CUISINES.items():
        table["cuisine." + k] = v
    for k, v in STORAGE.items():
        table["storage." + k] = v

    for rid, (name, job, beats) in REGULARS.items():
        table["regular." + rid + ".name"] = name
        table["regular." + rid + ".job"] = job
        for i, text in enumerate(beats):
            table["regular." + rid + ".beat" + str(i + 1)] = text

    table.update(UI)
    return table


def main():
    table = build()
    needed = required_keys()

    missing = sorted(k for k in needed if k not in table)
    # Arayuz anahtarlari icerikte gecmez; onlari fazlalik saymiyoruz.
    extra = sorted(k for k in table
                   if k not in needed and not SCREEN_KEY(k))

    print("icerigin istedigi : %d anahtar" % len(needed))
    print("tabloda           : %d anahtar (%d arayuz)"
          % (len(table), sum(1 for k in table if SCREEN_KEY(k))))

    if missing:
        print("")
        print("--- EKSIK METIN (%d) ---" % len(missing))
        for k in missing:
            print("  " + k)
    if extra:
        print("")
        print("--- ICERIKTE OLMAYAN ANAHTAR (%d) ---" % len(extra))
        for k in extra:
            print("  " + k)
    if missing or extra:
        return 1

    # Bos metin, eksik metinden daha sinsi: gorunurde anahtar var.
    blank = sorted(k for k, v in table.items() if not v or not v.strip())
    if blank:
        print("")
        print("--- BOS METIN (%d) ---" % len(blank))
        for k in blank:
            print("  " + k)
        return 1

    # --- KODDA GOMULU METIN VE OLU ANAHTAR ---------------------------------
    #
    # Bu iki denetim yillarca YOKTU ve ikisinin de bedeli olculdu:
    # docstring "yirmi bes kadar arayuz dizesi hala kodun icinde gomulu"
    # diye ITIRAF EDIYORDU ama hicbir sey kirmiyordu, sayi 40'a cikti;
    # SCREEN_KEY butun ui.* ailesini fazlalik denetiminden muaf tuttugu
    # icin de 15 olu anahtar iki dilde bakimi yapilip hic
    # gosterilmiyordu.
    gomulu = loc_tarama.gomulu_metinler()
    if gomulu:
        print("")
        print("--- KODDA GOMULU ARAYUZ METNI (%d) ---" % len(gomulu))
        for dosya, satir, metin in gomulu:
            print("  %s:%d  %s" % (dosya, satir, metin))
        return 1

    turkce = loc_tarama.turkce_dizeler()
    if turkce:
        print("")
        print("--- ARAYUZDE TURKCE SABIT DIZE (%d) ---" % len(turkce))
        for dosya, satir, metin in turkce:
            print("  %s:%d  %s" % (dosya, satir, metin))
        return 1

    olu = loc_tarama.olu_anahtarlar(table)
    if olu:
        print("")
        print("--- HICBIR EKRANIN ISTEMEDIGI ANAHTAR (%d) ---" % len(olu))
        for k in olu:
            print("  " + k)
        return 1

    # --- IKINCI DIL --------------------------------------------------------
    en = build_en()
    ayrisma = compare(table, en)
    if ayrisma:
        print("")
        print("--- DILLER AYRISMIS (%d) ---" % len(ayrisma))
        for x in ayrisma:
            print("  " + x)
        return 1

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(table, ensure_ascii=False, indent=2, sort_keys=True) + "\n")
    io.open(OUT_EN, "w", encoding="utf-8", newline="\n").write(
        json.dumps(en, ensure_ascii=False, indent=2, sort_keys=True) + "\n")
    print("")
    print("yazildi: content/loc/tr.json, content/loc/en.json (%d anahtar x 2 dil)"
          % len(table))
    print("butun metinler tam.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
