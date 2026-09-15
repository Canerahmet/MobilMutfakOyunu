# -*- coding: utf-8 -*-
"""
Ingilizce metin tablosu. gen_loc.py bunu okuyup content/loc/en.json uretir.

NEDEN AYRI DOSYA, AYRI URETEC DEGIL:

Iki dil TEK uretecten cikiyor ve uretec ikisinin ANAHTARLARININ AYNI
oldugunu dogruluyor. Ayri bir arac, iki tablonun sessizce ayrismasi
demekti - bu projede ayni sayiyi iki yere yazmak bes kez ayristi ve
metinde ayrisma "[ui.staff.hire]" yazan bir dugme demek.

CEVIRI KARARLARI:

  Malzemeler TAMAMEN cevriliyor: betimleyiciler (Tuz -> Salt).

  Yemekler DEGISKEN. Turk mutfaginda dunyaca taninan adlar KORUNUYOR
  (Lahmacun, Doner, Iskender, Baklava gibi) cunku onlar ozel ad; bir
  Ingiliz menusunde de oyle yaziyor. Betimleyici olanlar CEVRILIYOR
  (Mercimek Corbasi -> Lentil Soup), cunku "Mercimek Corbasi" yazan bir
  satir Ingilizce oynayan icin bilgi tasimiyor. Korunan adlarin yaninda
  gerektiginde kisa bir aciklama var (Karniyarik -> "Karniyarik (Stuffed
  Aubergine)").

  Duzenli musterilerin sahneleri OYUNUN SESI. Birebir degil, ayni seyi
  ayni tonda soyleyecek sekilde cevrildi: kisa, gundelik, tek bir sey
  anlatan cumleler (docs/16 dokunus butcesi).

  Turkce ozel adlar (Hasan Usta, Nazife Teyze) KORUNUYOR; unvanlar
  Ingilizce okuyucuya anlam tasimadigi icin meslek satirinda aciklaniyor.
"""

# ---------------------------------------------------------------------------
# Malzeme adlari
# ---------------------------------------------------------------------------
INGREDIENTS = {
    "tuz": "Salt", "karabiber": "Black Pepper", "zeytinyagi": "Olive Oil",
    "aycicek_yagi": "Sunflower Oil", "un": "Flour", "sogan": "Onion",
    "sarimsak": "Garlic", "domates": "Tomato", "seker": "Sugar",
    "sut": "Milk", "yumurta": "Egg", "tereyagi": "Butter",
    "kiyma": "Minced Meat", "tavuk_gogus": "Chicken Breast",
    "tavuk_kanat": "Chicken Wings",
    "doner_eti": "Döner Meat", "balik_filetosu": "Fish Fillet",
    "sosis": "Sausage", "dana_kusbasi": "Diced Beef",
    "kuzu_kusbasi": "Diced Lamb", "kuzu_pirzola_et": "Lamb Chops",
    "iskembe": "Tripe", "burger_ekmek": "Burger Bun",
    "hotdog_ekmek": "Hot Dog Bun", "tost_ekmegi": "Sandwich Bread",
    "lavas": "Lavash", "yufka": "Filo Pastry", "makarna": "Pasta",
    "galeta_unu": "Breadcrumbs", "misir_nisastasi": "Cornflour",
    "kabartma_tozu": "Baking Powder", "maya": "Yeast", "irmik": "Semolina",
    "kasar": "Kaşar Cheese", "mozzarella": "Mozzarella", "yogurt": "Yoghurt",
    "beyaz_peynir": "White Cheese", "dondurma_karisimi": "Ice Cream Mix",
    "patates": "Potato", "marul": "Lettuce", "lahana": "Cabbage",
    "havuc": "Carrot", "jalapeno": "Jalapeño", "tursu": "Pickles",
    "patlican": "Aubergine", "yesil_biber": "Green Pepper",
    "kabak": "Courgette", "bamya": "Okra", "taze_fasulye": "Green Beans",
    "salatalik": "Cucumber", "maydanoz": "Parsley",
    "kuru_fasulye_tane": "Dried Beans", "nohut_tane": "Chickpeas",
    "mercimek": "Lentils", "bulgur": "Bulgur", "pirinc": "Rice",
    "burger_sos": "Burger Sauce", "acili_sos": "Hot Sauce",
    "ketcap": "Ketchup", "mayonez": "Mayonnaise", "salca": "Tomato Paste",
    "sirke": "Vinegar", "baharat_karisimi": "Spice Mix",
    "kirmizi_biber": "Red Pepper Flakes", "kimyon": "Cumin", "nane": "Mint",
    "tarcin": "Cinnamon", "gazoz_surubu": "Soda Syrup",
    "kola_surubu": "Cola Syrup", "cay": "Tea", "limon": "Lemon",
    "elma": "Apple", "cikolata": "Chocolate", "kakao": "Cocoa",
    "ceviz": "Walnuts", "kadayif_tel": "Shredded Kadayıf",
    "vejetaryen_kofte": "Veggie Patty",
}

