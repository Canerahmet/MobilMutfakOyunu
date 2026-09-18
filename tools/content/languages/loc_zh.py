# -*- coding: utf-8 -*-
"""
Chinese (Simplified) string table. gen_loc.py produces content/loc/zh.json.

SIMPLIFIED CHINESE (zh-Hans), the mainland standard. What the user asked
for was that it "be commonly understood"; the simplified script is the
choice that reaches the widest audience.

TRANSLATION DECISIONS - THE SAME line as loc_en.py and loc_es.py:

  Ingredients are translated IN FULL.

  Dishes VARY. Turkish names the world already knows are kept BY
  TRANSLITERATION with a short explanation set next to them: for
  somebody reading Chinese, "Lahmacun" on its own says nothing - but
  neither does "拉赫马俊", so the two go together:
  土耳其肉馅薄饼(Lahmacun). Descriptive names are translated directly.

  The regulars' scenes are THE GAME'S VOICE: short, everyday sentences
  that each tell one thing. Not word for word, but in the same tone.

  Proper names are kept IN LATIN LETTERS (Hasan Usta). Leaving a foreign
  name in Latin script inside Chinese text is common and it is read;
  transliterating would have made twenty names unrecognisable. The title
  is explained on the occupation line.

NOTE - IN THE CHINESE TABLE PROPER NAMES ARE WRITTEN WITHOUT DIACRITICS:

  Guler Hanim, Ismail Sofor, Yagmur ... (not Güler/Şoför/Yağmur)

The reason is twofold, and both halves were measured:
  1. The Chinese font (Noto Sans SC) DOES NOT CARRY Latin Extended-A -
     it has no ğ, İ, ı or Ş. While the game is in Chinese the whole
     interface is drawn with that font, so had we written the
     diacritics the player would have seen EMPTY BOXES.
  2. For somebody reading Chinese the difference between ğ and g does
     not carry information anyway; the name is still recognised.

In the Turkish, English, Spanish and Arabic tables the names stand with
ALL their diacritics - there Rubik does the drawing, and Rubik carries
them.

FONT WARNING: Rubik has NO Chinese glyphs. This language needs a font of
its own (see docs/54).
"""

INGREDIENTS = {
    "tuz": "盐", "karabiber": "黑胡椒", "zeytinyagi": "橄榄油",
    "aycicek_yagi": "葵花籽油", "un": "面粉", "sogan": "洋葱",
    "sarimsak": "大蒜", "domates": "番茄", "seker": "糖",
    "sut": "牛奶", "yumurta": "鸡蛋", "tereyagi": "黄油",
    "kiyma": "肉馅", "tavuk_gogus": "鸡胸肉",
    "tavuk_kanat": "鸡翅",
    "doner_eti": "旋转烤肉肉料", "balik_filetosu": "鱼柳",
    "sosis": "香肠", "dana_kusbasi": "牛肉块",
    "kuzu_kusbasi": "羊肉块", "kuzu_pirzola_et": "羊排",
    "iskembe": "牛肚", "burger_ekmek": "汉堡面包",
    "hotdog_ekmek": "热狗面包", "tost_ekmegi": "吐司面包",
    "lavas": "拉瓦什薄饼", "yufka": "酥皮", "makarna": "意面",
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
    "musakka": "穆萨卡(Musakka)", "etli_bamya": "羊肉炖秋葵",
    "patlican_kebabi": "茄子烤肉",
    "mercimek_corbasi": "扁豆汤",
    "ezogelin": "红扁豆辣汤(Ezogelin)", "yayla_corbasi": "酸奶薄荷汤",
    "iskembe_corbasi": "牛肚汤", "pirinc_pilavi": "土耳其黄油米饭(Pilav)",
    "bulgur_pilavi": "碎麦饭", "borek": "千层酥(Borek)",
    "manti": "土耳其小饺子(Manti)",
    "kofte": "土耳其肉丸(Kofte)", "tavuk_sis": "鸡肉串",
    "adana": "阿达纳烤肉(Adana)", "doner": "旋转烤肉(Doner)",
    "iskender": "伊斯肯德尔烤肉(Iskender)",
    "kiymali_pide": "肉馅船形饼", "lahmacun": "土耳其肉馅薄饼(Lahmacun)",
    "kuzu_pirzola": "烤羊排", "coban_salata": "牧羊人沙拉",
    "cacik": "酸奶黄瓜酱(Cacik)", "piyaz": "白豆沙拉(Piyaz)",
    "sutlac": "米布丁", "kadayif": "卡达伊夫甜点(Kadayif)", "revani": "粗麦糖浆蛋糕(Revani)",
}

