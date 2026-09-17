# -*- coding: utf-8 -*-
"""
Spanish string table. gen_loc.py reads it and produces content/loc/es.json.

TRANSLATION DECISIONS - every one of them THE SAME as the decisions in
loc_en.py, because if they drifted apart one language would read
"Lahmacun" where the other read "Pizza turca":

  Ingredients are translated IN FULL.

  Dishes VARY. Turkish names the world already knows are KEPT (Lahmacun,
  Doner, Iskender, Baklava); the descriptive ones are translated. Where
  a kept name needs it, a short explanation sits beside it.

  The regulars' scenes are THE GAME'S VOICE: short, everyday sentences
  that each tell one thing. Not word for word, but in the same tone.

  Proper names (Hasan Usta, Nazife Teyze) are KEPT; the title is
  explained on the occupation line.

NEUTRAL SPANISH. What the user asked for was that it "be commonly
understood". So instead of options that give a region away, the word
that is understood everywhere is picked:
  - the neutral term rather than "computadora"/"ordenador"; "movil"/
    "celular" is not needed here in any case.
  - The second person singular is "tu" (no voseo), and the plural is not
    "ustedes" - the game addresses the player in the singular anyway.
  - In the "patatas"/"papas" dilemma the menu says "Papas fritas", not
    "Patatas fritas" - of the two, "Papas fritas" is the one understood
    by the wider audience (recognised in Latin America and in Spain).
"""

# ---------------------------------------------------------------------------
# Ingredient names
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
# Dish names
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
    # turk -- the known names are kept, the descriptive ones translated
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
# Customer archetypes
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
# Staff traits, roles, stations
# ---------------------------------------------------------------------------
TRAITS = {
    "hizli_ama_daginik": "Prisa y Desorden",
    "yavas_ama_titiz": "Despacio y con Cuidado",
    "kalabalikta_panikleyen": "Se Bloquea en el Apuro",
    "sakin": "Imperturbable",
    "musteriyle_iyi_anlasan": "Don de Gentes",
    "suratsiz": "Trato Seco",
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
    "musteriyle_iyi_anlasan": "Los clientes se van más contentos cuando cobra esta persona.",
    "suratsiz": "Los clientes se van menos contentos cuando cobra esta persona.",
    "cabuk_yorulan": "Baja el ritmo en el último cuarto del día.",
    "dayanikli": "Trabaja al mismo ritmo hasta el cierre.",
    "ekip_moralini_yukselten": "Sube el ánimo del equipo.",
    "huysuz": "Baja el ánimo del equipo.",
    "cirak": "Barato, lento, aprende rápido.",
    "tecrubeli": "Caro, rápido, ya no mejora más.",
}

