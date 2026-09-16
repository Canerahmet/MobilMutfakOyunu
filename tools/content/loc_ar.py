# -*- coding: utf-8 -*-
"""
Arapca metin tablosu. gen_loc.py content/loc/ar.json uretir.

MODERN STANDART ARAPCA (fusha). Kullanicinin istegi "ortak anlasilir
olsun" idi; lehce (Misir, Sam, Halic) secmek kitlenin bir kismini
disarida birakirdi. Fusha, yazili metinde her yerde okunuyor.

CEVIRI KARARLARI - loc_en.py cizgisiyle AYNI:

  Malzemeler TAMAMEN cevriliyor.

  Yemekler DEGISKEN. Arap mutfagiyla ORTAK olan adlar zaten Arapca:
  lahmacun -> لحم بعجين, doner -> شاورما, kofte -> كفتة, sutlac ->
  أرز بالحليب. Bunlar ceviri degil, AYNI YEMEGIN Arapcasi - ve bu,
  oyunun Arapca okuyan icin en dogal hali. Karsiligi olmayanlar
  betimleyici cevriliyor, tanindik Turkce ad parantezde kaliyor.

  Duzenli musterilerin sahneleri OYUNUN SESI: kisa, gundelik, tek bir
  sey anlatan cumleler.

  Ozel adlar LATIN HARFLERIYLE korunuyor (Hasan Usta). Arapca metinde
  Latin harfli ad birakmak, RTL akista yon karisikligi yaratabilir ama
  yirmi adi ses cevirisiyle yazmak onlari taninmaz hale getirirdi;
  unvan meslek satirinda aciklaniyor.

YON UYARISI: Arapca SAGDAN SOLA. Loc.IsRightToLeft bunu tasiyor;
Unity'nin bunu nasil cizdigi OLCULMEK zorunda (bkz. docs/54).
"""

INGREDIENTS = {
    "tuz": "ملح", "karabiber": "فلفل أسود", "zeytinyagi": "زيت زيتون",
    "aycicek_yagi": "زيت دوار الشمس", "un": "طحين", "sogan": "بصل",
    "sarimsak": "ثوم", "domates": "طماطم", "seker": "سكر",
    "sut": "حليب", "yumurta": "بيض", "tereyagi": "زبدة",
    "kiyma": "لحم مفروم", "tavuk_gogus": "صدر دجاج",
    "tavuk_kanat": "أجنحة دجاج",
    "doner_eti": "لحم شاورما", "balik_filetosu": "فيليه سمك",
    "sosis": "نقانق", "dana_kusbasi": "مكعبات لحم بقري",
    "kuzu_kusbasi": "مكعبات لحم ضأن", "kuzu_pirzola_et": "ريش ضأن",
    "iskembe": "كرشة", "burger_ekmek": "خبز برغر",
    "hotdog_ekmek": "خبز هوت دوغ", "tost_ekmegi": "خبز توست",
    "lavas": "خبز لافاش", "yufka": "عجين رقائق", "makarna": "معكرونة",
    "galeta_unu": "بقسماط", "misir_nisastasi": "نشا ذرة",
    "kabartma_tozu": "بيكنغ بودر", "maya": "خميرة", "irmik": "سميد",
    "kasar": "جبن قشقوان", "mozzarella": "موزاريلا", "yogurt": "لبن",
    "beyaz_peynir": "جبن أبيض", "dondurma_karisimi": "خليط مثلجات",
    "patates": "بطاطس", "marul": "خس", "lahana": "ملفوف",
    "havuc": "جزر", "jalapeno": "فلفل هالبينو", "tursu": "مخلل",
    "patlican": "باذنجان", "yesil_biber": "فلفل أخضر",
    "kabak": "كوسا", "bamya": "بامية", "taze_fasulye": "فاصولياء خضراء",
    "salatalik": "خيار", "maydanoz": "بقدونس",
    "kuru_fasulye_tane": "فاصولياء بيضاء", "nohut_tane": "حمص",
    "mercimek": "عدس", "bulgur": "برغل", "pirinc": "أرز",
    "burger_sos": "صلصة برغر", "acili_sos": "صلصة حارة",
    "ketcap": "كاتشب", "mayonez": "مايونيز", "salca": "معجون طماطم",
    "sirke": "خل", "baharat_karisimi": "خلطة بهارات",
    "kirmizi_biber": "فلفل أحمر مجروش", "kimyon": "كمون", "nane": "نعناع",
    "tarcin": "قرفة", "gazoz_surubu": "شراب غازي مركّز",
    "kola_surubu": "شراب كولا مركّز", "cay": "شاي", "limon": "ليمون",
    "elma": "تفاح", "cikolata": "شوكولاتة", "kakao": "كاكاو",
    "ceviz": "جوز", "kadayif_tel": "شعيرية كنافة",
    "vejetaryen_kofte": "قرص نباتي",
}

