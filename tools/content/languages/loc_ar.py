# -*- coding: utf-8 -*-
"""
Arabic string table. gen_loc.py produces content/loc/ar.json.

MODERN STANDARD ARABIC (fusha). What the user asked for was that it "be
commonly understood"; picking a dialect (Egyptian, Levantine, Gulf)
would have left part of the audience outside. Fusha is read everywhere
in written text.

TRANSLATION DECISIONS - THE SAME line as loc_en.py:

  Ingredients are translated IN FULL.

  Dishes VARY. The names that Arab cooking SHARES are Arabic already:
  lahmacun -> لحم بعجين, doner -> شاورما, kofte -> كفتة, sutlac ->
  أرز بالحليب. These are not translations, they are the Arabic for THE
  SAME DISH - and that is the most natural form of the game for somebody
  reading Arabic. The ones with no counterpart are translated
  descriptively, with the familiar Turkish name left in brackets.

  The regulars' scenes are THE GAME'S VOICE: short, everyday sentences
  that each tell one thing.

  Proper names are kept IN LATIN LETTERS (Hasan Usta). Leaving a
  Latin-script name inside Arabic text can create a muddle of direction
  in the RTL flow, but writing twenty names by transliteration would
  have made them unrecognisable; the title is explained on the
  occupation line.

DIRECTION WARNING: Arabic runs FROM RIGHT TO LEFT. Loc.IsRightToLeft
carries this; how Unity draws it HAS to be measured (see docs/54).
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
    "kasar": "جبن قشقوان", "mozzarella": "موزاريلا", "yogurt": "لبن رائب",
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
    "ezogelin": "شوربة إيزوغلين", "yayla_corbasi": "شوربة لبن رائب بالنعناع",
    "iskembe_corbasi": "شوربة كرشة", "pirinc_pilavi": "أرز مفلفل",
    "bulgur_pilavi": "برغل مفلفل", "borek": "بوريك",
    "manti": "مانتي (عجين محشي باللحم)",
    "kofte": "كفتة", "tavuk_sis": "شيش طاووق",
    "adana": "كباب أضنة", "doner": "شاورما", "iskender": "كباب إسكندر",
    "kiymali_pide": "بيدة باللحم المفروم", "lahmacun": "لحم بعجين",
    "kuzu_pirzola": "ريش ضأن مشوية", "coban_salata": "سلطة الراعي",
    "cacik": "جاجيك (لبن رائب بالخيار)", "piyaz": "سلطة الفاصولياء",
    "sutlac": "أرز بالحليب", "kadayif": "كنافة", "revani": "بسبوسة",
}

ARCHETYPES = {
    "yalniz_musteri": "زبون بمفرده", "cift": "زوجان", "aile": "عائلة",
    "kurye": "عامل توصيل", "cocuklu_ebeveyn": "أحد الوالدين مع طفل",
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
    "uzun_yol_soforu": "سائق شاحنة", "titiz_musteri": "زبون صعب الإرضاء",
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

# The rules of the voice are docs/53: name the behaviour, not the person;
# spare the explanation; plain and short.
TRAIT_VOICE = {
    "hizli_ama_daginik": "يخرج الطلب بسرعة. وتنتهي عجلته عند الترتيب.",
    "yavas_ama_titiz": "ينظر إلى الطبق مرة أخرى قبل أن يتركه.",
    "kalabalikta_panikleyen": "إذا امتلأت الصالة ضاع منه الخيط.",
    "sakin": "تمر ساعة الذروة دون أن يرفع صوته.",
    "musteriyle_iyi_anlasan": "يذكر الزبائن اسمه وهم خارجون.",
    "suratsiz": "يعمل ولا يتكلم. بعض الطاولات تأخذ الأمر على محمل شخصي.",
    "cabuk_yorulan": "كلما تقدّم اليوم اتكأ على المنضدة أكثر.",
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
    "fritoz": "المقلاة",
    "ocak": "موقد", "izgara": "شواية", "firin": "فرن",
    "soguk": "قسم بارد", "icecek": "مشروبات", "tatli": "حلويات",
    "milkshake_makinesi": "آلة ميلك شيك",
    "waffle_makinesi": "آلة وافل",
    "tas_firin": "فرن حجري",
    "doner_ocagi": "سيخ الشاورما",
    "pide_firini": "فرن البيدة",
}

CUISINES = {
    "fastfood": "وجبات سريعة",
    "turk": "مطعم تركي",
}

STORAGE = {"soguk_hava": "غرفة تبريد"}

REGULARS = {
    # --- turk ---
    "hasan_usta": ("الأسطى حسن", "خرّاط في الشارع المقابل", [
        "ما إن يدخل حتى ينظر إلى المطبخ. «هل يوجد فاصولياء؟»",
        "لم يعد يطلب. يجلس، وأنت تعرف.",
        "«عاد ابني من الخدمة. سأحضره الليلة.»",
    ]),
    "nazife_teyze": ("الخالة سميرة", "جارة من الطابق العلوي", [
        "تتذوّق الشوربة ولا تقول شيئًا. ستعود غدًا.",
        "«كان طعم شوربتي هكذا. قبل سنوات.»",
        "تتوقّف عند الباب: «صار هذا المكان وجه الشارع.»",
    ]),
    "selim_bey": ("الأستاذ سليم", "موظّف في دائرة الضرائب", [
        "الطاولة نفسها، والساعة نفسها. لا يتأخّر دقيقة.",
        "«لديّ أربعون دقيقة للغداء. أخرج من هنا بعد خمس وثلاثين.»",
        "يتحدّث عن التقاعد. «عندها سآتي أكثر.»",
    ]),
    "rasim_amca": ("عم راسم", "رئيس عمّال في ورشة بناء", [
        "يداه مغبرّتان بالجير. ينفض سترته قبل أن يجلس.",
        "«قلت للشباب أيضًا: الغداء من الآن هنا.»",
        "الورشة توشك أن تنتهي. «سأمرّ عليك رغم ذلك، لا تقلق.»",
    ]),
    "guler_hanim": ("السيدة جميلة", "صاحبة صالون الحلاقة في الزاوية", [
        "تدخل وتخرج واقفة، وتأخذ الأرز معها.",
        "«أقول لزبوناتي: اعبرن الشارع إليه.»",
        "توسّع محلّها. «كبرنا معًا، أنا وأنت.»",
    ]),
    "okan": ("عمر", "طالب جامعي", [
        "يسأل عن أرخص ما في اللائحة.",
        "«وصلت المنحة.» واليوم يطلب الحلوى أيضًا.",
        "يبدأ تدريبه. «مع أول راتب، الحساب عليّ هنا.»",
    ]),
    "nurten_abla": ("الأخت نورا", "رئيسة عمّال في ورشة نسيج", [
        "استراحة قصيرة. تأخذ الجاجيك معها دائمًا.",
        "«سيأتي ثلاثة آخرون من الورشة — احجز لنا طاولة.»",
        "الورشة تغلق. «المكان الجديد بعيد، لكني سآتي.»",
    ]),
    "ismail_sofor": ("إسماعيل السائق", "سائق شاحنة على الخطوط الطويلة", [
        "يركن الشاحنة في الزاوية، يأكل بسرعة، ويمضي.",
        "«في طريق العودة من أنقرة صرت أتوقّف هنا دائمًا.»",
        "قالها عبر اللاسلكي: «سيسأل عنك سائقان آخران.»",
    ]),
    "perihan_hanim": ("المعلّمة بريهان", "معلّمة متقاعدة", [
        "ترفع الشوكة نحو الضوء. لا تقول شيئًا، لكنها تنظر.",
        "«المفرش نظيف اليوم. لاحظت ذلك.»",
        "«إرضائي ليس سهلًا. هذا المكان يعجبني.»",
    ]),
    "mehmet_dede": ("الحاج محمد", "زبون المطعم القديم", [
        "يتردّد عند الباب. «كان هذا المكان لغيرك.»",
        "«كان الحمّص هكذا في ذلك الزمن. تمامًا.»",
        "صار يأتي كل يوم. والجميع يعرف أيّ كرسي كرسيه.",
    ]),
    # --- fastfood ---
    "deniz": ("ديمة", "طالب ثانوية", [
        "من المدرسة مباشرة، الحقيبة على كتف واحد، على عجل.",
        "«صرنا نلتقي كلّنا هنا.»",
        "نجح في الامتحان. «الاحتفال هنا. نحن ستة.»",
    ]),
    "burak": ("بلال", "مطوّر برمجيات", [
        "يفتح حاسوبه ويطلب دون أن يُبقيك منتظرًا.",
        "«نقلنا غداء الفريق إلى هنا.»",
        "«صرت أعمل عن بُعد، لكن هذا المكان صار مكتبي عمليًا.»",
    ]),
    "elif": ("آلاء", "بائعة في متجر", [
        "تصل وأكياس التسوّق بيدها، لديها خمس عشرة دقيقة.",
        "«رأيته من الواجهة — أضفت شيئًا جديدًا.»",
        "«أخبرت زميلاتي في المتجر. صرنا نطلب منك.»",
    ]),
    "cem_abi": ("أبو كريم", "عامل توصيل بدرّاجة", [
        "الدرّاجة أمام الباب، والخوذة في يده.",
        "«أضفتك إلى مجموعة عمّال التوصيل.»",
        "«سأفتح محلّي الخاص. تعلّمت المهنة منك.»",
    ]),
    "melis": ("ميساء", "محاسبة مستقلّة", [
        "تقرأ قائمة الأسعار من أوّلها إلى آخرها.",
        "«أنا أمسك الدفاتر وأنت تصنع الطعام.»",
        "«يثير فضولي هامش ربحك. لست أمزح.»",
    ]),
    "ozan": ("أسامة", "لاعب كرة هاوٍ", [
        "بعد المباراة، مع الفريق كلّه، بصوت عالٍ.",
        "«نأتي إلى هنا حين نفوز. أنت حظّنا.»",
        "«أخذنا الكأس. هل نضع اسمك على القميص؟»",
    ]),
    "sevda": ("سعاد", "أخصائية تغذية", [
        "تسأل عن السلطة: «هل تضع الصلصة جانبًا؟»",
        "«أنصح من أتابعهم بهذا المكان.»",
        "«صارت لائحتك على جدار العيادة.»",
    ]),
    "tolga": ("طلال", "حارس أمن في الوردية الليلية", [
        "يأتي عند منتصف الليل. يفاجئه أن الباب مفتوح.",
        "«أنتم المكان الوحيد المفتوح في هذه الساعة.»",
        "«بدأ الحرّاس الآخرون يأتون أيضًا. هل لاحظت؟»",
    ]),
    "kaan_hoca": ("الكابتن كنان", "مدرّب في النادي الرياضي", [
        "من التمرين مباشرة، يسأل عن البروتين.",
        "«أقول لمن أدرّبهم أن يأكلوا هنا.»",
        "«علّقت عنوانك على لوحة النادي. أرجو ألّا تمانع.»",
    ]),
    "yagmur": ("غيداء", "معلّمة دروس مسائية", [
        "متأخّرة ومتعبة. تسأل عن الحلوى.",
        "«هذه اللحظة الجيدة الوحيدة في يومي.»",
        "«انتهى الدرس. لكنها صارت عادة — سأعود.»",
    ]),
}

from loc_ar_ui import UI    # noqa: E402

# ---------------------------------------------------------------- staff
# NINETY-SIX NAMES, INDEX-ALIGNED WITH content/names.json.
#
# The core stores a staff member's name as an INDEX into that list and
# the save carries the index, so a row here is the same PERSON in every
# language - switching language renames the cook rather than replacing
# them. Any list of a different length would break that silently, so
# tools/content/gen_loc.py checks the length.
#
# The Turkish list alternates female and male and spans three
# generations, so a crew of twelve reads as a neighbourhood and not as a
# spreadsheet. This one keeps that shape.
STAFF = [
    "نورا", "حسن", "عائشة", "محمد", "فاطمة", "مصطفى", "أمينة", "أحمد",
    "خديجة", "علي", "زينب", "حسين", "شريفة", "إبراهيم", "ألفت", "عثمان",
    "حواء", "يوسف", "سلطانة", "رمضان", "مريم", "كمال", "كلثوم", "خليل",
    "سعاد", "نبيل", "سناء", "نادر", "بثينة", "سامي", "رانيا", "ضياء",
    "بشرى", "قاسم", "وفاء", "طارق", "منال", "غسان", "جميلة", "كريم",
    "لبنى", "رامي", "دعاء", "مروان", "تغريد", "باسم", "جمانة", "غالب",
    "كريمة", "ضرار", "شيماء", "لؤي", "سحر", "وائل", "غادة", "فادي",
    "عبير", "سليم", "نسرين", "ماهر", "رغد", "وسيم", "ليلى", "قصي",
    "مرام", "إلياس", "بلقيس", "غيث", "تمارا", "رؤوف", "دلال", "صابر",
    "نجلاء", "عصام", "صفاء", "جمال", "فدوى", "داوود", "وداد", "بدر",
    "رشا", "لطفي", "بيان", "أنس", "كنانة", "عمار", "منى", "رفيق",
    "إسراء", "ديب", "غفران", "وليد", "إيناس", "رضا", "علياء", "فايز",
]
