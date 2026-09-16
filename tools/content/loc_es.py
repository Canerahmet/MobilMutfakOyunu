# -*- coding: utf-8 -*-
"""
Ispanyolca metin tablosu. gen_loc.py bunu okuyup content/loc/es.json uretir.

CEVIRI KARARLARI - hepsi loc_en.py'deki kararlarin AYNISI, cunku
ayrismalari halinde bir dilde "Lahmacun" digerinde "Pizza turca" yazardi:

  Malzemeler TAMAMEN cevriliyor.

  Yemekler DEGISKEN. Dunyaca taninan Turkce adlar KORUNUYOR (Lahmacun,
  Doner, Iskender, Baklava); betimleyiciler cevriliyor. Korunan adlarin
  yaninda gerektiginde kisa bir aciklama var.

  Duzenli musterilerin sahneleri OYUNUN SESI: kisa, gundelik, tek bir
  sey anlatan cumleler. Birebir degil, ayni tonda.

  Ozel adlar (Hasan Usta, Nazife Teyze) KORUNUYOR; unvan meslek
  satirinda aciklaniyor.

NOTR ISPANYOLCA. Kullanicinin istegi "ortak anlasilir olsun" idi. Bu
yuzden yereli belli secenekler yerine her yerde anlasilan sozcuk
seciliyor:
  - "computadora"/"ordenador" yerine yansizi; "movil"/"celular"
    gerekmiyor zaten.
  - Ikinci tekil sahis "tu" (voseo yok) ve cogul "ustedes" degil, oyun
    zaten oyuncuya tekil sesleniyor.
  - "patatas"/"papas" ikileminde menude "Papas fritas" degil
    "Patatas fritas" degil - ikisini de bilen "Papas fritas" daha genis
    bir kitlede anlasiliyor (Latin Amerika + Ispanya'da taninir).
"""

# ---------------------------------------------------------------------------
# Malzeme adlari
# ---------------------------------------------------------------------------
INGREDIENTS = {
    "tuz": "Sal", "karabiber": "Pimienta Negra", "zeytinyagi": "Aceite de Oliva",
    "aycicek_yagi": "Aceite de Girasol", "un": "Harina", "sogan": "Cebolla",
    "sarimsak": "Ajo", "domates": "Tomate", "seker": "Azúcar",
    "sut": "Leche", "yumurta": "Huevo", "tereyagi": "Mantequilla",
    "kiyma": "Carne Picada", "tavuk_gogus": "Pechuga de Pollo",
    "tavuk_kanat": "Alitas de Pollo",
    "doner_eti": "Carne de Döner", "balik_filetosu": "Filete de Pescado",
    "sosis": "Salchicha", "dana_kusbasi": "Ternera en Cubos",
    "kuzu_kusbasi": "Cordero en Cubos", "kuzu_pirzola_et": "Chuletas de Cordero",
    "iskembe": "Callos", "burger_ekmek": "Pan de Hamburguesa",
    "hotdog_ekmek": "Pan de Hot Dog", "tost_ekmegi": "Pan de Molde",
    "lavas": "Pan Lavash", "yufka": "Masa Filo", "makarna": "Pasta",
    "galeta_unu": "Pan Rallado", "misir_nisastasi": "Maicena",
    "kabartma_tozu": "Polvo de Hornear", "maya": "Levadura", "irmik": "Sémola",
    "kasar": "Queso Kaşar", "mozzarella": "Mozzarella", "yogurt": "Yogur",
    "beyaz_peynir": "Queso Blanco", "dondurma_karisimi": "Base de Helado",
    "patates": "Papa", "marul": "Lechuga", "lahana": "Col",
    "havuc": "Zanahoria", "jalapeno": "Jalapeño", "tursu": "Encurtidos",
    "patlican": "Berenjena", "yesil_biber": "Pimiento Verde",
    "kabak": "Calabacín", "bamya": "Okra", "taze_fasulye": "Ejotes",
    "salatalik": "Pepino", "maydanoz": "Perejil",
    "kuru_fasulye_tane": "Frijoles Secos", "nohut_tane": "Garbanzos",
    "mercimek": "Lentejas", "bulgur": "Bulgur", "pirinc": "Arroz",
    "burger_sos": "Salsa para Hamburguesa", "acili_sos": "Salsa Picante",
    "ketcap": "Kétchup", "mayonez": "Mayonesa", "salca": "Pasta de Tomate",
    "sirke": "Vinagre", "baharat_karisimi": "Mezcla de Especias",
    "kirmizi_biber": "Hojuelas de Chile", "kimyon": "Comino", "nane": "Menta",
    "tarcin": "Canela", "gazoz_surubu": "Jarabe de Gaseosa",
    "kola_surubu": "Jarabe de Cola", "cay": "Té", "limon": "Limón",
    "elma": "Manzana", "cikolata": "Chocolate", "kakao": "Cacao",
    "ceviz": "Nueces", "kadayif_tel": "Kadayıf en Hebras",
    "vejetaryen_kofte": "Medallón Vegetal",
}