DISHES = {
    # fast food
    "hamburger": "برغر", "hot_dog": "هوت دوغ", "cizburger": "تشيز برغر",
    "kasarli_tost": "توست بالجبن", "tavuk_burger": "برغر دجاج",
    "duble_burger": "برغر مزدوج", "acili_burger": "برغر حار",
    "crispy_tavuk": "دجاج مقرمش", "tavuk_durum": "شاورما دجاج",
    "balik_burger": "برغر سمك", "vejetaryen_burger": "برغر نباتي",
    "et_durum": "شاورما لحم", "patates_kizartma": "بطاطس مقلية",
    "nugget": "ناغتس", "baharatli_patates": "بطاطس حارة",
    "yesil_salata": "سلطة خضراء", "sogan_halkasi": "حلقات بصل",
    "acili_kanat": "أجنحة حارة", "mozzarella_cubuk": "أصابع موزاريلا",
    "coleslaw": "سلطة ملفوف", "gazoz": "مشروب غازي", "kola": "كولا",
    "limonata": "ليموناضة", "milkshake": "ميلك شيك", "ayran": "عيران",
    "buzlu_cay": "شاي مثلج", "dondurma": "مثلجات",
    "elmali_turta": "فطيرة تفاح", "cikolatali_kek": "كيك شوكولاتة",
    "donut": "دونات", "brownie": "براوني", "waffle": "وافل",
    # turk
    "kuru_fasulye": "يخنة الفاصولياء البيضاء", "nohut": "يخنة الحمص",
    "etli_turlu": "يخنة لحم وخضار",
    "karniyarik": "باذنجان محشي باللحم",
    "taze_fasulye": "فاصولياء خضراء بالزيت",
    "imambayildi": "إمام بايلدي (باذنجان بالزيت)",
    "musakka": "مسقعة", "etli_bamya": "بامية باللحم",
    "patlican_kebabi": "كباب باذنجان",
    "mercimek_corbasi": "شوربة عدس",
    "ezogelin": "شوربة إيزوغلين", "yayla_corbasi": "شوربة لبن بالنعناع",
    "iskembe_corbasi": "شوربة كرشة", "pirinc_pilavi": "أرز مفلفل",
    "bulgur_pilavi": "برغل مفلفل", "borek": "بوريك",
    "manti": "منتي (عجين محشي باللحم)",
    "kofte": "كفتة", "tavuk_sis": "شيش طاووق",
    "adana": "كباب أضنة", "doner": "شاورما", "iskender": "إسكندر كباب",
    "kiymali_pide": "بيده باللحم المفروم", "lahmacun": "لحم بعجين",
    "kuzu_pirzola": "ريش ضأن مشوية", "coban_salata": "سلطة الراعي",
    "cacik": "جاجيك (لبن بالخيار)", "piyaz": "سلطة الفاصولياء",
    "sutlac": "أرز بالحليب", "kadayif": "كنافة", "revani": "بسبوسة",
}