ARCHETYPES = {
    "yalniz_musteri": "独自用餐", "cift": "情侣", "aile": "一家人",
    "kurye": "快递员", "cocuklu_ebeveyn": "带孩子的家长",
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

# The rules of the voice are docs/53: name the behaviour, not the person;
# spare the explanation; plain and short.
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
    "fritoz": "炸炉",
    "ocak": "灶台", "izgara": "烤架", "firin": "烤箱",
    "soguk": "冷菜台", "icecek": "饮品", "tatli": "甜点",
    "milkshake_makinesi": "奶昔机",
    "waffle_makinesi": "华夫饼机",
    "tas_firin": "石窑烤炉",
    "doner_ocagi": "旋转烤肉架",
    "pide_firini": "船形饼烤炉",
}

CUISINES = {
    "fastfood": "快餐店",
    "turk": "土耳其餐馆",
}

STORAGE = {"soguk_hava": "冷藏库"}

REGULARS = {
    # --- turk ---
    "hasan_usta": ("陈师傅", "对街的车工", [
        "一进门就往厨房看。“有炖豆子吗？”",
        "他不再报菜名了。坐下，你就知道。",
        "“我儿子当兵回来了，今晚带他来。”",
    ]),
    "nazife_teyze": ("张阿姨", "楼上的邻居", [
        "尝一口汤，什么也不说。明天还会来。",
        "“我做的以前也是这个味儿。好多年前了。”",
        "在门口停了停：“这条街现在就看你这家店了。”",
    ]),
    "selim_bey": ("李先生", "税务局的职员", [
        "同一张桌子，同一个钟点。一分钟都不差。",
        "“午饭四十分钟。在这儿三十五分钟就能走。”",
        "他说起退休。“那时候我就来得更勤了。”",
    ]),
    "rasim_amca": ("老王", "工地工头", [
        "手上还沾着石灰。坐下前先抖抖外套。",
        "“我也跟工人们说了——以后午饭就在这儿吃。”",
        "工地快完了。“我还是会常过来，你放心。”",
    ]),
    "guler_hanim": ("王姐", "街角的理发师", [
        "站着来站着走，米饭打包带上。",
        "“我跟客人说，过条街去他那儿。”",
        "她的店要扩了。“咱们是一起做大的。”",
    ]),
    "okan": ("小凯", "大学生", [
        "问菜单上最便宜的是哪个。",
        "“助学金下来了。”今天他还点了甜点。",
        "要去实习了。“第一份工资，我在这儿请客。”",
    ]),
    "nurten_abla": ("秀兰姐", "制衣作坊的领班", [
        "休息时间短。每次都把酸奶黄瓜酱带走。",
        "“作坊还有三个人要来——给我们留张桌子。”",
        "作坊要关了。“新地方远，可我还是会来。”",
    ]),
    "ismail_sofor": ("老马", "跑长途的货车司机", [
        "车停在街角，吃得快，走得也快。",
        "“从安卡拉回来，现在我总在这儿停一脚。”",
        "他在电台里说了：“还会有两个司机来找你。”",
    ]),
    "perihan_hanim": ("周老师", "退休教师", [
        "把叉子举到亮处看。一句话不说，但看了。",
        "“今天桌布是干净的。我注意到了。”",
        "“我这人不好伺候。这家店我喜欢。”",
    ]),
    "mehmet_dede": ("孙爷爷", "老馆子的老主顾", [
        "在门口犹豫了一下。“这儿以前是别人家的。”",
        "“那时候的鹰嘴豆就是这个味儿。一模一样。”",
        "现在他天天来。谁都知道哪把椅子是他的。",
    ]),
    # --- fastfood ---
    "deniz": ("小雨", "高中生", [
        "放学出来，背着包，赶时间。",
        "“现在大家碰头就在这儿。”",
        "考试过了。“就在这儿庆祝，我们六个。”",
    ]),
    "burak": ("小林", "软件开发", [
        "打开笔记本电脑，点单不含糊。",
        "“我们把团队午餐搬到这儿了。”",
        "“我以后远程办公，不过这儿算我办公室。”",
    ]),
    "elif": ("小娟", "店里的导购", [
        "拎着购物袋进来，只有十五分钟。",
        "“隔着橱窗看见的——你上新菜了。”",
        "“跟店里的姐妹说了。以后我们都订你家。”",
    ]),
    "cem_abi": ("阿杰", "摩托快递员", [
        "车停在门口，头盔拿在手里。",
        "“我把你拉进快递员的群了。”",
        "“我要自己开一家。手艺是跟你学的。”",
    ]),
    "melis": ("美玲", "自由职业会计", [
        "把价目表从头看到尾。",
        "“账我来记，菜你来做。”",
        "“我挺好奇你的利润率。不是开玩笑。”",
    ]),
    "ozan": ("小志", "业余足球运动员", [
        "比赛完了，全队一起来，闹哄哄的。",
        "“赢了我们就来这儿。你是我们的福星。”",
        "“杯赛拿下了。要不把你的名字印球衣上？”",
    ]),
    "sevda": ("雯雯", "营养师", [
        "问起沙拉：“酱能单独给吗？”",
        "“我把这家店推荐给我看的人。”",
        "“你的菜单现在贴在诊所墙上了。”",
    ]),
    "tolga": ("老杜", "上夜班的保安", [
        "半夜才来。没想到门还开着。",
        "“这个点儿只有你们家还开着。”",
        "“别的保安也开始来了。你发现没有？”",
    ]),
    "kaan_hoca": ("阿凯教练", "健身房教练", [
        "刚练完就过来，开口先问蛋白质。",
        "“我让我带的学员都来这儿吃。”",
        "“我把你的地址贴在健身房的板子上了，不介意吧。”",
    ]),
    "yagmur": ("雨桐", "夜校老师", [
        "来得晚，一脸疲惫。问有没有甜点。",
        "“这是我一天里唯一舒服的时候。”",
        "“课上完了。可已经成习惯了——我还会来。”",
    ]),
}

