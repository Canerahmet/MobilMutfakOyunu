# -*- coding: utf-8 -*-
"""
Cince (basitlestirilmis) metin tablosu. gen_loc.py content/loc/zh.json uretir.

BASITLESTIRILMIS CINCE (zh-Hans), anakara olcunu. Kullanicinin istegi
"ortak anlasilir olsun" idi; basitlestirilmis yazi en genis kitleye
ulasan secim.

CEVIRI KARARLARI - loc_en.py ve loc_es.py ile AYNI cizgi:

  Malzemeler TAMAMEN cevriliyor.

  Yemekler DEGISKEN. Dunyaca taninan Turkce adlar SES CEVIRISIYLE
  korunuyor ve yanina kisa aciklama konuyor: Cince okuyan biri icin
  "Lahmacun" tek basina hicbir sey soylemiyor, ama "拉赫马俊" de
  soylemiyor - o yuzden ikisi birlikte: 土耳其肉馅薄饼(Lahmacun).
  Betimleyici adlar dogrudan cevriliyor.

  Duzenli musterilerin sahneleri OYUNUN SESI: kisa, gundelik, tek bir
  sey anlatan cumleler. Birebir degil, ayni tonda.

  Ozel adlar LATIN HARFLERIYLE korunuyor (Hasan Usta). Cince metinde
  yabanci ad birakmak yaygin ve okunuyor; ses cevirisi yapmak, yirmi
  karakteri taninmaz hale getirirdi. Unvan meslek satirinda aciklaniyor.

NOT - CINCE TABLODA OZEL ADLAR ISARETSIZ YAZILIYOR:

  Guler Hanim, Ismail Sofor, Yagmur ... (Güler/Şoför/Yağmur degil)

Sebep ikili ve ikisi de olculdu:
  1. Cince yazi tipi (Noto Sans SC) Latin Extended-A TASIMIYOR - ğ, İ,
     ı, Ş onda yok. Oyun Cince'deyken butun arayuz o fontla ciziliyor,
     yani isaretli yazsak oyuncu BOS KUTU gorurdu.
  2. Cince okuyan biri icin ğ ile g arasindaki fark zaten bilgi
     tasimiyor; ad yine taniniyor.

Turkce, Ingilizce, Ispanyolca ve Arapca tablolarda adlar TAM
isaretleriyle duruyor - orada Rubik ciziyor ve Rubik onlari tasiyor.

YAZI TIPI UYARISI: Rubik'te Cince glif YOK. Bu dil ayri bir yazi tipi
gerektiriyor (bkz. docs/54).
"""

INGREDIENTS = {
    "tuz": "盐", "karabiber": "黑胡椒", "zeytinyagi": "橄榄油",
    "aycicek_yagi": "葵花籽油", "un": "面粉", "sogan": "洋葱",
    "sarimsak": "大蒜", "domates": "番茄", "seker": "糖",
    "sut": "牛奶", "yumurta": "鸡蛋", "tereyagi": "黄油",
    "kiyma": "肉馅", "tavuk_gogus": "鸡胸肉",
    "tavuk_kanat": "鸡翅",
    "doner_eti": "旋转烤肉", "balik_filetosu": "鱼柳",
    "sosis": "香肠", "dana_kusbasi": "牛肉块",
    "kuzu_kusbasi": "羊肉块", "kuzu_pirzola_et": "羊排",
    "iskembe": "牛肚", "burger_ekmek": "汉堡面包",
    "hotdog_ekmek": "热狗面包", "tost_ekmegi": "吐司面包",
    "lavas": "薄饼", "yufka": "酥皮", "makarna": "意面",
    "galeta_unu": "面包糠", "misir_nisastasi": "玉米淀粉",
    "kabartma_tozu": "泡打粉", "maya": "酵母", "irmik": "粗粒小麦粉",
    "kasar": "卡沙奶酪", "mozzarella": "马苏里拉", "yogurt": "酸奶",
    "beyaz_peynir": "白奶酪", "dondurma_karisimi": "冰淇淋浆",
    "patates": "土豆", "marul": "生菜", "lahana": "卷心菜",
    "havuc": "胡萝卜", "jalapeno": "墨西哥辣椒", "tursu": "腌菜",
    "patlican": "茄子", "yesil_biber": "青椒",
    "kabak": "西葫芦", "bamya": "秋葵", "taze_fasulye": "四季豆",
    "salatalik": "黄瓜", "maydanoz": "欧芹",
    "kuru_fasulye_tane": "干白豆", "nohut_tane": "鹰嘴豆",
    "mercimek": "扁豆", "bulgur": "碎麦", "pirinc": "大米",
    "burger_sos": "汉堡酱", "acili_sos": "辣酱",
    "ketcap": "番茄酱", "mayonez": "蛋黄酱", "salca": "番茄膏",
    "sirke": "醋", "baharat_karisimi": "混合香料",
    "kirmizi_biber": "辣椒碎", "kimyon": "孜然", "nane": "薄荷",
    "tarcin": "肉桂", "gazoz_surubu": "汽水糖浆",
    "kola_surubu": "可乐糖浆", "cay": "茶", "limon": "柠檬",
    "elma": "苹果", "cikolata": "巧克力", "kakao": "可可",
    "ceviz": "核桃", "kadayif_tel": "卡达伊夫细丝",
    "vejetaryen_kofte": "素肉饼",
}

