using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Bir figuru yol noktalari boyunca YURUTUR.
    ///
    /// Neden ayri bir bilesen: Figure bir figurun NE YAPTIGINI biliyor
    /// (durus), Walker NEREDE OLDUGUNU. Ikisi ayri, cunku duran bir
    /// asci da oturan bir musteri de Figure kullaniyor ama ikisinden
    /// yalnizca biri yuruyor.
    ///
    /// SIMULASYON METRE BILMIYOR. Cekirdek "garson 3 numarali masayla
    /// ilgileniyor" diyor; o masanin nerede oldugu ve oraya nasil
    /// gidilecegi tamamen gorunum katmaninin isi (docs/23: cekirdekte
    /// Unity yok, kayan nokta yok). Bu sinif simulasyona HICBIR SEY
    /// yazmiyor - yalnizca okudugu hedefe dogru yuruyor.
    ///
    /// HIZ OYUNUN HIZIYLA OLCEKLENIYOR. Oyuncu x16'ya bastiginda
    /// simulasyon on alti kat hizli akiyor; yuruyus gercek zamanda
    /// kalsaydi figurler simulasyonun onlarca saniye gerisinde kalir ve
    /// ekranda gordugu sey artik olan bitenle ilgisiz olurdu. Carpanin
    /// bir tavani var: cok hizlida figurler kayiyormus gibi gorunuyor,
    /// o yuzden belli bir noktadan sonra ISINLANIYORLAR (Warp) -
    /// gorulmeyecek kadar hizli bir yuruyus, yuruyus degil titremedir.
    /// </summary>
    public sealed class Walker : MonoBehaviour
    {
        /// <summary>Bu govdenin durusunu suren bilesen.</summary>
        public Figure Body;

        /// <summary>Saniyede metre, x1 hizda. Sakin bir servis yuruyusu.</summary>
        public float Speed = 1.15f;

        /// <summary>
        /// Oyunun hiz carpani. GameApp'ten her karede yaziliyor; static,
        /// cunku sahnedeki her figur ayni saati kullaniyor ve figur
        /// basina bir baglanti tutmak elli nesnede elli referans demek.
        /// </summary>
        public static float GameSpeed = 1f;

        /// <summary>
        /// Bu carpanin ustunde yuruyus CIZILMIYOR, isinlaniyor.
        ///
        /// x4'te bir figur saniyede ~4,6 m gidiyor; 30 fps'te kare
        /// basina 15 cm. Ustune cikildiginda adimlar arasi mesafe
        /// figurun kendisinden buyuyor ve hareket "yuruyus" degil
        /// "sicrama" olarak okunuyor.
        /// </summary>
        public const float TeleportAbove = 4.5f;

        /// <summary>
        /// BU figurun uyabilecegi en yuksek zaman carpani.
        ///
        /// Varsayilan sonsuz: salondaki herkes oyun saatine uyuyor,
        /// cunku onlarin yaptigi is simulasyonda bir karsiligi olan bir
        /// is ve gerisinde kalmalari ekranda yalan olurdu.
        ///
        /// Sokaktan gecenlerin ise simulasyonda karsiligi YOK - onlar
        /// susa. Tavan onlar icin var ve sebebi olculdu: oyuncu hizi
        /// TeleportAbove'un ustune cikardiginda Walker yuruyusu cizmeyip
        /// dogrudan yolun sonuna isinliyor; sokakta yolun sonu iki nokta
        /// oldugu icin bes yaya o iki noktada UST USTE yigiliyordu.
        /// Tur bunu "en kotu 4 cift ic ice" diye olctu.
        /// </summary>
        public float SpeedCap = float.PositiveInfinity;

        /// <summary>
        /// BU KAREDE gercekten kullanilan yer hizi (m/sn).
        ///
        /// Denetim bunu okuyor, Speed x GameSpeed'i yeniden HESAPLAMIYOR.
        /// Sebep olculdu: GameSpeed'i GameApp.Update yaziyor ve kare
        /// icindeki sirasi Walker.Update'e gore garanti degil. Yeniden
        /// hesaplayan bir kontrol, oyun hizinin degistigi karede klip
        /// temposunu bir onceki hiza gore olcup %123 "kayma" bildiriyordu -
        /// oysa figur o karede dogru mesafeyi kat etmisti ve bir sonraki
        /// kare zaten duzeltiyordu.
        ///
        /// Olculmesi gereken sey "klip temposu, KAT EDILEN mesafeye uydu
        /// mu" - o da tam olarak bu alan.
        /// </summary>
        public float LastGroundSpeed { get; private set; }

        private readonly List<Vector3> _path = new List<Vector3>(4);
        private int _at;
        private System.Action _onArrive;
        private float _faceYaw;

        /// <summary>Hedefe varmadan mi.</summary>
        public bool Moving { get { return _at < _path.Count; } }

        /// <summary>Su anki yolun son noktasi; yol yoksa bulundugu yer.</summary>
        public Vector3 Destination
        {
            get
            {
                return _path.Count > 0 ? _path[_path.Count - 1]
                                       : transform.localPosition;
            }
        }

        // =====================================================================
        /// <summary>Aninda yerlestirir; varsa yolu iptal eder.</summary>
        public void Warp(Vector3 local, float yaw)
        {
            if (Body != null) Body.ResetPlaybackSpeed();
            _path.Clear();
            _at = 0;
            _onArrive = null;
            transform.localPosition = local;
            _faceYaw = yaw;
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Verilen yol boyunca yurur, sonunda onArrive cagirir.
        ///
        /// finalYaw: varista donecegi yon. NaN ise son adimin yonunde
        /// kaliyor - yuruyup bir masaya varan garson icin dogrusu bu.
        /// </summary>
        public void GoTo(List<Vector3> waypoints, float finalYaw,
                         System.Action onArrive)
        {
            _path.Clear();
            if (waypoints != null) _path.AddRange(waypoints);
            _at = 0;
            _onArrive = onArrive;
            _finalYaw = finalYaw;

            if (_path.Count == 0)
            {
                Arrive();
                return;
            }
            if (Body != null) Body.Set(Figure.Pose.Walk);
        }

        private float _finalYaw = float.NaN;

        /// <summary>Yuruyusu keser, oldugu yerde birakir.</summary>
        public void Stop()
        {
            _path.Clear();
            _at = 0;
            _onArrive = null;
            if (Body != null) Body.ResetPlaybackSpeed();
        }

        // =====================================================================
        private void Update()
        {
            if (_at >= _path.Count) return;

            // Duraklatildiginda YURUYUS DE DURUYOR (GameSpeed = 0).
            if (GameSpeed <= 0.001f) return;
            float carpan = Mathf.Min(SpeedCap, Mathf.Max(0.5f, GameSpeed));
            Vector3 hedef = _path[_at];
            Vector3 su = transform.localPosition;

            // Cok hizlida yuruyus okunmuyor: isinla.
            if (carpan > TeleportAbove)
            {
                transform.localPosition = _path[_path.Count - 1];
                _at = _path.Count;
                Arrive();
                return;
            }

            Vector3 fark = hedef - su;
            fark.y = 0f;
            float uzak = fark.magnitude;
            float adim = Speed * carpan * Time.deltaTime;

            if (uzak <= adim)
            {
                transform.localPosition = new Vector3(hedef.x, su.y, hedef.z);
                _at++;
                if (_at >= _path.Count) Arrive();
                return;
            }

            Vector3 yon = fark / uzak;
            transform.localPosition = su + yon * adim;

            // YUZ GITTIGI YONE DONUYOR, ANINDA DEGIL.
            //
            // Aninda dondurmek kose donuslerinde figuru "sicratiyor";
            // yumusatma, sekiz kare suren bir donus veriyor ve yuruyus
            // klibiyle ayni ritimde okunuyor.
            _faceYaw = Mathf.LerpAngle(
                _faceYaw, Mathf.Atan2(yon.x, yon.z) * Mathf.Rad2Deg,
                Mathf.Clamp01(Time.deltaTime * 10f));
            transform.localRotation = Quaternion.Euler(0f, _faceYaw, 0f);

            // Animator yuruyus boyunca ACIK kalmali. Figure gecisten
            // 0,9 sn sonra kapatiyor - uzun bir yuruyuste figur donup
            // duran bir poz olarak kayardi.
            //
            // KLIP HIZI YER HIZINA BAGLI.
            //
            // Once Anim.speed hicbir yerde ayarlanmiyordu: figur x4 oyun
            // hizinda saniyede 4,6 m gidiyor ama bacaklar 1x tempoda
            // oynuyordu - ayaklar yerde kaydiriyordu. Sokaktaki yayalarin
            // hizlari da rastgele (0,92-1,34) ve ayni sekilde
            // eslesmiyordu.
            if (Body != null)
            {
                Body.HoldAwake();
                LastGroundSpeed = Speed * carpan;
                Body.SetGroundSpeed(LastGroundSpeed);

                // KLIP GERCEKTEN ILERLIYOR MU.
                //
                // Yuruyen bir figurun klibi her karede ilerlemeli.
                // Ilerlemiyorsa figur KAYIYOR: bacaklar donmus, govde
                // gidiyor. Kullanicinin bildirdigi sey buydu ve sebebi
                // kliplerin dongusuz ice aktarilmasiydi - klip bir kez
                // oynayip son karesinde duruyordu.
                //
                // Sayac STATIK ve kumulatif: tur toplami soruyor.
                float p = Body.ClipProgress;
                if (p >= 0f)
                {
                    if (p > _sonKlip + 0.0001f) AnimAdvanced++;
                    else AnimStalled++;
                    _sonKlip = p;
                }
            }
        }

        /// <summary>
        /// Yuruyen figurlerde klibin ilerledigi ve DONDUGU kare sayisi.
        /// Turun sorabilmesi icin; kumulatif ve statik.
        /// </summary>
        public static int AnimAdvanced, AnimStalled;

        private float _sonKlip = -1f;

        private void Arrive()
        {
            // Yuruyus bitti: klip hizi normale donuyor. Duran bir figurun
            // oturma ya da dograma klibi, yer hiziyla olceklenmemeli.
            if (Body != null) Body.ResetPlaybackSpeed();

            if (!float.IsNaN(_finalYaw))
            {
                _faceYaw = _finalYaw;
                transform.localRotation = Quaternion.Euler(0f, _finalYaw, 0f);
            }
            System.Action f = _onArrive;
            _onArrive = null;
            if (f != null) f();
        }
    }
}