# ---------------------------------------------------------------------------
# Yemek adlari
# ---------------------------------------------------------------------------
DISHES = {
    # fast food
    "hamburger": "Hamburger", "hot_dog": "Hot Dog", "cizburger": "Cheeseburger",
    "kasarli_tost": "Cheese Toastie", "tavuk_burger": "Chicken Burger",
    "duble_burger": "Double Burger", "acili_burger": "Spicy Burger",
    "crispy_tavuk": "Crispy Chicken", "tavuk_durum": "Chicken Wrap",
    "balik_burger": "Fish Burger", "vejetaryen_burger": "Veggie Burger",
    "et_durum": "Beef Wrap", "patates_kizartma": "Fries",
    "nugget": "Nuggets", "baharatli_patates": "Spicy Fries",
    "yesil_salata": "Green Salad", "sogan_halkasi": "Onion Rings",
    "acili_kanat": "Hot Wings", "mozzarella_cubuk": "Mozzarella Sticks",
    "coleslaw": "Coleslaw", "gazoz": "Fizzy Drink", "kola": "Cola",
    "limonata": "Lemonade", "milkshake": "Milkshake", "ayran": "Ayran",
    "buzlu_cay": "Iced Tea", "dondurma": "Ice Cream",
    "elmali_turta": "Apple Pie", "cikolatali_kek": "Chocolate Cake",
    "donut": "Donut", "brownie": "Brownie", "waffle": "Waffle",
    # turk -- taninan adlar korunuyor, betimleyiciler cevriliyor
    "kuru_fasulye": "Kuru Fasulye (Bean Stew)", "nohut": "Chickpea Stew",
    "etli_turlu": "Meat & Vegetable Stew",
    "karniyarik": "Karnıyarık (Stuffed Aubergine)",
    "taze_fasulye": "Green Bean Stew",
    "imambayildi": "İmambayıldı (Braised Aubergine)",
    "musakka": "Moussaka", "etli_bamya": "Okra with Lamb",
    "patlican_kebabi": "Aubergine Kebab",
    "mercimek_corbasi": "Lentil Soup",
    "ezogelin": "Ezogelin Soup", "yayla_corbasi": "Yoghurt & Mint Soup",
    "iskembe_corbasi": "Tripe Soup", "pirinc_pilavi": "Rice Pilaf",
    "bulgur_pilavi": "Bulgur Pilaf", "borek": "Börek", "manti": "Mantı",
    "kofte": "Köfte (Meatballs)", "tavuk_sis": "Chicken Şiş",
    "adana": "Adana Kebab", "doner": "Döner", "iskender": "İskender",
    "kiymali_pide": "Minced Meat Pide", "lahmacun": "Lahmacun",
    "kuzu_pirzola": "Lamb Chops", "coban_salata": "Shepherd's Salad",
    "cacik": "Cacık (Yoghurt Dip)", "piyaz": "Piyaz (Bean Salad)",
    "sutlac": "Rice Pudding", "kadayif": "Kadayıf", "revani": "Revani",
}

# ---------------------------------------------------------------------------
# Musteri arketipleri
# ---------------------------------------------------------------------------
ARCHETYPES = {
    "yalniz_musteri": "Solo Diner", "cift": "Couple", "aile": "Family",
    "kurye": "Courier", "cocuklu_ebeveyn": "Parent with Child",
    "yolcu": "Traveller", "paylasimci": "Sharing Group",
    "yemek_elestirmeni": "Food Critic",
    "aceleci_ogrenci": "Student in a Hurry", "ofis_grubu": "Office Group",
    "antrenman_sonrasi": "Post-Workout", "alisveris_molasi": "Shopping Break",
    "pazarlikci": "Haggler", "gec_saat_musterisi": "Late-Night Diner",
    "mac_grubu": "Match-Day Crowd", "diyet_yapan": "Dieter",
    "gece_vardiyasi": "Night Shift", "dogum_gunu_grubu": "Birthday Party",
    "sikayetci_musteri": "Complainer", "toplu_siparis": "Bulk Order",
    "esnaf_komsu": "Neighbouring Shopkeeper",
    "ogle_molasi_calisani": "Lunch-Break Worker",
    "insaat_iscisi": "Construction Worker", "memur": "Civil Servant",
    "emekli": "Pensioner", "ogrenci": "Student",
    "hafta_sonu_ailesi": "Weekend Family",
    "uzun_yol_soforu": "Long-Haul Driver", "titiz_musteri": "Fussy Diner",
    "mahalle_toplu_yemegi": "Neighbourhood Gathering",
    "denetim_gorevlisi": "Health Inspector", "eski_musteri": "Old Regular",
}

