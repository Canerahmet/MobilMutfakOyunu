using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// CIZILEN SIMGELER.
    ///
    /// Hicbiri yazi tipinden gelmiyor ve bu bir tercih degil, bir ders:
    /// yildiz karakteri Rubik'te yok ve oyuncuya BOS KUTU olarak
    /// gorunuyordu (tools/art/check_font.py yakaladi). Ayni sey ok,
    /// onay ve disli karakterleri icin de gecerli - hepsi yazi tipine
    /// gore var ya da yok.
    ///
    /// Hepsi dikdortgen, daire (yarim yaricap yuvarlama) ve DONUSTEN
    /// kuruluyor. UI Toolkit'in kendi cizim API'si (Painter2D)
    /// kullanilmiyor: bu projenin butun "editorde calisti, yapida
    /// cikmadi" hikayeleri, yapiya dolayli giren bir seye
    /// guvenmekten cikti. Dikdortgen her yerde dikdortgendir.
    ///
    /// Olcu: hepsi KARE bir kutu doner ve kutunun kenari 's'. Boylece
    /// satirda hizalamak icin tek sayi yetiyor.
    /// </summary>
    public static class Icons
    {
        private static VisualElement Box(float s)
        {
            VisualElement v = new VisualElement();
            v.style.width = s;
            v.style.height = s;
            v.style.flexShrink = 0;
            return v;
        }

        private static VisualElement Rect(float w, float h, Color c, float r = 0f)
        {
            VisualElement v = new VisualElement();
            v.style.width = w;
            v.style.height = h;
            v.style.backgroundColor = c;
            v.style.position = Position.Absolute;
            if (r > 0f) Theme.Round(v, r);
            return v;
        }

        private static void At(VisualElement v, float x, float y)
        {
            v.style.left = x;
            v.style.top = y;
        }

        /// <summary>Sikke: altin daire, ortasinda koyu bir halka.</summary>
        public static VisualElement Coin(float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement dis = Rect(s, s, Kit.Coin, s * 0.5f);
            At(dis, 0f, 0f);
            box.Add(dis);

            VisualElement ic = Rect(s * 0.52f, s * 0.52f,
                                    new Color(0.85f, 0.60f, 0.10f), s * 0.26f);
            At(ic, s * 0.24f, s * 0.24f);
            box.Add(ic);
            return box;
        }

        /// <summary>
        /// Itibar tasi: 45 derece dondurulmus kare.
        ///
        /// Bes kollu yildiz cizmek dikdortgenle mumkun degil; dondurulmus
        /// kare hem "degerli tas" hem de "puan" diye okunuyor ve kasa
        /// sikkesinden siluetiyle ayriliyor - iki kapsul yan yana
        /// duracak, birbirine benzememeli.
        /// </summary>
        public static VisualElement Gem(float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement d = Rect(s * 0.68f, s * 0.68f, Kit.Gem, 3f);
            At(d, s * 0.16f, s * 0.16f);
            d.style.rotate = new Rotate(45f);
            box.Add(d);

            VisualElement parlak = Rect(s * 0.22f, s * 0.22f,
                                        new Color(0.85f, 0.74f, 1f), 2f);
            At(parlak, s * 0.26f, s * 0.26f);
            parlak.style.rotate = new Rotate(45f);
            box.Add(parlak);
            return box;
        }

        /// <summary>Disli: halka ve dort dis.</summary>
        public static VisualElement Gear(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            for (int i = 0; i < 4; i++)
            {
                VisualElement dis = Rect(s * 0.22f, s * 0.9f, c, 2f);
                At(dis, s * 0.39f, s * 0.05f);
                dis.style.rotate = new Rotate(i * 45f);
                box.Add(dis);
            }
            VisualElement halka = Rect(s * 0.62f, s * 0.62f, c, s * 0.31f);
            At(halka, s * 0.19f, s * 0.19f);
            box.Add(halka);

            VisualElement delik = Rect(s * 0.26f, s * 0.26f, Kit.CardBg, s * 0.13f);
            At(delik, s * 0.37f, s * 0.37f);
            delik.style.backgroundColor = new Color(0.071f, 0.086f, 0.118f, 1f);
            box.Add(delik);
            return box;
        }

        /// <summary>Sepet: govde ve iki tekerlek. Hal ekrani.</summary>
        public static VisualElement Cart(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement govde = Rect(s * 0.72f, s * 0.40f, c, 3f);
            At(govde, s * 0.20f, s * 0.22f);
            box.Add(govde);

            VisualElement sap = Rect(s * 0.22f, s * 0.10f, c, 2f);
            At(sap, s * 0.02f, s * 0.14f);
            box.Add(sap);

            VisualElement t1 = Rect(s * 0.18f, s * 0.18f, c, s * 0.09f);
            At(t1, s * 0.26f, s * 0.70f);
            box.Add(t1);
            VisualElement t2 = Rect(s * 0.18f, s * 0.18f, c, s * 0.09f);
            At(t2, s * 0.62f, s * 0.70f);
            box.Add(t2);
            return box;
        }

        /// <summary>Asci kepi: uc kabarti ve bir bant. Kadro ekrani.</summary>
        public static VisualElement Hat(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement k1 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(k1, s * 0.02f, s * 0.16f);
            box.Add(k1);
            VisualElement k2 = Rect(s * 0.40f, s * 0.40f, c, s * 0.20f);
            At(k2, s * 0.30f, s * 0.06f);
            box.Add(k2);
            VisualElement k3 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(k3, s * 0.64f, s * 0.16f);
            box.Add(k3);

            VisualElement govde = Rect(s * 0.74f, s * 0.30f, c, 2f);
            At(govde, s * 0.13f, s * 0.38f);
            box.Add(govde);

            VisualElement bant = Rect(s * 0.80f, s * 0.24f, c, 3f);
            At(bant, s * 0.10f, s * 0.66f);
            box.Add(bant);
            return box;
        }

        /// <summary>Liste: uc satir ve onlerinde birer nokta. Menu ekrani.</summary>
        public static VisualElement List(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            for (int i = 0; i < 3; i++)
            {
                float y = s * (0.14f + i * 0.30f);
                VisualElement nokta = Rect(s * 0.16f, s * 0.16f, c, s * 0.08f);
                At(nokta, 0f, y);
                box.Add(nokta);

                VisualElement satir = Rect(s * 0.62f, s * 0.14f, c, 2f);
                At(satir, s * 0.26f, y + s * 0.01f);
                box.Add(satir);
            }
            return box;
        }

        /// <summary>Yukari ok: ucgen yerine dondurulmus kare ve govde.</summary>
        public static VisualElement ArrowUp(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement govde = Rect(s * 0.22f, s * 0.52f, c, 2f);
            At(govde, s * 0.39f, s * 0.40f);
            box.Add(govde);

            // Dondurulmus kare bir ok basi gibi okunuyor: ust yarisi
            // gorunuyor, alt yarisi govdenin arkasinda kaliyor.
            VisualElement bas = Rect(s * 0.46f, s * 0.46f, c, 2f);
            At(bas, s * 0.27f, s * 0.10f);
            bas.style.rotate = new Rotate(45f);
            box.Add(bas);
            return box;
        }

        /// <summary>Iki kisi: memnuniyet karti.</summary>
        public static VisualElement People(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement b1 = Rect(s * 0.30f, s * 0.30f, c, s * 0.15f);
            At(b1, s * 0.04f, s * 0.10f);
            box.Add(b1);
            VisualElement g1 = Rect(s * 0.40f, s * 0.34f, c, s * 0.10f);
            At(g1, 0f, s * 0.46f);
            box.Add(g1);

            VisualElement b2 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(b2, s * 0.50f, s * 0.04f);
            box.Add(b2);
            VisualElement g2 = Rect(s * 0.46f, s * 0.38f, c, s * 0.11f);
            At(g2, s * 0.44f, s * 0.44f);
            box.Add(g2);
            return box;
        }

        /// <summary>
        /// Oynat ucgeni.
        ///
        /// Ucgen UI Toolkit'te dogrudan yok; ikinci yol KENAR HILESI
        /// (sifir boyutlu bir ogenin uc kenari saydam). O hile
        /// gölgelendiriciye ve kenar birlestirmesine bagli - yani tam
        /// bu projenin kacindigi turden bir bahis. Yerine: kare
        /// 45 derece donduruluyor ve yarisi kirpiliyor. Kirpma
        /// (overflow: hidden) UI Toolkit'in en temel davranisi.
        /// </summary>
        public static VisualElement Play(Color c, float s = 18f)
        {
            VisualElement box = Box(s);
            box.style.overflow = Overflow.Hidden;

            VisualElement kare = Rect(s * 0.72f, s * 0.72f, c, 2f);
            At(kare, -s * 0.22f, s * 0.14f);
            kare.style.rotate = new Rotate(45f);
            box.Add(kare);

            // SAGDAN SOLA OKUYAN BIRI ICIN "ILERI" SOLDADIR.
            //
            // Bu simge yon bildiren tek simge: "gunu ac", "ertesi gun".
            // Arapca arayuzde her sey saga akarken ucgenin saga bakmasi,
            // oyuncuyu geldigi yone isaret etmek olurdu. Diger simgeler
            // (duraklat, kitap, tabak) yonsuz - onlar cevrilmiyor.
            if (Loc.IsRightToLeft)
                box.style.scale = new Scale(new Vector2(-1f, 1f));
            return box;
        }

        /// <summary>Duraklat: iki cubuk.</summary>
        public static VisualElement Pause(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement sol = Rect(s * 0.26f, s * 0.80f, c, 2f);
            At(sol, s * 0.14f, s * 0.10f);
            box.Add(sol);
            VisualElement sag = Rect(s * 0.26f, s * 0.80f, c, 2f);
            At(sag, s * 0.60f, s * 0.10f);
            box.Add(sag);
            return box;
        }

        /// <summary>Kombo: ust uste iki tabak. Imza mekanigi.</summary>
        public static VisualElement Combo(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement alt = Rect(s, s * 0.26f, c, s * 0.13f);
            At(alt, 0f, s * 0.62f);
            box.Add(alt);
            VisualElement orta = Rect(s * 0.78f, s * 0.24f, c, s * 0.12f);
            At(orta, s * 0.11f, s * 0.34f);
            box.Add(orta);
            VisualElement ust = Rect(s * 0.52f, s * 0.22f, c, s * 0.11f);
            At(ust, s * 0.24f, s * 0.08f);
            box.Add(ust);
            return box;
        }

        /// <summary>Veresiye defteri: kapak ve sayfa cizgisi.</summary>
        public static VisualElement Book(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement kapak = Rect(s * 0.82f, s * 0.90f, c, 3f);
            At(kapak, s * 0.09f, s * 0.05f);
            box.Add(kapak);
            VisualElement sayfa = Rect(s * 0.12f, s * 0.90f,
                                       new Color(0.071f, 0.086f, 0.118f, 1f), 1f);
            At(sayfa, s * 0.26f, s * 0.05f);
            box.Add(sayfa);
            return box;
        }

        /// <summary>Arti: iki cubuk.</summary>
        public static VisualElement Plus(Color c, float s = 14f)
        {
            VisualElement box = Box(s);
            VisualElement dik = Rect(s * 0.22f, s, c, 1f);
            At(dik, s * 0.39f, 0f);
            box.Add(dik);
            VisualElement yatay = Rect(s, s * 0.22f, c, 1f);
            At(yatay, 0f, s * 0.39f);
            box.Add(yatay);
            return box;
        }

        /// <summary>
        /// Onay kutusu: bos kare ya da icinde tik.
        ///
        /// Tik iki cubuktan: kisa olan sola yatik, uzun olan saga.
        /// </summary>
        public static VisualElement Check(bool ok, Color c, float s = 16f)
        {
            VisualElement box = Box(s);
            VisualElement kutu = Rect(s, s, Color.clear, 4f);
            kutu.style.borderTopWidth = 2;
            kutu.style.borderBottomWidth = 2;
            kutu.style.borderLeftWidth = 2;
            kutu.style.borderRightWidth = 2;
            Color kenar = ok ? c : Theme.Line;
            kutu.style.borderTopColor = kenar;
            kutu.style.borderBottomColor = kenar;
            kutu.style.borderLeftColor = kenar;
            kutu.style.borderRightColor = kenar;
            if (ok) kutu.style.backgroundColor = new Color(c.r, c.g, c.b, 0.22f);
            At(kutu, 0f, 0f);
            box.Add(kutu);

            if (!ok) return box;

            VisualElement kisa = Rect(s * 0.14f, s * 0.34f, c, 1f);
            At(kisa, s * 0.26f, s * 0.42f);
            kisa.style.rotate = new Rotate(-45f);
            box.Add(kisa);

            VisualElement uzun = Rect(s * 0.14f, s * 0.62f, c, 1f);
            At(uzun, s * 0.56f, s * 0.16f);
            uzun.style.rotate = new Rotate(35f);
            box.Add(uzun);
            return box;
        }
    }
}