ARCHETYPES = {
    "yalniz_musteri": "زبون بمفرده", "cift": "زوجان", "aile": "عائلة",
    "kurye": "عامل توصيل", "cocuklu_ebeveyn": "أب مع طفل",
    "yolcu": "مسافر", "paylasimci": "مجموعة تتشارك الطعام",
    "yemek_elestirmeni": "ناقد طعام",
    "aceleci_ogrenci": "طالب على عجل", "ofis_grubu": "مجموعة من مكتب",
    "antrenman_sonrasi": "بعد التمرين", "alisveris_molasi": "استراحة تسوق",
    "pazarlikci": "مساوم", "gec_saat_musterisi": "زبون متأخر",
    "mac_grubu": "جمهور مباراة", "diyet_yapan": "على حمية",
    "gece_vardiyasi": "وردية ليلية", "dogum_gunu_grubu": "حفلة عيد ميلاد",
    "sikayetci_musteri": "كثير الشكوى", "toplu_siparis": "طلب كبير",
    "esnaf_komsu": "صاحب محل مجاور",
    "ogle_molasi_calisani": "موظف في استراحة الغداء",
    "insaat_iscisi": "عامل بناء", "memur": "موظف حكومي",
    "emekli": "متقاعد", "ogrenci": "طالب",
    "hafta_sonu_ailesi": "عائلة في عطلة الأسبوع",
    "uzun_yol_soforu": "سائق شاحنة", "titiz_musteri": "زبون دقيق",
    "mahalle_toplu_yemegi": "مأدبة الحي",
    "denetim_gorevlisi": "مفتش صحي", "eski_musteri": "زبون قديم",
}

TRAITS = {
    "hizli_ama_daginik": "سريع لكن فوضوي",
    "yavas_ama_titiz": "بطيء لكن دقيق",
    "kalabalikta_panikleyen": "يرتبك في الزحمة",
    "sakin": "رابط الجأش",
    "musteriyle_iyi_anlasan": "يجيد التعامل مع الزبائن",
    "suratsiz": "عبوس",
    "cabuk_yorulan": "يتعب بسرعة",
    "dayanikli": "لا يكلّ",
    "ekip_moralini_yukselten": "يرفع معنويات الفريق",
    "huysuz": "سيّئ المزاج",
    "cirak": "متدرّب",
    "tecrubeli": "صاحب خبرة",
}

TRAIT_DESC = {
    "hizli_ama_daginik": "يقدّم بسرعة، ويتأخر في ترتيب الطاولات.",
    "yavas_ama_titiz": "يحسن تقديم الطبق، لكنه يستغرق وقتًا أطول.",
    "kalabalikta_panikleyen": "يبطؤ بوضوح في أكثر الساعات ازدحامًا.",
    "sakin": "الزحمة لا تصل إليه.",
    "musteriyle_iyi_anlasan": "إذا استلم الحساب غادر الزبون أكثر رضًا.",
    "suratsiz": "إذا استلم الحساب غادر الزبون أقل رضًا.",
    "cabuk_yorulan": "يبطؤ في الربع الأخير من اليوم.",
    "dayanikli": "يعمل بالوتيرة نفسها حتى الإغلاق.",
    "ekip_moralini_yukselten": "يرفع معنويات الفريق.",
    "huysuz": "يخفض معنويات الفريق.",
    "cirak": "رخيص، بطيء، سريع التعلّم.",
    "tecrubeli": "غالٍ، سريع، ولن يتحسّن أكثر.",
}