DISHES = {
    # fast food
    "hamburger": "汉堡", "hot_dog": "热狗", "cizburger": "芝士汉堡",
    "kasarli_tost": "芝士烤三明治", "tavuk_burger": "鸡肉汉堡",
    "duble_burger": "双层汉堡", "acili_burger": "辣味汉堡",
    "crispy_tavuk": "香脆鸡", "tavuk_durum": "鸡肉卷",
    "balik_burger": "鱼堡", "vejetaryen_burger": "素汉堡",
    "et_durum": "牛肉卷", "patates_kizartma": "薯条",
    "nugget": "鸡块", "baharatli_patates": "辣味薯条",
    "yesil_salata": "蔬菜沙拉", "sogan_halkasi": "洋葱圈",
    "acili_kanat": "辣鸡翅", "mozzarella_cubuk": "芝士条",
    "coleslaw": "凉拌卷心菜", "gazoz": "汽水", "kola": "可乐",
    "limonata": "柠檬水", "milkshake": "奶昔", "ayran": "咸酸奶饮(Ayran)",
    "buzlu_cay": "冰茶", "dondurma": "冰淇淋",
    "elmali_turta": "苹果派", "cikolatali_kek": "巧克力蛋糕",
    "donut": "甜甜圈", "brownie": "布朗尼", "waffle": "华夫饼",
    # turk
    "kuru_fasulye": "炖白豆(Kuru Fasulye)", "nohut": "炖鹰嘴豆",
    "etli_turlu": "肉菜炖锅",
    "karniyarik": "肉馅酿茄子(Karniyarik)",
    "taze_fasulye": "炖四季豆",
    "imambayildi": "橄榄油焖茄子(Imambayildi)",
    "musakka": "慕沙卡", "etli_bamya": "羊肉炖秋葵",
    "patlican_kebabi": "茄子烤肉",
    "mercimek_corbasi": "扁豆汤",
    "ezogelin": "红扁豆辣汤(Ezogelin)", "yayla_corbasi": "酸奶薄荷汤",
    "iskembe_corbasi": "牛肚汤", "pirinc_pilavi": "土耳其手抓饭",
    "bulgur_pilavi": "碎麦饭", "borek": "千层酥(Borek)",
    "manti": "土耳其小饺子(Manti)",
    "kofte": "土耳其肉丸(Kofte)", "tavuk_sis": "鸡肉串",
    "adana": "阿达纳烤肉", "doner": "旋转烤肉(Doner)",
    "iskender": "伊斯肯德尔烤肉",
    "kiymali_pide": "肉馅船形饼", "lahmacun": "土耳其肉馅薄饼(Lahmacun)",
    "kuzu_pirzola": "烤羊排", "coban_salata": "牧羊人沙拉",
    "cacik": "酸奶黄瓜汁(Cacik)", "piyaz": "白豆沙拉(Piyaz)",
    "sutlac": "米布丁", "kadayif": "卡达伊夫甜点", "revani": "粗麦糖浆蛋糕",
}