# ---------------------------------------------------------------------------
# Yemek adlari
# ---------------------------------------------------------------------------
DISHES = {
    # fast food
    "hamburger": "Hamburguesa", "hot_dog": "Hot Dog",
    "cizburger": "Hamburguesa con Queso",
    "kasarli_tost": "Sándwich de Queso", "tavuk_burger": "Hamburguesa de Pollo",
    "duble_burger": "Hamburguesa Doble", "acili_burger": "Hamburguesa Picante",
    "crispy_tavuk": "Pollo Crujiente", "tavuk_durum": "Wrap de Pollo",
    "balik_burger": "Hamburguesa de Pescado",
    "vejetaryen_burger": "Hamburguesa Vegetal",
    "et_durum": "Wrap de Carne", "patates_kizartma": "Papas Fritas",
    "nugget": "Nuggets", "baharatli_patates": "Papas Picantes",
    "yesil_salata": "Ensalada Verde", "sogan_halkasi": "Aros de Cebolla",
    "acili_kanat": "Alitas Picantes", "mozzarella_cubuk": "Palitos de Mozzarella",
    "coleslaw": "Ensalada de Col", "gazoz": "Gaseosa", "kola": "Cola",
    "limonata": "Limonada", "milkshake": "Malteada", "ayran": "Ayran",
    "buzlu_cay": "Té Helado", "dondurma": "Helado",
    "elmali_turta": "Tarta de Manzana", "cikolatali_kek": "Pastel de Chocolate",
    "donut": "Dona", "brownie": "Brownie", "waffle": "Waffle",
    # turk -- taninan adlar korunuyor, betimleyiciler cevriliyor
    "kuru_fasulye": "Kuru Fasulye (Guiso de Frijoles)",
    "nohut": "Guiso de Garbanzos",
    "etli_turlu": "Guiso de Carne y Verduras",
    "karniyarik": "Karnıyarık (Berenjena Rellena)",
    "taze_fasulye": "Guiso de Ejotes",
    "imambayildi": "İmambayıldı (Berenjena Guisada)",
    "musakka": "Musaca", "etli_bamya": "Okra con Cordero",
    "patlican_kebabi": "Kebab de Berenjena",
    "mercimek_corbasi": "Sopa de Lentejas",
    "ezogelin": "Sopa Ezogelin", "yayla_corbasi": "Sopa de Yogur y Menta",
    "iskembe_corbasi": "Sopa de Callos", "pirinc_pilavi": "Arroz Pilaf",
    "bulgur_pilavi": "Bulgur Pilaf", "borek": "Börek", "manti": "Mantı",
    "kofte": "Köfte (Albóndigas)", "tavuk_sis": "Brochetas de Pollo",
    "adana": "Kebab Adana", "doner": "Döner", "iskender": "İskender",
    "kiymali_pide": "Pide de Carne Picada", "lahmacun": "Lahmacun",
    "kuzu_pirzola": "Chuletas de Cordero", "coban_salata": "Ensalada del Pastor",
    "cacik": "Cacık (Salsa de Yogur)", "piyaz": "Piyaz (Ensalada de Frijoles)",
    "sutlac": "Arroz con Leche", "kadayif": "Kadayıf", "revani": "Revani",
}

