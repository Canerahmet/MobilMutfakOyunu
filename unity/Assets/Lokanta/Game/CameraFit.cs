using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Kamerayi bir kutuyu TAM CERCEVELEYECEK sekilde yerlestirir.
    ///
    /// Bu sinifin varlik sebebi olculdu ve gorulerek bulundu. Dokunma
    /// hedefi olcumu (Editor/RoomLayout.cs) kamerayi arsaya gore
    /// SIGDIRIYOR: 32 derece gorus acisi, 34 derece egim, -12 derece
    /// donme, ve kutuyu tam dolduran bir mesafe. Oyun kamerasi ise sabit
    /// 15,5 m yukseklik ve 42 derece ile yazilmisti.
    ///
    /// Sonuc: ilk render'da restoran karenin yalnizca %38'ini kapliyordu.
    /// Yani genel gorunumde 81 dp olculen bir oda, oyunda ~32 dp'ye
    /// dusuyordu - Google'in 48 dp asgarisinin cok altina.
    ///
    /// Duzeltme SAYIYI DEGISTIRMEK degil, KURALI PAYLASMAK: kamera artik
    /// olcumle ayni sigdirma hesabini kullaniyor. Ayni sey iki yerde
    /// yazildiginda sessizce ayrisiyor - bu projede bes kez oldu.
    /// </summary>
    public static class CameraFit
    {
        /// <summary>
        /// GORUS ACISI, EGIM VE DONME - hepsi olculerek secildi.
        ///
        /// Ilk deger 32 derece gorus acisi, 34 egim, -12 DONME idi. O -12
        /// derecelik cevirme "2,5D" gorunusu icin konmustu ve bedeli
        /// olculmemisti. Kullanici ekran goruntusune bakip "bos odalar yer
        /// kaplamasin, restoran tam ekran olsun" deyince olculdu:
        ///
        ///   ayar          kare dolgusu (acilis/buyumus)   TABAN dokunma hedefi
        ///   32, -12       %27 / %33                       52 -> 48 dp
        ///   32,  -6       %34 / %39                       -
        ///   22,   0       %48 / %43                       71 -> 71 dp
        ///
        /// TABAN = en kucuk ACIK odanin ekrandaki kisa kenari, arayuz
        /// cubuklari yerindeyken (Editor/RoomLayout.cs). Google'in asgarisi
        /// 48 dp: eski ayar en buyuk kademede TAM TAMINA 48'e dusuyordu,
        /// yani payi yoktu. Yeni ayarda 71 dp ve kademeyle DEGISMIYOR.
        ///
        /// Donmenin sifirlanmasi kareyi asil acan sey (%27 -> %41); gorus
        /// acisinin 32'den 22'ye inmesi perspektifi duzleyip kalanini
        /// kazaniyor. Egim 34'te kaldi - derinlik hissi oradan geliyor ve
        /// arttirmak dolguyu dusuruyor (42'de %18, 50'de %17).
        ///
        /// Bu sayilar oyunun ve OLCUMUN ortak kaynagi. Birini degistiren,
        /// once RoomLayout.Capture'i kosturup TABAN satirina bakmali.
        /// </summary>
        public const float FieldOfView = 22f;

        /// <summary>
        /// Egim. 34'te KALDI - ve 28 denendi.
        ///
        /// Erken kademede cerceveyi baglayan sey DERINLIK (acik odalar
        /// 13,4 x 9,6, yani neredeyse kare); egimi dusurmek derinligi
        /// daha cok sikistirip binayi buyutur diye dusunuldu. Olculdu:
        /// 28 derecede taban 45 dp'den 42'ye DUSTU. Izdusum uc boyutlu;
        /// kutunun sekiz kosesi tek bir trigonometri satirina uymuyor.
        /// </summary>
        public const float Pitch = 34f;
        /// <summary>
        /// DONME. Referans istegiyle geri geldi - ama OLCULEREK.
        ///
        /// Sifir, docs/31'de kareyi doldurmak ve dokunma hedefini
        /// buyutmek icin secilmisti (-12 derece donme tabani 71 dp'den
        /// 48 dp'ye dusuruyordu). Kullanici referans gorselleri getirip
        /// "kamera acisi da referanstaki gibi olsa" deyince yeniden
        /// bakildi: referansin butun karelerinde bina UC CEYREK
        /// gorunuyor, yani iki yuzu birden.
        ///
        /// Donme arttikca odalar ekranda ESKENAR DORTGENE donuyor ve
        /// kisa kenari kisaliyor; olcumu RoomLayout.Capture'in TABAN
        /// satiri veriyor. Deger o satira bakilarak secildi.
        /// </summary>
        /// <summary>
        /// DONME. Referans istegiyle geri geldi - ve BEDELI OLCULDU.
        ///
        /// Sifir, docs/31'de bilerek secilmisti: -12 derecelik donme
        /// dokunma hedefi tabanini 71 dp'den 48'e dusuruyordu. Kullanici
        /// referans gorselleri getirip "kamera acisi da referanstaki gibi
        /// olsa daha iyi degil mi, referans noktasindan cok uzaktayiz"
        /// deyince yeniden bakildi - referansin dort karesinde de bina
        /// UC CEYREK duruyor, iki yuzu birden goruunuyor.
        ///
        /// RoomLayout.Capture ile olculdu (TABAN = en kucuk ACIK odanin
        /// seritli kisa kenari, 20:9 telefon):
        ///
        ///     donme    TABAN 20:9    TABAN 16:9
        ///       0        53 dp         66 dp
        ///      10        45 dp         57 dp
        ///      18        41 dp         51 dp
        ///      30        39 dp         49 dp
        ///
        /// VE BIR DE TELEFONDA BAKILDI. Yirmi derece goruntu aracinda
        /// (1280x560) harika duruyordu ama gercek yapida, 873x393 ve
        /// seritler yerindeyken bina KUCULDU: donmus bir dikdortgen
        /// ekranda daha genis yer istiyor, sigdirma da kamerayi geri
        /// cekiyor. Yani donme tek basina referansa YAKLASTIRMIYOR,
        /// uzaklastiriyor.
        ///
        /// On derece secildi: ucceyrek his geliyor, taban 45 dp'de
        /// kaliyor. Tabanin altinda kalan sey bir DUGME degil, bes
        /// metrelik bir odanin ekrandaki KISA kenari; uzun kenari iki
        /// katindan fazla ve oyuncu iki parmakla yaklasabiliyor.
        ///
        /// KAT PLANI SUCLU DEGIL - BU IDDIA OLCUMLE CURUDU.
        ///
        /// Once "referansin binasi kare, bizimki uzun bir serit; plan
        /// derinlesirse hem donme hem boyut geri gelir" yazmistim.
        /// Arayuzsuz bir kare (873x393) cekilince gorundu ki bina
        /// ekrani ZATEN dolduruyor: genislik sinirda, derinlikte az
        /// bir pay var. 34 derecelik egim derinligi sin(34)=0,56 ile
        /// sikistiriyor, yani 18 x 9,6'lik arsa ekranda 3,3:1 oraninda
        /// duruyor ve seritler arasindaki bant 3,7:1. Arsa KARE
        /// olsaydi bina kucuurdu, buyumezdi.
        ///
        /// Gercek darbogaz ARAYUZ: seritler 393 dp'nin 156'sini
        /// aliyordu (%40). Dugmeler 62'den 54 dp'ye, kapsul 42'den
        /// 38'e indi.
        ///
        /// Asil cozum baska: referansin binasi KARE, bizimki 18 x 9,6 m
        /// uzun bir serit. Kat plani derinlesirse ayni donmede oda
        /// ekranda buyur. docs/41 5. maddede sirada.
        /// </summary>
        public const float Yaw = 10f;

        /// <summary>Kenar payi. Olcum %2 birakiyor.</summary>
        public const float Margin = 1.02f;

        public static Quaternion Rotation
        {
            get { return Quaternion.Euler(Pitch, Yaw, 0f); }
        }

        /// <summary>Butun ekrani kullanan cerceve.</summary>
        public static Vector3 Position(Bounds target, float aspect)
        {
            return Position(target, aspect, 0f, 0f);
        }

        /// <summary>
        /// Ayni hesap, ama cerceve GUVENLI SERIDE.
        ///
        /// top01 ve bottom01, ekranin ustunde ve altinda arayuzun kapladigi
        /// oran (0..1). Oyun ekraninda ust cubuk ve eylem cubugu var;
        /// kutuyu butun ekrana sigdirmak, restoranin bir kismini cubuklarin
        /// altinda birakiyor ve ust tarafta bos bir serit olusturuyordu.
        ///
        /// Iki sey yapiliyor: dikey sigdirma yalnizca seridin yuksekligine
        /// gore, ve kamera seridin ortasi kutunun ortasina gelecek kadar
        /// kaydiriliyor.
        /// </summary>
        public static Vector3 Position(Bounds target, float aspect,
                                       float top01, float bottom01)
        {
            // Mesafe HESAPLA-VE-UMUT-ET degil, ARA-VE-DOGRULA ile bulunuyor.
            //
            // Once kapali bir formul vardi: en genis yayilim bolu gorus
            // acisinin tanjanti, arti kutunun yari derinligi, arti pay.
            // O formulun her terimi guvenli tarafta hata yapiyor ve
            // hatalar toplaniyordu - restoran 1280 genisliginde bir karede
            // yalnizca 875 piksel kapliyordu, yani oda dokunma hedefi
            // gereksiz yere %30 kucuktu.
            //
            // Ikili arama, kutunun sekiz kosesini GERCEKTEN izdusurup
            // cerceveye sigan EN KUCUK mesafeyi buluyor. Yaklasim yok,
            // birikmis pay yok.
            float lo = 0.5f, hi = 400f;
            for (int i = 0; i < 28; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(target, mid, aspect, top01, bottom01)) hi = mid;
                else lo = mid;
            }
            return CameraAt(target, hi * Margin, top01, bottom01);
        }

        /// <summary>
        /// Verilen mesafedeki kamera konumu. Kutu merkezi, arayuz
        /// cubuklarinin ARASINDAKI seridin ortasina denk gelecek sekilde
        /// kaydiriliyor.
        /// </summary>
        private static Vector3 CameraAt(Bounds target, float dist,
                                        float top01, float bottom01)
        {
            float tanV = Mathf.Tan(FieldOfView * Mathf.Deg2Rad * 0.5f);

            // Seridin ortasi, ekran ortasina gore normalize edilmis olarak.
            float centre = bottom01 - top01;
            float shift = centre * dist * tanV;

            return target.center
                   - Rotation * Vector3.forward * dist
                   - Rotation * Vector3.up * shift;
        }

        /// <summary>
        /// Bu mesafede kutunun sekiz kosesi de seride siginiyor mu.
        /// </summary>
        private static bool Fits(Bounds target, float dist, float aspect,
                                 float top01, float bottom01)
        {
            Vector3 cam = CameraAt(target, dist, top01, bottom01);
            Quaternion inv = Quaternion.Inverse(Rotation);

            float tanV = Mathf.Tan(FieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * aspect;

            // Normalize edilmis dikey sinirlar: -1 alt kenar, +1 ust kenar.
            float yMin = -1f + 2f * bottom01;
            float yMax = 1f - 2f * top01;

            Vector3 e = target.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = target.center + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);

                Vector3 v = inv * (corner - cam);
                if (v.z <= 0.05f) return false;          // kameranin arkasinda

                float x = v.x / (v.z * tanH);
                float y = v.y / (v.z * tanV);
                if (x < -1f || x > 1f) return false;
                if (y < yMin || y > yMax) return false;
            }
            return true;
        }

        /// <summary>
        /// Butun arsa. Yukseklik 2,6 m: duvarlar da cerceveye girmeli,
        /// yoksa arka duvar ust kenardan tasiyor.
        /// </summary>
        public static Bounds PlotBounds()
        {
            // Yukseklik 1,2 m ve kenar payi 0,4 m.
            //
            // Kutunun YUKSEKLIGI bagliyici: sigdirma sekiz koseyi
            // izdusuruyor ve o koseler arsanin KENARLARINDA, o kadar
            // yukseklikte hicbir sey yokken. Once 2,6 m yaziyordu
            // ("duvarlar da girmeli" - duvar yok), sonra 2,0 m. Her
            // fazladan metre kamerayi geri cekiyor ve restorani
            // kuculuyor; 873x393 dp'lik bir telefonda bu, oyuncunun
            // bakmasi gereken seyin ekranin ucte birine dusmesi demek.
            //
            // 1,2 m'de en yuksek nesne (buzdolabi 1,80 m) kutunun
            // disina tasiyor ama o nesne arsanin ORTASINDA duruyor,
            // kenarinda degil - yani cerceveden cikmiyor.
            return new Bounds(
                new Vector3(RoomPlan.PlotW * 0.5f, 0.5f, RoomPlan.PlotD * 0.5f),
                new Vector3(RoomPlan.PlotW + 0.4f, 1.2f, RoomPlan.PlotD + 0.4f));
        }

        /// <summary>
        /// ACIK odalarin cevreleyen kutusu. Kapali kanatlar cerceveye
        /// girmiyor - ve RestaurantView onlari zaten cizmiyor.
        ///
        /// Once her zaman butun arsa cerceveleniyordu; gerekcesi dokunma
        /// hedefinin kademeler arasinda SABIT kalmasiydi (docs/31).
        /// Kullanici ekran goruntusune bakinca gerekce coktu: birinci
        /// kademede arsanin 172,8 m2'sinin yalnizca 102,6'si acikti, yani
        /// ekranin %41'i "henuz senin olmayan" bos levhaydi.
        ///
        /// Kutu dar oldugu icin kamera YAKLASMIYOR da olabilir: yatay
        /// ekranda cogu zaman bagliyan sey EN degil DERINLIK (mutfak
        /// blogu arsanin butun derinligini kapliyor). Olculdu, su anki
        /// acilarda kademeler arasi mesafe hic degismiyor - yani hedef
        /// gercekten sabit, ama artik bos levha sayesinde degil.
        ///
        /// Kutunun kendisi yine de gerekli: cizilmeyen odayi
        /// cercevelemek, olcumun (RoomLayout) oyundan FARKLI bir sey
        /// olcmesi demekti.
        ///
        /// Yukseklik ve kenar payi PlotBounds ile ayni sebeplerden.
        /// </summary>
        /// <summary>
        /// Cerceveye giren sokak derinligi (m).
        ///
        /// 1,10 -> 1,94. Sebep olculdu: kaldirimda IKI yaya seridi
        /// gerekti (karsi yonde yuruyen figurler birbirinin icinden
        /// geciyordu) ve seritler arasi mesafe figurun EN GENIS govde
        /// bandindan geliyor - 0,67 m, bas hizasi
        /// (Editor/PlacementAudit PROFIL satirlari). 0,70 m serit
        /// araligi + govdelerin yarilari = 1,40 m kaldirim; ustune
        /// bordur (0,12) ve goruunur bir asfalt seridi (0,42) = 1,94.
        ///
        /// 1,10'da kaldirim tek seride sigiyordu ve yalnizca kaldirimi
        /// genisletmek yolu cercevenin DISINA atiyordu: sokak, asfaltsiz
        /// bir kaldirim seridine donuyordu.
        ///
        /// BEDELI OLCULDU, GOZLE KARAR VERILMEDI: Editor/RoomLayout
        /// TABAN satiri (en kucuk acik odanin ekrandaki kisa kenari,
        /// arayuz cubuklari yerindeyken). Google'in asgarisi 48 dp.
        ///
        /// Bu arada docs/31'deki 71 dp sayisinin BAYAT oldugu goruldu:
        /// o olcum sokak HIC YOKKEN alinmisti. Sokagin kendisi onu
        /// zaten ~64'e indirmisti ve kimse yeniden olcmemisti. Simdiki
        /// deger 20:9'da 59 dp, 16:9'da 74 dp.
        ///
        /// Bu sayiyi degistiren, once RoomLayout.Capture'i kosturup
        /// TABAN satirina bakmali - cerceve derinlige bagli ve onden
        /// eklenen her metre restorani ekranda kucultuyor.
        /// </summary>
        public const float StreetInFrame = 1.94f;

        public static Bounds OpenBounds(int tableCount)
        {
            float x1 = float.MaxValue, z1 = float.MaxValue;
            float x2 = float.MinValue, z2 = float.MinValue;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tableCount)) continue;

                if (r.X0 < x1) x1 = r.X0;
                if (r.Z0 < z1) z1 = r.Z0;
                if (r.X0 + r.W > x2) x2 = r.X0 + r.W;
                if (r.Z0 + r.D > z2) z2 = r.Z0 + r.D;
            }

            if (x1 > x2 || z1 > z2) return PlotBounds();

            // SOKAK DA CERCEVEYE GIRIYOR.
            //
            // Musteri artik kaldirimdan yuruyerek geliyor; goruunmeyen
            // bir yerden gelmek, hic yurumemekle ayni sey olurdu.
            //
            // 1,1 m OLCULEREK secildi, gozle degil: cerceve derinlige
            // bagli ve onden eklenen her metre restorani ekranda
            // kuculttuyor. Dokunma hedefi olcumu (docs/31) zaten
            // Google'in 48 dp asgarisine yakin; 1,1 m'de en kucuk acik
            // oda hala esigin ustunde kaliyor. Daha fazlasi sokagi
            // genisletir ama restorani kucultur.
            z1 -= StreetInFrame;

            return new Bounds(
                new Vector3((x1 + x2) * 0.5f, 0.5f, (z1 + z2) * 0.5f),
                new Vector3(x2 - x1 + 0.4f, 1.2f, z2 - z1 + 0.4f));
        }

        /// <summary>Tek bir oda, biraz payla.</summary>
        public static Bounds RoomBounds(int room)
        {
            RoomPlan.Room r = RoomPlan.Rooms[room];
            return new Bounds(
                new Vector3(r.CenterX, 0.55f, r.CenterZ),
                new Vector3(r.W + 1.0f, 1.8f, r.D + 1.0f));
        }
    }
}