# Ses kurallari docs/53: davranisi adlandir kisiyi degil, aciklamayi
# esirge, duz ve kisa.
TRAIT_VOICE = {
    "hizli_ama_daginik": "يخرج الطلب بسرعة. وتنتهي عجلته عند الترتيب.",
    "yavas_ama_titiz": "ينظر إلى الطبق مرة أخرى قبل أن يتركه.",
    "kalabalikta_panikleyen": "إذا امتلأت الصالة ضاع منه الخيط.",
    "sakin": "تمر ساعة الذروة دون أن يرفع صوته.",
    "musteriyle_iyi_anlasan": "يذكر الزبائن اسمه وهم خارجون.",
    "suratsiz": "يعمل ولا يتكلم. بعض الطاولات تأخذها على محمل شخصي.",
    "cabuk_yorulan": "كلما تقدّم اليوم اتكأ على الطاولة أكثر.",
    "dayanikli": "يغلق اليوم بالخطوة التي فتحه بها.",
    "ekip_moralini_yukselten": "في الاستراحة يلتفّ الناس حوله.",
    "huysuz": "له مشكلة مع الجميع. وفي أكثرها على حق.",
    "cirak": "جديد على المهنة. تريه مرة فيبقى.",
    "tecrubeli": "ثلاثون سنة في هذا العمل. لم يعد يسأل عن جديد.",
}

ROLES = {
    "asci": "طبّاخ", "garson": "نادل",
    "bulasikci": "غسّال صحون", "kasiyer": "أمين صندوق",
}

STATIONS = {
    "ocak": "موقد", "izgara": "شواية", "firin": "فرن",
    "soguk": "قسم بارد", "icecek": "مشروبات", "tatli": "حلويات",
    "milkshake_makinesi": "آلة ميلك شيك",
    "waffle_makinesi": "آلة وافل",
    "tas_firin": "فرن حجري",
    "doner_ocagi": "سيخ الشاورما",
    "pide_firini": "فرن البيده",
}

CUISINES = {
    "fastfood": "وجبات سريعة",
    "turk": "مطعم تركي",
}

STORAGE = {"soguk_hava": "غرفة تبريد"}