ARCHETYPES = {
    "yalniz_musteri": "独自用餐", "cift": "情侣", "aile": "一家人",
    "kurye": "送餐员", "cocuklu_ebeveyn": "带孩子的家长",
    "yolcu": "旅客", "paylasimci": "分食的一群人",
    "yemek_elestirmeni": "美食评论家",
    "aceleci_ogrenci": "赶时间的学生", "ofis_grubu": "办公室同事",
    "antrenman_sonrasi": "健身后", "alisveris_molasi": "购物间隙",
    "pazarlikci": "爱讲价的", "gec_saat_musterisi": "深夜客人",
    "mac_grubu": "看球的一群人", "diyet_yapan": "正在节食",
    "gece_vardiyasi": "夜班工人", "dogum_gunu_grubu": "生日聚会",
    "sikayetci_musteri": "爱抱怨的", "toplu_siparis": "大单外带",
    "esnaf_komsu": "邻街店主",
    "ogle_molasi_calisani": "午休的上班族",
    "insaat_iscisi": "建筑工人", "memur": "公务员",
    "emekli": "退休老人", "ogrenci": "学生",
    "hafta_sonu_ailesi": "周末家庭",
    "uzun_yol_soforu": "长途司机", "titiz_musteri": "挑剔的客人",
    "mahalle_toplu_yemegi": "街坊聚餐",
    "denetim_gorevlisi": "卫生检查员", "eski_musteri": "老主顾",
}

TRAITS = {
    "hizli_ama_daginik": "手快但邋遢",
    "yavas_ama_titiz": "慢但仔细",
    "kalabalikta_panikleyen": "一忙就乱",
    "sakin": "沉得住气",
    "musteriyle_iyi_anlasan": "会跟客人打交道",
    "suratsiz": "脸色不好",
    "cabuk_yorulan": "容易累",
    "dayanikli": "不知疲倦",
    "ekip_moralini_yukselten": "带动士气",
    "huysuz": "脾气差",
    "cirak": "学徒",
    "tecrubeli": "老手",
}

TRAIT_DESC = {
    "hizli_ama_daginik": "上菜快，收桌慢。",
    "yavas_ama_titiz": "摆盘更好，但更费时间。",
    "kalabalikta_panikleyen": "最忙的时段明显变慢。",
    "sakin": "再忙也影响不到他。",
    "musteriyle_iyi_anlasan": "由他结账，客人走得更满意。",
    "suratsiz": "由他结账，客人走得没那么满意。",
    "cabuk_yorulan": "一天最后四分之一会变慢。",
    "dayanikli": "到打烊都是一个节奏。",
    "ekip_moralini_yukselten": "把团队士气往上带。",
    "huysuz": "把团队士气往下拉。",
    "cirak": "便宜、慢、学得快。",
    "tecrubeli": "贵、快、不会再进步。",
}

# Ses kurallari docs/53: davranisi adlandir kisiyi degil, aciklamayi
# esirge, duz ve kisa.
TRAIT_VOICE = {
    "hizli_ama_daginik": "出餐很快。轮到收桌，急劲就没了。",
    "yavas_ama_titiz": "盘子放手前还要再看一眼。",
    "kalabalikta_panikleyen": "厅里一满，手脚就乱了。",
    "sakin": "最忙的那个钟头，他连嗓门都没抬。",
    "musteriyle_iyi_anlasan": "客人走的时候会叫他的名字。",
    "suratsiz": "干活不说话。有的桌子往心里去。",
    "cabuk_yorulan": "天越晚，靠柜台的次数越多。",
    "dayanikli": "关门时的步子还是开门时那个。",
    "ekip_moralini_yukselten": "休息的时候，人都围到他那边。",
    "huysuz": "跟谁都有意见。多半还占理。",
    "cirak": "刚来的。教一遍就记住了。",
    "tecrubeli": "干了三十年。不再问新东西了。",
}

ROLES = {
    "asci": "厨师", "garson": "服务员",
    "bulasikci": "洗碗工", "kasiyer": "收银员",
}

STATIONS = {
    "ocak": "灶台", "izgara": "烤架", "firin": "烤箱",
    "soguk": "冷菜台", "icecek": "饮品", "tatli": "甜点",
    "milkshake_makinesi": "奶昔机",
    "waffle_makinesi": "华夫饼机",
    "tas_firin": "石板烤炉",
    "doner_ocagi": "旋转烤肉架",
    "pide_firini": "船形饼烤炉",
}

CUISINES = {
    "fastfood": "快餐",
    "turk": "土耳其餐馆",
}

STORAGE = {"soguk_hava": "冷藏库"}

