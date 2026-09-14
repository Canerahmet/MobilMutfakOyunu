using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// BIR KEZ gorunen ipuclari. Ogretici degil, hatirlatma.
    ///
    /// docs/16 dakika dakika bir ogretici tarif ediyor ve kodda tek satir
    /// karsiligi yoktu. Yeni bir oyuncunun ilk gun gordugu sey: bos bir
    /// salon, dort gri dugme ve adi olmayan kirmizi bir sayi. Dort alt
    /// ekranin dordu de "yapacak bir seyin yok" diyor (stok dolu, menu
    /// kilitli, salon kadrosu bos, ekipman ayni fiyatta). Tek anlamli
    /// eylem "Servisi Ac" ve o da GERI DONUSU OLMAYAN bir kapi.
    ///
    /// Metin duvari DEGIL: ilgili dugmenin yaninda, ilk kez gorundugunde,
    /// tek cumle. Oyuncunun dokunus butcesini yemeden (docs/16 gunluk
    /// 40-60 dokunus) sadece bilmesi gereken uc seyi soyluyor.
    ///
    /// Gorulduyse bir daha gorunmuyor ve bu CIHAZDA saklaniyor, kayitta
    /// degil: ikinci bir kampanya acan oyuncuya ayni cumleleri tekrar
    /// okutmak, onu oyunun kendisinden uzaklastirir.
    /// </summary>
    public static class Hints
    {
        /// <summary>Gosterilecek ipucu. Yoksa null.</summary>
        public sealed class Hint
        {
            public readonly string Id;
            public readonly string TextKey;

            /// <summary>
            /// Metnin {0} yerine koyacagi deger. Null ise yok.
            ///
            /// Mudahale ipucu bir zamanlar "gunde dort" diye SABIT
            /// yaziyordu, oysa sayi content/economy.json'dan geliyor ve
            /// o dosya denge araci tarafindan URETILIYOR. Denge degisince
            /// ipucu sessizce yalan soylerdi.
            /// </summary>
            public System.Func<Lokanta.Core.Sim.Simulation, object> Arg;

            public Hint(string id, string textKey,
                        System.Func<Lokanta.Core.Sim.Simulation, object> arg = null)
            { Id = id; TextKey = textKey; Arg = arg; }
        }

        // Ucu de bir KURAL soyluyor, bir dugmeyi tarif etmiyor.
        //
        //   1. Servisi acmak geri donusu olmayan tek karar.
        //   2. Menude yemek yoksa musteri kapidan doner - bu kural
        //      oyunda ancak KAYBEDEREK ogreniliyordu ve kaybettigi de
        //      yalnizca aksam raporunda, sifirdan buyukse gorunuyordu.
        //   3. Mudahale hakki gunluk ve UCUNU BIRDEN kapsiyor; ekranda
        //      yalnizca "Hak 4" yaziyordu, neyin hakki belli degildi.
        private static readonly Hint Service = new Hint("servis", "ui.hint.service");
        private static readonly Hint Menu = new Hint("menu", "ui.hint.menu");
        private static readonly Hint Interventions = new Hint(
            "mudahale", "ui.hint.interventions", s => s.InterventionsPerDay);
        private static readonly Hint Rent = new Hint("kira", "ui.hint.rent");
        private static readonly Hint Price = new Hint("fiyat", "ui.hint.price");
        private static readonly Hint Staff = new Hint("personel", "ui.hint.staff");
        private static readonly Hint Select = new Hint("secim", "ui.hint.select");

        // ITIBAR TAVANI. Oyunun en onemli ilerleme kurali ve hicbir yerde
        // yazmiyordu: itibar masa sayisina gore tavanli, ve tavana
        // dayanan oyuncu servisi ne kadar iyi yaparsa yapsin sayinin
        // kipirdamadigini goruyor. Olculdu: iyi oynayan bir oyuncu 7
        // masada 75'e dayanip 32 gun orada kaliyor.
        private static readonly Hint Cap = new Hint(
            "tavan", "ui.hint.cap", s => s.ReputationCapCenti / 100);

        private const string Prefix = "lokanta.ipucu.";

        private static readonly HashSet<string> Shown = new HashSet<string>();

        /// <summary>Bu ipucu daha once gorulduyse true.</summary>
        public static bool Seen(Hint h)
        {
            if (h == null) return true;
            if (Shown.Contains(h.Id)) return true;
            return PlayerPrefs.GetInt(Prefix + h.Id, 0) != 0;
        }

        public static void MarkSeen(Hint h)
        {
            if (h == null) return;
            Shown.Add(h.Id);
            PlayerPrefs.SetInt(Prefix + h.Id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Butun ipuclarini unutur. Ayarlardan cagriliyor.</summary>
        public static void Reset()
        {
            Shown.Clear();
            foreach (Hint h in All) PlayerPrefs.DeleteKey(Prefix + h.Id);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Butun ipuclarini GORULMUS sayar.
        ///
        /// Magaza ekran goruntusu icin: ipucu seridi salonun ustune
        /// bindigi icin magaza listesinde oyunun kendisi degil
        /// ogreticisi goruunuyor. Olcum turunda KULLANILMIYOR - orada
        /// tam tersi gerekiyor (Reset), cunku katilim katmani da
        /// olculmeli.
        /// </summary>
        public static void MarkAllSeen()
        {
            foreach (Hint h in All) MarkSeen(h);
        }

        /// <summary>
        /// Butun ipuclari. Reset() bu diziyi geziyor, yani buraya
        /// eklenmeyen bir ipucu "ipuclarini yeniden goster"e RAGMEN
        /// geri gelmiyor - Cap tam bu yuzden eksikti.
        /// </summary>
        private static readonly Hint[] All =
            { Service, Menu, Interventions, Rent, Price, Staff, Select, Cap };

        // =====================================================================
        /// <summary>
        /// Simdi gosterilmesi gereken ipucu. Yoksa null.
        ///
        /// Sirasi ONEM SIRASI degil ZAMAN SIRASI: oyuncu once servisi
        /// acmayi, sonra menuyu, sonra mudahaleyi, en son kirayi
        /// ogreniyor. Ayni anda ikisini gostermek ikisini de okutmaz.
        /// </summary>
        public static Hint Current(Lokanta.Core.Sim.Simulation sim)
        {
            if (sim == null) return null;

            if (sim.Phase == Lokanta.Core.Sim.DayPhase.Morning)
            {
                if (!Seen(Service)) return Service;
                if (sim.Day >= 2 && !Seen(Menu)) return Menu;
                // Kira, gunu geldiginde her seyin onune geciyor: o gun
                // kasada para yoksa oyuncu borclaniyor ve bunu once
                // ogrenmesi gerekiyor.
                if (sim.DaysToRent <= 1 && !Seen(Rent)) return Rent;
                if (sim.Day >= 3 && !Seen(Price)) return Price;
                if (sim.Day >= 4 && !Seen(Staff)) return Staff;
                // Tavana DAYANDIGINDA: kural ancak o an anlamli.
                if (sim.ReputationCenti >= sim.ReputationCapCenti && !Seen(Cap))
                    return Cap;
                return null;
            }

            if (sim.Phase == Lokanta.Core.Sim.DayPhase.Service)
            {
                if (!Seen(Interventions)) return Interventions;
                // Masa secimi IKINCI serviste: ilk servis zaten
                // mudahalenin ne oldugunu ogrenmekle geciyor.
                if (sim.Day >= 2 && !Seen(Select)) return Select;
            }

            return null;
        }

        // =====================================================================
        /// <summary>
        /// Ipucu seridi. Ekranin ustune giren, tek cumlelik, kapatilabilir
        /// bir kutu.
        ///
        /// Kalici bir ogretici katmani DEGIL: kapatildiginda bir daha
        /// gelmiyor ve oyuncunun dokunus butcesinden (docs/16 gunluk
        /// 40-60 dokunus) yalnizca bir dokunus aliyor.
        /// </summary>
        public static VisualElement Strip(Hint h, Lokanta.Core.Sim.Simulation sim,
                                         System.Action onDismiss)
        {
            if (h == null) return null;

            VisualElement box = new VisualElement();
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.Center;
            box.style.backgroundColor = new Color(0.20f, 0.17f, 0.11f);
            box.style.borderLeftWidth = 3;
            box.style.borderLeftColor = Theme.Warn;
            box.style.paddingLeft = Theme.Pad;
            box.style.paddingRight = Theme.Gap;
            box.style.paddingTop = Theme.Gap;
            box.style.paddingBottom = Theme.Gap;
            box.style.marginBottom = Theme.Gap;

            string body = h.Arg != null && sim != null
                ? Loc.T(h.TextKey, h.Arg(sim))
                : Loc.T(h.TextKey);
            Label text = Theme.Text(body, Theme.FontSmall, Theme.Ink);
            // Tek satira sigmiyor: sarilmali, yoksa cumlenin yarisi
            // ekranin disinda kaliyor. 873x393'te olculdu.
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.flexGrow = 1;
            text.style.flexShrink = 1;
            box.Add(text);

            Button ok = Theme.Btn(Loc.T("ui.hint.ok"), () =>
            {
                MarkSeen(h);
                if (onDismiss != null) onDismiss();
            });
            // Dokunma hedefi TEMANIN kendi olcusu (52 dp). Once 44
            // yaziliydi - Google'in 48 dp tavsiyesinin de altinda ve
            // projenin her yerde kullandigi olcunun altinda. Bir sayiyi
            // elle yazmak, temayi degistirince sessizce geride kaliyor.
            ok.style.minHeight = Theme.Touch;
            ok.style.marginLeft = Theme.Gap;
            ok.style.flexShrink = 0;
            box.Add(ok);

            return box;
        }
    }
}