# The rules of the voice are the same as loc_en.py and docs/53: name the
# behaviour, not the person; spare the explanation; plain and short.
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
    "cirak": "Acaba de llegar. Se lo enseñas una vez y se le queda.",
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
# Regulars: name, occupation, three scenes
# ---------------------------------------------------------------------------
REGULARS = {
    # --- turk ---
    "hasan_usta": ("Don Rafael", "Tornero de la calle de enfrente", [
        "Nada más entrar mira a la cocina. “¿Hay guiso de frijoles?”",
        "Ya no pide nada. Se sienta, y tú lo sabes.",
        "“Mi hijo volvió del servicio militar. Esta noche lo traigo.”",
    ]),
    "nazife_teyze": ("Doña Pilar", "Vecina del piso de arriba", [
        "Prueba la sopa y no dice nada. Mañana vuelve.",
        "“La mía sabía así. Hace años.”",
        "Se detiene en la puerta: “Este lugar ahora es la cara de la calle.”",
    ]),
    "selim_bey": ("Don Sergio", "Funcionario de la oficina de impuestos", [
        "La misma mesa, la misma hora. No se pasa ni un minuto.",
        "“Cuarenta minutos para comer. Acá salgo a los treinta y cinco.”",
        "Habla de jubilarse. “Entonces voy a venir más seguido.”",
    ]),
    "rasim_amca": ("Don Ramón", "Maestro de obra", [
        "Las manos llenas de cal. Se sacude la chaqueta antes de sentarse.",
        "“A los muchachos también les dije: de ahora en adelante, el almuerzo acá.”",
        "La obra está por terminar. “Igual me voy a seguir pasando, no te preocupes.”",
    ]),
    "guler_hanim": ("Doña Gloria", "Peluquera de la esquina", [
        "Entra y sale de pie, se lleva el arroz consigo.",
        "“A mis clientas les digo que crucen la calle y vayan contigo.”",
        "Está ampliando su local. “Crecimos juntos, tú y yo.”",
    ]),
    "okan": ("Óscar", "Estudiante universitario", [
        "Pregunta qué es lo más barato del menú.",
        "“Me salió la beca.” Hoy también pide postre.",
        "Empieza sus prácticas. “Con el primer sueldo, invito yo acá.”",
    ]),
    "nurten_abla": ("Nuria", "Encargada de un taller textil", [
        "Descanso corto. Siempre se lleva el cacık.",
        "“Vienen tres más del taller — guárdanos una mesa.”",
        "El taller cierra. “El lugar nuevo queda lejos, pero voy a venir.”",
    ]),
    "ismail_sofor": ("Don Ismael", "Camionero de larga distancia", [
        "Estaciona el camión en la esquina, come rápido y se va.",
        "“Volviendo de Ankara, ahora siempre paro acá.”",
        "Lo dijo por la radio: “Otros dos choferes van a preguntar por ti.”",
    ]),
    "perihan_hanim": ("Doña Pepa", "Maestra jubilada", [
        "Levanta el tenedor a contraluz. No dice nada, pero mira.",
        "“Hoy el mantel está limpio. Me di cuenta.”",
        "“No soy fácil de contentar. Este lugar me gusta.”",
    ]),
    "mehmet_dede": ("Abuelo Manolo", "Cliente del restaurante de antes", [
        "Duda en la puerta. “Esto antes era de otro.”",
        "“Los garbanzos sabían así en aquel entonces. Idénticos.”",
        "Ahora viene todos los días. Todos saben cuál es su silla.",
    ]),
    # --- fastfood ---
    "deniz": ("Dani", "Estudiante de secundaria", [
        "Recién salido de clase, la mochila en un hombro, apurado.",
        "“Ahora nos juntamos todos acá.”",
        "Pasó el examen. “El festejo es acá. Somos seis.”",
    ]),
    "burak": ("Bruno", "Programador", [
        "Abre la laptop y pide sin hacerte esperar.",
        "“Mudamos el almuerzo del equipo para acá.”",
        "“Ahora trabajo a distancia, pero esta es prácticamente mi oficina.”",
    ]),
    "elif": ("Elena", "Asesora de tienda", [
        "Llega con las bolsas de las compras, tiene quince minutos.",
        "“Lo vi por la vitrina: pusieron algo nuevo.”",
        "“Les conté a las chicas de la tienda. Ahora te pedimos a ti.”",
    ]),
    "cem_abi": ("Chema", "Mensajero en moto", [
        "La moto en la puerta, el casco en la mano.",
        "“Te puse en el grupo de los mensajeros.”",
        "“Voy a abrir mi propio local. El oficio lo aprendí de ti.”",
    ]),
    "melis": ("Marisa", "Contadora independiente", [
        "Lee la lista de precios de arriba abajo.",
        "“Yo llevo las cuentas, tú haces la comida.”",
        "“Me da curiosidad tu margen. No es broma.”",
    ]),
    "ozan": ("Óliver", "Futbolista aficionado", [
        "Después del partido, con todo el equipo, a los gritos.",
        "“Venimos acá cuando ganamos. Nos das suerte.”",
        "“Nos llevamos la copa. ¿Ponemos tu nombre en la camiseta?”",
    ]),
    "sevda": ("Sonia", "Nutricionista", [
        "Pregunta por la ensalada: “¿Me pones el aderezo aparte?”",
        "“A la gente que atiendo le recomiendo este lugar.”",
        "“Tu menú ya está en la pared de la clínica.”",
    ]),
    "tolga": ("Tomás", "Guardia del turno de noche", [
        "Llega a medianoche. Le sorprende encontrar la puerta abierta.",
        "“Son el único lugar abierto a esta hora.”",
        "“Los otros guardias también empezaron a venir. ¿Te diste cuenta?”",
    ]),
    "kaan_hoca": ("Profe Carlos", "Entrenador del gimnasio", [
        "Recién salido del entrenamiento, preguntando por la proteína.",
        "“A los que entreno les digo que coman acá.”",
        "“Puse tu dirección en el tablero del gimnasio. Ojalá no te moleste.”",
    ]),
    "yagmur": ("Yolanda", "Profesora de clases nocturnas", [
        "Tarde y cansada. Pregunta por el postre.",
        "“Este es el único rato bueno de mi día.”",
        "“Se terminó el curso. Pero ya es costumbre: voy a volver.”",
    ]),
}

# Interface text lives in a SEPARATE FILE (loc_es_ui.py): the sources differ.
from loc_es_ui import UI    # noqa: E402

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
    "Nieves", "Hilario", "Ana", "Manuel", "Flora", "Mateo", "Emilia", "Andrés",
    "Herminia", "Álvaro", "Zaira", "Hugo", "Soledad", "Ismael", "Elena", "Óscar",
    "Eva", "Julián", "Susana", "Ramón", "María", "Carmelo", "Gloria", "Hernán",
    "Dolores", "Nicolás", "Sandra", "Nacho", "Paloma", "Sergio", "Rosa", "Damián",
    "Begoña", "Cosme", "Amparo", "Tomás", "Milagros", "Gonzalo", "Juana", "Clemente",
    "Lourdes", "Rodrigo", "Delia", "Marcial", "Trini", "Bruno", "Julia", "Gabriel",
    "Karina", "Diego", "Sonia", "Leandro", "Charo", "Vicente", "Gemma", "Fermín",
    "Azucena", "Simón", "Nuria", "Miguel", "Rocío", "Wenceslao", "Lidia", "Cristóbal",
    "Marisol", "Eliseo", "Berta", "Godofredo", "Teresa", "Rubén", "Adela", "Cirilo",
    "Noelia", "Ernesto", "Sofía", "Guillermo", "Fátima", "Desiderio", "Vanesa", "Bernardo",
    "Rut", "Lisardo", "Belén", "Ángel", "Clara", "Otilio", "Marta", "Rafael",
    "Estrella", "Domingo", "Greta", "Wilfredo", "Irene", "Remigio", "Olga", "Félix",
]