REGULARS = {
    # --- turk ---
    "hasan_usta": ("Hasan Usta", "对街的车工师傅", [
        "一进门就往厨房看。“有炖豆子吗？”",
        "他不再报菜名了。坐下，你就知道。",
        "“我儿子当兵回来了，明天带他来。”",
    ]),
    "nazife_teyze": ("Nazife Teyze", "隔壁楼的邻居", [
        "来得早，坐在靠窗的位子。",
        "“以前都是我自己做饭。现在来这儿。”",
        "“我跟儿媳说了，让她学学你这汤。”",
    ]),
    "selim_bey": ("Selim Bey", "楼上的会计", [
        "夹着文件夹进来，看了看表。",
        "“这儿半小时能吃完。所以我来。”",
        "“我带了个客户。让他看看我在哪儿吃饭。”",
    ]),
    "rasim_amca": ("Rasim Amca", "车站的出租车司机", [
        "车停在路边，人快步进来。",
        "“站上的人都问我在哪儿吃。”",
        "“你关门的日子，我都不知道去哪儿。”",
    ]),
    "guler_hanim": ("Guler Hanim", "街角的理发师", [
        "站着点单，米饭要打包。",
        "“我跟我的客人说，让她们过街来。”",
        "店面扩大了。“可以说，我们是一起长大的。”",
    ]),
    "okan": ("Okan", "高中生", [
        "背着书包进来，数了数零钱。",
        "“我这点钱，在这儿够吃一顿。”",
        "“我考上了。先来告诉你一声。”",
    ]),
    "nurten_abla": ("Nurten Abla", "制衣作坊的裁缝", [
        "午饭点到，总是同一群人。",
        "“作坊里八个人。都问起你。”",
        "“我们自己开作坊了。是从这儿开始的。”",
    ]),
    "ismail_sofor": ("Ismail Sofor", "长途货车司机", [
        "车停在后面，手还没洗干净就进来了。",
        "“每趟都在这儿歇脚。成习惯了。”",
        "“我跟同行说了，就认这一家。”",
    ]),
    "perihan_hanim": ("Perihan Hanim", "退休教师", [
        "一个人坐下，摊开报纸。",
        "“一个人在家吃，那不叫吃饭。”",
        "“以前的学生也来。我们约在这儿。”",
    ]),
    "mehmet_dede": ("Mehmet Dede", "街坊里最年长的", [
        "走得慢，拄着拐杖进来。",
        "“这家店在这儿的年头，比我还久。”",
        "“愿你生意兴隆。我什么都见过了。”",
    ]),
    # --- fast food ---
    "deniz": ("Deniz", "高中生", [
        "放学出来，背着包，赶时间。",
        "“跟朋友碰头就在这儿。”",
        "考试过了。“就在这儿庆祝，我们六个。”",
    ]),
    "burak": ("Burak", "程序员", [
        "打开笔记本电脑，点单不含糊。",
        "“我们把团队午餐搬到这儿了。”",
        "“我以后远程办公，不过这儿算我办公室。”",
    ]),
    "elif": ("Elif", "店铺导购", [
        "拎着购物袋进来，只有十五分钟。",
        "“我从橱窗看到了，你们上新品了。”",
        "“我跟店里的姑娘们说了。我们现在都来这儿。”",
    ]),
    "kaan_hoca": ("Kaan Hoca", "高中老师", [
        "下课后来，手里还拿着点名册。",
        "“在这儿吃饭，学生看不见我。”",
        "“我把办公室的同事带来了。让他们也认认门。”",
    ]),
    "sevda": ("Sevda", "护士", [
        "下了班，制服都没换就坐下了。",
        "“我下班的点，就这一家开着。”",
        "“医院里都知道这个地方了。”",
    ]),
    "tolga": ("Tolga", "私人教练", [
        "拎着健身包进来，把菜单从头看到尾。",
        "“你告诉我里面有什么，我自己算。”",
        "“我让我的学员都来这儿。我信得过。”",
    ]),
    "melis": ("Melis", "大学生", [
        "坐在最里头那桌，摊开笔记。",
        "“考试季我就住这儿了。”",
        "“我毕业了。毕业后第一顿，在这儿吃。”",
    ]),
    "ozan": ("Ozan", "实习生", [
        "午饭点第一个到，总是一个人。",
        "“在公司我是新人。在这儿不是。”",
        "“我转正了。这顿我请。”",
    ]),
    "yagmur": ("Yagmur", "平面设计师", [
        "戴着耳机进来，用手势点单。",
        "“这儿没人跟我搭话。挺好。”",
        "“我自己接活儿了。这是我的桌子。”",
    ]),
    "cem_abi": ("Cem Abi", "摩托快递员", [
        "车停在门口，头盔拿在手里。",
        "“我把你们发到快递员群里了。”",
        "“我自己开店了。这行是跟你学的。”",
    ]),
}

from loc_zh_ui import UI    # noqa: E402