# ---------------------------------------------------------------------------
# Musteri arketipleri
# ---------------------------------------------------------------------------
ARCHETYPES = {
    "yalniz_musteri": "Comensal Solo", "cift": "Pareja", "aile": "Familia",
    "kurye": "Repartidor", "cocuklu_ebeveyn": "Padre con Niño",
    "yolcu": "Viajero", "paylasimci": "Grupo que Comparte",
    "yemek_elestirmeni": "Crítico Gastronómico",
    "aceleci_ogrenci": "Estudiante con Prisa", "ofis_grubu": "Grupo de Oficina",
    "antrenman_sonrasi": "Después del Gimnasio",
    "alisveris_molasi": "Pausa de Compras",
    "pazarlikci": "Regateador", "gec_saat_musterisi": "Cliente Nocturno",
    "mac_grubu": "Hinchada del Partido", "diyet_yapan": "A Dieta",
    "gece_vardiyasi": "Turno de Noche", "dogum_gunu_grubu": "Fiesta de Cumpleaños",
    "sikayetci_musteri": "Quejoso", "toplu_siparis": "Pedido Grande",
    "esnaf_komsu": "Comerciante Vecino",
    "ogle_molasi_calisani": "Trabajador en su Pausa",
    "insaat_iscisi": "Obrero", "memur": "Funcionario",
    "emekli": "Jubilado", "ogrenci": "Estudiante",
    "hafta_sonu_ailesi": "Familia de Fin de Semana",
    "uzun_yol_soforu": "Camionero", "titiz_musteri": "Comensal Exigente",
    "mahalle_toplu_yemegi": "Comida del Barrio",
    "denetim_gorevlisi": "Inspector Sanitario", "eski_musteri": "Cliente de Siempre",
}

# ---------------------------------------------------------------------------
# Personel huylari, roller, istasyonlar
# ---------------------------------------------------------------------------
TRAITS = {
    "hizli_ama_daginik": "Rápido pero Desordenado",
    "yavas_ama_titiz": "Lento pero Cuidadoso",
    "kalabalikta_panikleyen": "Se Bloquea en el Apuro",
    "sakin": "Imperturbable",
    "musteriyle_iyi_anlasan": "Bueno con la Gente",
    "suratsiz": "Seco",
    "cabuk_yorulan": "Se Cansa Pronto",
    "dayanikli": "Incansable",
    "ekip_moralini_yukselten": "Levanta al Equipo",
    "huysuz": "De Mal Genio",
    "cirak": "Aprendiz",
    "tecrubeli": "Con Experiencia",
}

TRAIT_DESC = {
    "hizli_ama_daginik": "Sirve rápido, recoge las mesas tarde.",
    "yavas_ama_titiz": "Emplata mejor, pero tarda más.",
    "kalabalikta_panikleyen": "Baja el ritmo en la hora más cargada.",
    "sakin": "El apuro no le llega.",
    "musteriyle_iyi_anlasan": "Los clientes se van más contentos si él cobra.",
    "suratsiz": "Los clientes se van menos contentos si él cobra.",
    "cabuk_yorulan": "Baja el ritmo en el último cuarto del día.",
    "dayanikli": "Trabaja al mismo ritmo hasta el cierre.",
    "ekip_moralini_yukselten": "Sube el ánimo del equipo.",
    "huysuz": "Baja el ánimo del equipo.",
    "cirak": "Barato, lento, aprende rápido.",
    "tecrubeli": "Caro, rápido, ya no mejora más.",
}