# ---------------------------------------------------------------------------
# Personel huylari, roller, istasyonlar
# ---------------------------------------------------------------------------
TRAITS = {
    "hizli_ama_daginik": "Fast but Messy",
    "yavas_ama_titiz": "Slow but Careful",
    "kalabalikta_panikleyen": "Panics in a Rush",
    "sakin": "Unflappable",
    "musteriyle_iyi_anlasan": "Good with People",
    "suratsiz": "Surly",
    "cabuk_yorulan": "Tires Quickly",
    "dayanikli": "Tireless",
    "ekip_moralini_yukselten": "Lifts the Team",
    "huysuz": "Bad-Tempered",
    "cirak": "Apprentice",
    "tecrubeli": "Experienced",
}

TRAIT_DESC = {
    "hizli_ama_daginik": "Quick to serve, slow to clear tables.",
    "yavas_ama_titiz": "Plates it better, but takes longer.",
    "kalabalikta_panikleyen": "Slows down noticeably at the busiest hour.",
    "sakin": "The rush doesn't touch them.",
    "musteriyle_iyi_anlasan": "Guests leave happier when they take the bill.",
    "suratsiz": "Guests leave less happy when they take the bill.",
    "cabuk_yorulan": "Slows down in the last quarter of the day.",
    "dayanikli": "Works at the same pace until closing.",
    "ekip_moralini_yukselten": "Pulls the team's morale up.",
    "huysuz": "Drags the team's morale down.",
    "cirak": "Cheap, slow, learns fast.",
    "tecrubeli": "Expensive, fast, won't improve further.",
}

# The trait's VOICE. Same two-string split as the Turkish table:
# TRAIT_DESC translates a number, TRAIT_VOICE describes a person.
#
# These are written as English rather than rendered from the Turkish, but
# the first draft was not: three lines carried a definite article on a
# body part with no possessor ("the hands get tangled", "the feet start
# talking"), which is Turkish possessive morphology showing through. Two
# more failed on antecedent - "Tables leave laughing when they carry the
# bill" parses as the tables carrying their own bill.
#
# Genderless throughout: the game's staff have no gender. Where a pronoun
# would be needed, the sentence is rebuilt so none is.
TRAIT_VOICE = {
    "hizli_ama_daginik": "Gets the order out fast. The hurry stops when it's time to clear.",
    "yavas_ama_titiz": "Checks the plate once more before letting it go.",
    "kalabalikta_panikleyen": "Loses the thread when the room fills up.",
    "sakin": "The busiest hour goes by without a raised voice.",
    "musteriyle_iyi_anlasan": "Guests say the name on their way out.",
    "suratsiz": "Does the work, says nothing. Some tables take it personally.",
    "cabuk_yorulan": "Leans on the counter more as the day goes on.",
    "dayanikli": "Still at the morning's pace when the shutters come down.",
    "ekip_moralini_yukselten": "People gather round on the break.",
    "huysuz": "Has a problem with everyone. Right about most of them.",
    "cirak": "New to this. Show it once and it stays.",
    "tecrubeli": "Thirty years in. Doesn't ask anything new.",
}


ROLES = {
    "asci": "Cook", "garson": "Waiter",
    "bulasikci": "Dishwasher", "kasiyer": "Cashier",
}

STATIONS = {
    "ocak": "Stove", "izgara": "Grill", "firin": "Oven",
    "soguk": "Cold Station", "icecek": "Drinks", "tatli": "Desserts",
    "milkshake_makinesi": "Milkshake Machine",
    "waffle_makinesi": "Waffle Iron",
    "tas_firin": "Stone Oven", "doner_ocagi": "Döner Grill",
    "pide_firini": "Pide Oven",
}

CUISINES = {"fastfood": "Fast Food", "turk": "Turkish Restaurant"}
STORAGE = {"soguk_hava": "Cold Store"}