from loc_zh_ui import UI    # noqa: E402

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
    "秀兰", "建国", "爱华", "国强", "凤英", "建军", "玉梅", "志强",
    "桂芳", "立新", "丽娟", "永强", "秀英", "建华", "小燕", "德明",
    "淑芬", "宝国", "春花", "红军", "美玲", "文斌", "桂英", "海涛",
    "晓琳", "俊杰", "婷婷", "宇航", "佳怡", "子豪", "雨欣", "浩然",
    "梦琪", "泽宇", "欣怡", "博文", "语嫣", "皓轩", "诗涵", "梓豪",
    "若曦", "天佑", "思雨", "嘉豪", "雅静", "明轩", "静怡", "睿哲",
    "可欣", "俊熙", "紫萱", "承志", "沐阳", "书豪", "娅楠", "文昊",
    "曼婷", "宇轩", "佳琪", "鑫磊", "露露", "伟诚", "琳琳", "少辉",
    "小敏", "志远", "玉兰", "光明", "秀珍", "建平", "彩霞", "长春",
    "翠花", "大勇", "巧云", "福来", "招娣", "满仓", "月娥", "有才",
    "银花", "铁柱", "改兰", "水生", "菊香", "石头", "素芬", "平安",
    "惠敏", "晓峰", "洁琼", "亮亮", "雪梅", "阿强", "燕子", "小刚",
]