# Sesin kurallari loc_en.py ve docs/53 ile ayni: davranisi adlandir,
# kisiyi degil; aciklamayi esirge; duz ve kisa.
TRAIT_VOICE = {
    "hizli_ama_daginik": "Saca el pedido rápido. La prisa se acaba al recoger.",
    "yavas_ama_titiz": "Mira el plato una vez más antes de soltarlo.",
    "kalabalikta_panikleyen": "Pierde el hilo cuando se llena el salón.",
    "sakin": "La hora pico pasa sin que levante la voz.",
    "musteriyle_iyi_anlasan": "Los clientes dicen su nombre al salir.",
    "suratsiz": "Hace lo suyo y no habla. Alguna mesa se lo toma a mal.",
    "cabuk_yorulan": "Se apoya más en el mostrador según avanza el día.",
    "dayanikli": "Cierra el día con el mismo paso con que lo abrió.",
    "ekip_moralini_yukselten": "En el descanso la gente se junta a su lado.",
    "huysuz": "Tiene algo contra todos. En casi todo lleva razón.",
    "cirak": "Recién llegado. Se lo enseñas una vez y se le queda.",
    "tecrubeli": "Treinta años en esto. Ya no pregunta nada nuevo.",
}

ROLES = {
    "asci": "Cocinero", "garson": "Mesero",
    "bulasikci": "Lavaplatos", "kasiyer": "Cajero",
}

STATIONS = {
    "ocak": "Fogón", "izgara": "Parrilla", "firin": "Horno",
    "soguk": "Estación Fría", "icecek": "Bebidas", "tatli": "Postres",
    "milkshake_makinesi": "Máquina de Malteadas",
    "waffle_makinesi": "Wafflera",
    "tas_firin": "Horno de Piedra",
    "doner_ocagi": "Asador de Döner",
    "pide_firini": "Horno de Pide",
}

CUISINES = {
    "fastfood": "Comida Rápida",
    "turk": "Restaurante Turco",
}

STORAGE = {"soguk_hava": "Cámara Frigorífica"}