REGULARS = {
    # --- turk ---
    "hasan_usta": ("Hasan Usta", "خرّاط في الشارع المقابل", [
        "ما إن يدخل حتى ينظر إلى المطبخ. «هل يوجد فاصولياء؟»",
        "لم يعد يقول طلبه. يجلس، وأنت تعرف.",
        "«ابني عاد من الجندية، سأحضره غدًا.»",
    ]),
    "nazife_teyze": ("Nazife Teyze", "جارة من البناية المجاورة", [
        "تأتي باكرًا، وتجلس عند النافذة.",
        "«كنت أطبخ بنفسي. أما الآن فآتي إلى هنا.»",
        "«قلت لزوجة ابني أن تتعلم شوربتك.»",
    ]),
    "selim_bey": ("Selim Bey", "محاسب في الطابق العلوي", [
        "يدخل والملف تحت ذراعه، ينظر إلى ساعته.",
        "«هنا يُؤكل في نصف ساعة. لهذا آتي.»",
        "«أحضرت زبونًا. ليرى أين آكل.»",
    ]),
    "rasim_amca": ("Rasim Amca", "سائق أجرة من الموقف", [
        "يترك السيارة عند الرصيف ويدخل مسرعًا.",
        "«الذين في الموقف يسألونني أين آكل.»",
        "«يوم تغلق، لا أعرف إلى أين أذهب.»",
    ]),
    "guler_hanim": ("Güler Hanım", "حلّاقة عند الناصية", [
        "تطلب وهي واقفة، والأرز سفري.",
        "«أقول لزبوناتي أن يعبرن الشارع.»",
        "توسّع محلها. «كبرنا معًا، يمكن القول.»",
    ]),
    "okan": ("Okan", "طالب ثانوية", [
        "يدخل والحقيبة على كتفه، يعدّ نقوده.",
        "«بما معي، يكفي لأكل هنا.»",
        "«نجحت في الامتحان. جئت أخبرك أولًا.»",
    ]),
    "nurten_abla": ("Nurten Abla", "خيّاطة في ورشة", [
        "تأتي وقت الغداء، دائمًا مع المجموعة نفسها.",
        "«في الورشة ثماني نساء. كلهن يسألن عنك.»",
        "«فتحنا ورشتنا الخاصة. من هنا بدأنا.»",
    ]),
    "ismail_sofor": ("İsmail Şoför", "سائق شاحنة على الخطوط الطويلة", [
        "يركن الشاحنة خلفًا، ويدخل ويداه ما زالتا متسختين.",
        "«أتوقف هنا في كل رحلة. صارت عادة.»",
        "«قلت لزملائي في المهنة: هذا هو المكان.»",
    ]),
    "perihan_hanim": ("Perihan Hanım", "معلّمة متقاعدة", [
        "تجلس وحدها، وتفتح الجريدة.",
        "«الأكل وحدي في البيت ليس أكلًا.»",
        "«طلابي القدامى يأتون. نتواعد هنا.»",
    ]),
    "mehmet_dede": ("Mehmet Dede", "أكبر أهل الحي سنًّا", [
        "يدخل على مهل، متكئًا على عصاه.",
        "«هذا المحل هنا من قبلي أنا.»",
        "«بارك الله في رزقك. أنا رأيت كل شيء.»",
    ]),
    # --- fast food ---
    "deniz": ("Deniz", "طالب ثانوية", [
        "يخرج من المدرسة، الحقيبة على كتفه، وهو على عجل.",
        "«صار هذا مكان اللقاء مع الأصدقاء.»",
        "نجح في الامتحان. «سنحتفل هنا، نحن ستة.»",
    ]),
    "burak": ("Burak", "مبرمج", [
        "يفتح حاسوبه، ويطلب دون أن يؤخر أحدًا.",
        "«نقلنا غداء الفريق إلى هنا.»",
        "«سأعمل عن بُعد، لكن هذا مكتبي عمليًا.»",
    ]),
    "elif": ("Elif", "بائعة في متجر", [
        "تأتي وبيدها أكياس التسوق، أمامها ربع ساعة.",
        "«رأيتها من الواجهة، أضفتم شيئًا جديدًا.»",
        "«قلت لبنات المتجر. صرنا نأخذ من عندكم.»",
    ]),
    "kaan_hoca": ("Kaan Hoca", "معلّم ثانوية", [
        "يأتي بعد الحصص، وما زال الدفتر بيده.",
        "«هنا آكل دون أن يراني الطلاب.»",
        "«أحضرت زملاء غرفة المعلمين. ليعرفوا المكان.»",
    ]),
    "sevda": ("Sevda", "ممرضة", [
        "تخرج من الوردية وتجلس دون أن تبدّل زيّها.",
        "«ساعة خروجي، لا يفتح غير هذا المكان.»",
        "«صار المستشفى كله يعرف هذا المحل.»",
    ]),
    "tolga": ("Tolga", "مدرّب لياقة", [
        "يدخل وحقيبة النادي بيده، يقرأ اللائحة كلها.",
        "«قل لي ما فيها، أنا أحسبها بنفسي.»",
        "«أرسل متدربيّ إلى هنا. أثق بالمكان.»",
    ]),
    "melis": ("Melis", "طالبة جامعية", [
        "تجلس إلى الطاولة الأخيرة وتفتح دفاترها.",
        "«في موسم الامتحانات أعيش هنا.»",
        "«تخرّجت. أول وجبة بعدها، هنا.»",
    ]),
    "ozan": ("Ozan", "متدرّب في شركة", [
        "أول من يصل وقت الغداء، ودائمًا وحده.",
        "«في الشركة أنا الجديد. هنا لا.»",
        "«ثبّتوني في العمل. هذه عليّ.»",
    ]),
    "yagmur": ("Yağmur", "مصمّمة غرافيك", [
        "تدخل والسمّاعات على أذنيها، وتطلب بالإشارة.",
        "«هنا لا يحادثني أحد. وهذا جيد.»",
        "«صرت أعمل لحسابي. هذه طاولتي.»",
    ]),
    "cem_abi": ("Cem Abi", "عامل توصيل بدرّاجة", [
        "الدرّاجة عند الباب، والخوذة بيده.",
        "«كتبت عنكم في مجموعة عمّال التوصيل.»",
        "«سأفتح محلي. هذه المهنة تعلّمتها منك.»",
    ]),
}

from loc_ar_ui import UI    # noqa: E402
