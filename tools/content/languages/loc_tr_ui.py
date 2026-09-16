# -*- coding: utf-8 -*-
"""
Turkish INTERFACE strings. loc_tr.py reads this.

Kept apart from the content strings (dishes, ingredients, guests)
because the sources differ: content strings follow from content/*.json,
these are written by hand.

FORMATTING PLACEHOLDERS ({0}, {1}) ARE CARRIED ACROSS AS THEY ARE. The
generator checks that every language holds the same placeholders: a
missing {0} is not a runtime formatting error, it is a SILENTLY missing
number.
"""

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
    "ui.staff.hall": "Salon",
    "ui.staff.fire_confirm": "Bu kişi çıkarılsın mı? Deneyimi de onunla gidiyor.",
    "ui.staff.hall_none": "Salon kadron yok. Bugünkü masa sayısı için gerek de yok.",
    "ui.staff.hall_needed": "Salon kadron yok — bugün {0} kişi gerekiyor.",
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
    "ui.staff.sink_none": "Kimse lavaboda değil — bulaşık birikince salondan biri geçer",
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
    "notice.plates_out_busy": "Temiz tabak bitti — lavabodaki yetişemiyor. "
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

    # UZUN KIDEM: mutfaga gore AYRI iki satir.
    #
    # Arastirma (docs/53): iki mekanin sikayeti de gururu da ayni degil.
    # Esnaf lokantasinda iliski USTAYA ve ise; zincirde VARDIYAYA ve
    # sisteme. Tek satir yazmak ikisini de genel yapardi.
    #
    # Ses mudavimlerle ayni kanal: ucuncu sahis, genis zaman, gozlem,
    # aciklamasiz. Ikisi de "beni gor" diyor ama sikayet etmeden -
    # Turkce kaynaklardaki asil dert gorunmezlik, ucret degil.
    "notice.tenure_lokanta": "{0} {1} gündür burada. Artık sormadan biliyor.",
    "notice.tenure_zincir": "{0} {1} gündür vardiyada. Yeni gelenler ona soruyor.",

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

    # --- haftalik karne ve nisanlar ---
    #
    # Bosluk sundu: oyun yedi eksende puan veriyor ve oyuncu onlari TAM
    # BIR KEZ goruyordu, altmisinci gunde. Goremedigin bir seyde ilerleme
    # hissedemezsin.
    #
    # Nisanlar GOREV DEGIL TANIMA: metinler bu yuzden gecmis zamanda ve
    # emir kipi YOK. "Zirveyi eksik kadroyla gec" bir gorevdir ve oyuncuyu
    # gorunmez bir patronun calisani yapar; "Zirveyi eksik kadroyla
    # gectin" onun kendi kararini goruyor.
    "ui.week.title": "{0}. hafta",
    "ui.week.note": "Geçen haftaya göre",
    "ui.badge.earned": "Yeni nişan",
    "ui.badge.title": "Nişanlar",
    "ui.badge.progress": "{0} / {1}",
    "ui.badge.have": "kazanıldı",
    "ui.badge.open": "henüz yok",

    "badge.full_house": "Kimse aç dönmedi",
    "badge.full_house.note": "Zirve gününde ne kapıdan dönen oldu ne masadan kızgın kalkan.",
    "badge.short_peak": "Zirveyi eksik kadroyla geçtin",
    "badge.short_peak.note": "Gerekenden az kişiyle çalıştın ve kimse masadan kızgın kalkmadı.",
    "badge.book_closed": "Defter kapandı",
    "badge.book_closed.note": "Verdiğin bütün veresiyeyi tahsil ettin.",
    "badge.first_beat": "Seni tanıdılar",
    "badge.first_beat.note": "Bir müdavimin hikâyesi ilk kez açıldı.",
    "badge.first_expand": "Dükkânı büyüttün",
    "badge.first_expand.note": "İlk kez bir kademe genişledin — daha çok masa, daha çok kira.",
    "badge.renowned": "Semtin konuştuğu lokanta",
    "badge.renowned.note": "İtibar 90'a çıktı.",
    "badge.first_ten_k": "Kasada on bin",
    "badge.first_ten_k.note": "Kasa ilk kez on bini gördü.",

    "ui.staff.busser": "Temizlikçi",
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
    "ui.common.hide": "gizle",
    "ui.common.show": "göster",
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
    # Artik `loc_scan.py` kiriyor.
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

    "ui.error.quit": "Oyunu kapat",
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
    "ui.credits.font_by": "Rubik + Noto Sans SC — SIL OFL 1.1",
    "ui.credits.font_copyright": "Copyright 2015 The Rubik Project Authors · Copyright 2014-2021 Adobe (Noto Sans SC)",

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
    "ui.morning.quality": "Malzeme kalitesi",
    "ui.quality.0": "Düşük",
    "ui.quality.1": "Standart",
    "ui.quality.2": "Yüksek",
    "ui.morning.quality_none": "Menüde yemek yok.",
    "ui.morning.quality_up": "Menündeki yemekler {0} puan daha memnun ediyor.",
    "ui.morning.quality_down": "Menündeki yemekler {0} puan daha az memnun ediyor.",
    "ui.morning.storage_tier": "Soğuk hava kademe {0}",
    "ui.morning.storage_none": "Soğuk hava yok — bozulan her şey gece gidiyor",
    "ui.menu.subtitle": "Menüde duran her yemek için stok tutuluyor.",
    "ui.menu.unlock_day": "{0}. günde açılıyor",
    "ui.staff.days": "{0} ({1} gün)",
    "ui.staff.inherited": "Huysuz — devraldığın aşçı",
    "ui.staff.inherited_hall": "Dükkânla birlikte geldi",
}
