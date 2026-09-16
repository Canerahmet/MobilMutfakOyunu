# -*- coding: utf-8 -*-
"""
Turkish string table. gen_loc.py reads this and writes content/loc/tr.json.

WHY THIS TABLE IS TURKISH AND THE REST OF THE REPOSITORY IS NOT:

Turkish is one of the five languages the GAME ships in, so these VALUES
are data, not source - the same as the Chinese in loc_zh.py. CLAUDE.md
exempts this file by name and tools/check_english.py enforces that
exemption. Everything around the values - keys, comments, the generator
- is English.

WHY IT LIVES HERE RATHER THAN INSIDE gen_loc.py:

It used to sit in the generator itself, and that made two claims false
at once. The exemption in CLAUDE.md pointed at a file that did not
exist, so the Turkish was in a file the English check was still
measuring; and gen_loc.py could not be translated without moving the
player's text first. Turkish is now in the same shape as the other four
languages: <content> + <_ui>, read through one generator that proves the
five tables have not drifted apart.
"""

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

# HUYUN SESI: kisiyi kisi yapan cumle.
#
# TRAIT_DESC mekanigi anlatiyor ("gunun son ceyreginde yavaslar") ve
# bir SAYININ cevirisi. Bu tablo ayni huyu bir INSAN olarak anlatiyor.
# Ikisi ayri dize, cunku ayri isler yapiyorlar - Two Point Hospital'in
# `Cheap` mekanigi ile "Will work for peanuts" metni gibi.
#
# NEDEN GEREKLIYDI: yirmi muMdavimin ucer sahnesi var, personelin SIFIR
# satiri vardi. Oyunun butun yari-anlatili personel metni
# `ui.staff.inherited` idi.
#
# SESIN KURALLARI (mudavim repliklerinden ve arastirmadan, docs/53):
#   - Ucuncu sahis, genis zaman, gozlem. Mudavimlerle ayni kanal.
#   - DAVRANISI adlandir, KISIYI degil. "Huysuz"un satiri onu kotu
#     ilan etmiyor; RimWorld'un "finds obligations confining" kalibi.
#   - Aciklamayi esirge. Bir seyi soylememek kisiyi kurduran sey.
#   - Simulasyonun yalanlayabilecegi hicbir sey soyleme.
#   - Duz ve kisa. Sirinlige uzanan replik yirminci gunde katlanilmaz
#     olur; sevk edilmis bark yazisinin tek ortak uyarisi bu.
TRAIT_VOICE = {
    # HAVUZ KORLUGU: bu satirlar HEM ASCIYA HEM SALONA dusuyor.
    #
    # Ilk yazimda iki satir simulasyonun YALANLADIGI seyi soyluyordu:
    #   - "Tezgahin haline bakma" mutfagi isaret ediyordu, oysa
    #     CleanlinessBp yalnizca TraitSum(1, ...) ile, yani SALONDA masa
    #     toplarken okunuyor. Asciya dusunce huyun bedeli hic yok.
    #   - "O mutfaktayken" diyordu, oysa MoraleAura iki havuzdan da
    #     toplaniyor ve bulasikciya da dusuyor.
    #
    # Kural (docs/53): simulasyonun yalanlayabilecegi hicbir sey soyleme.
    # Satirlar artik havuzdan bagimsiz.
    "hizli_ama_daginik": "Siparişi çabuk çıkarıyor. Toplamaya sıra gelince acelesi bitiyor.",
    "yavas_ama_titiz": "Tabağı bırakmadan bir kere daha bakıyor.",
    # Deyim "eli ayagina dolasmak". Ilk yazim "elleri birbirine
    # dolaniyor" idi - deyimin yarim hatirlanmis hali, yanlis uzuv ve
    # yanlis fiil. Bir dili bilen ya deyimi kullanir ya hic kullanmaz.
    "kalabalikta_panikleyen": "Salon dolunca eli ayağına dolaşıyor.",
    "sakin": "En kalabalık saatte sesi bile yükselmiyor.",
    "musteriyle_iyi_anlasan": "Masadan kalkarken adıyla teşekkür ediyorlar.",
    "suratsiz": "İşini yapar, konuşmaz. Bazı masalar üstüne alınıyor.",
    # "Ayaklari konusmaya basliyor" Ingilizce bir deyimin kalibiydi.
    # "Evening" da oyunun kendi asama adi; etki servisin son ceyreginde.
    "cabuk_yorulan": "Gün ilerledikçe tezgâha daha çok yaslanıyor.",
    "dayanikli": "Kapanışta da sabahki hızında.",
    "ekip_moralini_yukselten": "Molada etrafına toplanıyorlar.",
    "huysuz": "Herkesle bir derdi var. Çoğunda da haklı.",
    "cirak": "Daha yeni. Bir kere gösterince aklında kalıyor.",
    # "Yeni bir sey sormuyor" xpBp 0'in kendisi: ogrenecegi kalmamis.
    "tecrubeli": "Otuz yıldır bu işte. Yeni bir şey sormuyor.",
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
# Interface strings live in a SEPARATE FILE (loc_tr_ui.py): their source
# differs. Content strings follow from content/*.json, interface strings
# are written by hand - keeping them apart makes it obvious which is which.
# ---------------------------------------------------------------------------
from loc_tr_ui import UI    # noqa: E402