# ---------------------------------------------------------------------------
# Duzenli musteriler: ad, meslek, uc sahne
# ---------------------------------------------------------------------------
REGULARS = {
    # --- turk ---
    "hasan_usta": ("Hasan Usta", "Tornero de la calle de enfrente", [
        "Nada más entrar mira a la cocina. “¿Hay frijoles?”",
        "Ya no dice lo que quiere. Se sienta, y tú lo sabes.",
        "“Mi hijo volvió del servicio, mañana lo traigo.”",
    ]),
    "nazife_teyze": ("Nazife Teyze", "Vecina del edificio de al lado", [
        "Viene temprano, se sienta junto a la ventana.",
        "“Antes cocinaba yo. Ahora vengo aquí.”",
        "“A mi nuera le dije que aprendiera de tu sopa.”",
    ]),
    "selim_bey": ("Selim Bey", "Contador del piso de arriba", [
        "Entra con la carpeta bajo el brazo, mira el reloj.",
        "“Aquí se come en media hora. Por eso vengo.”",
        "“Traje a un cliente. Que vea dónde como yo.”",
    ]),
    "rasim_amca": ("Rasim Amca", "Taxista de la parada", [
        "Deja el coche en doble fila, entra deprisa.",
        "“Los de la parada me preguntan dónde como.”",
        "“Cuando cierras, no sé adónde ir.”",
    ]),
    "guler_hanim": ("Güler Hanım", "Peluquera de la esquina", [
        "Viene de pie, pide el arroz para llevar.",
        "“A mis clientas les digo que crucen la calle.”",
        "Amplía su local. “Crecimos juntos, digamos.”",
    ]),
    "okan": ("Okan", "Estudiante de instituto", [
        "Entra con la mochila al hombro, cuenta las monedas.",
        "“Aquí alcanza para comer con lo que tengo.”",
        "“Aprobé el examen. Vine a contártelo primero.”",
    ]),
    "nurten_abla": ("Nurten Abla", "Costurera del taller", [
        "Llega a la hora del almuerzo, siempre con el mismo grupo.",
        "“En el taller somos ocho. Todas preguntan por ti.”",
        "“Abrimos nuestro propio taller. Empezamos aquí.”",
    ]),
    "ismail_sofor": ("İsmail Şoför", "Camionero de larga distancia", [
        "Aparca el camión atrás, entra con las manos aún sucias.",
        "“Paro aquí en cada viaje. Ya es costumbre.”",
        "“Les dije a los del gremio que este es el sitio.”",
    ]),
    "perihan_hanim": ("Perihan Hanım", "Maestra jubilada", [
        "Se sienta sola, saca el periódico.",
        "“Comer en casa sola no es comer.”",
        "“Mis antiguos alumnos vienen. Aquí quedamos.”",
    ]),
    "mehmet_dede": ("Mehmet Dede", "El mayor del barrio", [
        "Entra despacio, se apoya en el bastón.",
        "“Este local lleva aquí más que yo.”",
        "“Que Dios te dé abundancia. Yo ya vi de todo.”",
    ]),
    # --- fast food ---
    "deniz": ("Deniz", "Estudiante de instituto", [
        "Sale de clase, mochila al hombro, con prisa.",
        "“Este es el punto de encuentro con los amigos.”",
        "Aprobó el examen. “Lo celebramos aquí, somos seis.”",
    ]),
    "burak": ("Burak", "Programador", [
        "Abre el portátil y pide sin hacer esperar.",
        "“Trajimos aquí el almuerzo del equipo.”",
        "“Trabajaré a distancia, pero esta es mi oficina.”",
    ]),
    "elif": ("Elif", "Asesora de tienda", [
        "Llega con bolsas de compras, tiene quince minutos.",
        "“Lo vi en el escaparate, han puesto algo nuevo.”",
        "“Se lo dije a las chicas de la tienda. Ya venimos aquí.”",
    ]),
    "kaan_hoca": ("Kaan Hoca", "Profesor de instituto", [
        "Viene después de clase, aún con la lista en la mano.",
        "“Aquí puedo comer sin que me vean los alumnos.”",
        "“Traje a los del claustro. Que lo conozcan.”",
    ]),
    "sevda": ("Sevda", "Enfermera", [
        "Sale del turno, se sienta sin quitarse el uniforme.",
        "“Es lo único abierto cuando salgo.”",
        "“En el hospital ya saben este sitio.”",
    ]),
    "tolga": ("Tolga", "Entrenador personal", [
        "Entra con la bolsa del gimnasio, mira el menú de arriba abajo.",
        "“Dime qué lleva, yo hago la cuenta.”",
        "“Mando aquí a mis alumnos. Me fío.”",
    ]),
    "melis": ("Melis", "Universitaria", [
        "Se sienta en la mesa del fondo, abre los apuntes.",
        "“En época de exámenes vivo aquí.”",
        "“Me gradué. La primera comida fuera, aquí.”",
    ]),
    "ozan": ("Ozan", "Chico de las prácticas", [
        "Llega el primero a la hora de comer, siempre solo.",
        "“En la oficina soy el nuevo. Aquí no.”",
        "“Me hicieron fijo. Invito yo.”",
    ]),
    "yagmur": ("Yağmur", "Diseñadora gráfica", [
        "Entra con los cascos puestos, pide por señas.",
        "“Aquí nadie me da conversación. Se agradece.”",
        "“Trabajo por mi cuenta. Esta es mi mesa.”",
    ]),
    "cem_abi": ("Cem Abi", "Mensajero en moto", [
        "La moto en la puerta, el casco en la mano.",
        "“Os puse en el grupo de los repartidores.”",
        "“Abro mi propio local. Este oficio lo aprendí contigo.”",
    ]),
}

# Arayuz metinleri AYRI DOSYADA (loc_es_ui.py): kaynaklari farkli.
from loc_es_ui import UI    # noqa: E402