# ---------------------------------------------------------------------------
# Isimli duzenli musteriler: ad, meslek ve UC SAHNE
#
# Adlar korunuyor. "Usta", "Teyze", "Abla", "Dede" unvanlari Ingilizce
# okuyucuya anlam tasimiyor ama adin PARCASI - birakiliyor ve meslek
# satiri kim olduklarini soyluyor.
# ---------------------------------------------------------------------------
REGULARS = {
    "hasan_usta": ("Hasan Usta", "Machinist across the street", [
        "Looks straight at the kitchen on the way in. “Any bean stew?”",
        "Doesn't order any more. He sits down; you know.",
        "“My son's back from the army. I'll bring him tonight.”",
    ]),
    "nazife_teyze": ("Nazife Teyze", "Neighbour from upstairs", [
        "Tastes the soup, says nothing. She'll be back tomorrow.",
        "“Mine used to taste like this. Years ago.”",
        "Pauses at the door: “This place is the face of the street now.”",
    ]),
    "selim_bey": ("Selim Bey", "Clerk at the tax office", [
        "Same table, same hour. Never a minute off.",
        "“Forty minutes for lunch. I'm out of here by thirty-five.”",
        "Talks about retiring. “Then I'll come more often.”",
    ]),
    "rasim_amca": ("Rasim Amca", "Site foreman", [
        "Hands dusty with lime. Shakes out his jacket before sitting.",
        "“Told the lads too — lunch is here from now on.”",
        "The site is finishing. “I'll still drop by, don't worry.”",
    ]),
    "guler_hanim": ("Güler Hanım", "Hairdresser on the corner", [
        "In and out on her feet, takes the pilaf away with her.",
        "“I tell my clients to go across the road to you.”",
        "Expanding her shop. “We grew together, you and I.”",
    ]),
    "okan": ("Okan", "University student", [
        "Asks what the cheapest thing on the menu is.",
        "“Grant came through.” Today he orders dessert too.",
        "Starting an internship. “First paycheque, I'm buying here.”",
    ]),
    "nurten_abla": ("Nurten Abla", "Foreman at a textile workshop", [
        "Short break. Always takes the cacık with her.",
        "“Three more coming from the workshop — keep us a table.”",
        "The workshop is closing. “New place is far, but I'll come.”",
    ]),
    "ismail_sofor": ("İsmail Şoför", "Long-haul lorry driver", [
        "Parks the lorry on the corner, eats fast, leaves.",
        "“Coming back from Ankara, I always stop here now.”",
        "Said it on the radio: “Two more drivers will ask for you.”",
    ]),
    "perihan_hanim": ("Perihan Hanım", "Retired teacher", [
        "Holds the fork up to the light. Says nothing, but looks.",
        "“The tablecloth is clean today. I noticed.”",
        "“I'm not easy to please. I like this place.”",
    ]),
    "mehmet_dede": ("Mehmet Dede", "Regular from the old restaurant", [
        "Hesitates at the door. “This used to be somebody else's.”",
        "“The chickpeas were like this back then. Exactly.”",
        "He comes every day now. Everyone knows which chair is his.",
    ]),
    "deniz": ("Deniz", "Secondary school student", [
        "Straight after school, backpack on one shoulder, in a hurry.",
        "“This is where we all meet up now.”",
        "Passed the exam. “Celebration's here. Six of us.”",
    ]),
    "burak": ("Burak", "Software developer", [
        "Opens his laptop, orders without making you wait.",
        "“We moved the team lunch here.”",
        "“I'll be remote now, but this is basically my office.”",
    ]),
    "elif": ("Elif", "Shop assistant", [
        "Arrives with shopping bags, has fifteen minutes.",
        "“Saw it through the window — you've added something new.”",
        "“Told the girls at the shop. We order from you now.”",
    ]),
    "kaan_hoca": ("Kaan Hoca", "Gym trainer", [
        "Straight after training, asking about protein.",
        "“I tell my clients to eat here.”",
        "“Pinned your address to the gym board. Hope that's alright.”",
    ]),
    "sevda": ("Sevda", "Dietitian", [
        "Asks about the salad: “Can I get the dressing on the side?”",
        "“I recommend this place to the people I see.”",
        "“Your menu is on the clinic wall now.”",
    ]),
    "tolga": ("Tolga", "Night-shift security guard", [
        "Comes at midnight. Surprised the door is open.",
        "“You're the only place open at this hour.”",
        "“The other guards started coming too. Did you notice?”",
    ]),
    "melis": ("Melis", "Freelance accountant", [
        "Reads the price list from top to bottom.",
        "“I'll keep the books, you make the food.”",
        "“I'm curious about your margins. Not joking.”",
    ]),
    "ozan": ("Ozan", "Amateur footballer", [
        "After the match, with the whole team, loud.",
        "“We come here when we win. You're our lucky charm.”",
        "“We took the cup. Shall we put your name on the shirt?”",
    ]),
    "yagmur": ("Yağmur", "Evening class teacher", [
        "Late, tired. Asks about dessert.",
        "“This is the one good moment of my day.”",
        "“Course is over. But it's a habit now — I'll be back.”",
    ]),
    "cem_abi": ("Cem Abi", "Motorbike courier", [
        "Bike out front, helmet in hand.",
        "“Put you in the couriers' group chat.”",
        "“Opening my own place. Learned the trade from you.”",
    ]),
}

# ---------------------------------------------------------------------------
# Arayuz metinleri AYRI DOSYADA (loc_en_ui.py): kaynaklari farkli.
# Icerik metinleri content/*.json'dan turiyor, arayuz metinleri elle
# yaziliyor - ayri tutmak hangisinin nereden geldigini belli ediyor.
# ---------------------------------------------------------------------------
from loc_en_ui import UI    # noqa: E402
