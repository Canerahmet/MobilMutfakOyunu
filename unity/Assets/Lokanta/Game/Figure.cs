using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Bir insan figurunun DURUSU. Musteri oturur, asci tezgahta calisir,
    /// garson tabak tasir.
    ///
    /// Neden ayri bir bilesen: figurler havuzdan geliyor ve ayni nesne
    /// gun icinde once musteri sonra bos duruyor. Durusu nesnenin uzerinde
    /// tutmak, "bu figur su an ne yapiyor" sorusunun tek bir yerde
    /// cevaplanmasini sagliyor.
    ///
    /// Klipler pakette hazir geliyor (Kenney Mini Characters, CC0); iskelet
    /// butun figurlerde ayni oldugu icin tek bir denetleyici hepsini
    /// suruyor.
    /// </summary>
    public sealed class Figure : MonoBehaviour
    {
        public enum Pose { Idle, Walk, Sit, Serve, Carry, Chop, Wash, Pick }

        /// <summary>Denetleyicideki durum adlari. Pose sirasiyla ayni.</summary>
        public static readonly string[] StateNames =
            { "idle", "walk", "sit", "serve", "carry", "chop", "wash", "pick" };

        /// <summary>
        /// Paketteki klip adlari. Pose sirasiyla ayni.
        ///
        /// MUTFAK ISLERI paketin hazir kliplerinden kuruluyor; yeni
        /// animasyon uretilmiyor:
        ///   chop  = attack-melee-right - yukaridan asagi inen kol.
        ///           Satirla dograma hareketinin ta kendisi.
        ///   wash  = interact-left      - onunde iki elle ugrasma.
        ///   pick  = pick-up            - egilip bir sey alma; asci
        ///           buzdolabinin onunde bunu yapiyor.
        /// </summary>
        public static readonly string[] ClipNames =
            { "idle", "walk", "sit", "interact-right", "holding-both",
              "attack-melee-right", "interact-left", "pick-up" };

        public Animator Anim;

        /// <summary>
        /// Klipler, EDITOR ONIZLEMESI icin. Oyunda denetleyici suruyor;
        /// editor kipinde Animator islemedigi icin klip elle orneklen&#305;yor.
        /// </summary>
        public AnimationClip[] Clips;

        private Pose _pose = Pose.Idle;
        private bool _started;

        // =====================================================================
        // OTURUSTA DIZ BUKULUYOR.
        //
        // Paketin iskeletinde diz yoktu; bacak basina tek kemik vardi ve
        // oturan figur ya minderin icinden geciyor ya da bacaklarini
        // one uzatip yere oturmus gibi duruyordu. Diz kemigi artik
        // URETIMDE ekleniyor (Editor/ArtPrefabs.AddKnees) ve hicbir klip
        // onu oynatmiyor - yani butun eski duruşlar aynen duruyor,
        // yalnizca burasi mudahale ediyor.
        //
        // Kural tek cumle: BALDIR DIKEY. Uyluk klibin dediği yerde
        // kaliyor (oturma klibi onu one uzatiyor), diz ise baldiri her
        // karede dunyaya gore asagi ceviriyor. Ayaklar boylece minderin
        // onunde, yere dogru sarkiyor.
        //
        // Neden dunyaya gore: kemigin kendi eksenlerinin nereye baktigini
        // bilmek gerekmiyor. Diz kemigi baglanma durusunda (bacaklar
        // asagi) kokune gore hangi aciysa, oturuşta da o aci yaziliyor.
        //
        // Neden LateUpdate: Animator pozu Update'ten SONRA yaziyor.
        /// <summary>Uyluk kemikleri. Prefab uretiminde baglaniyor.</summary>
        public Transform[] Legs;

        /// <summary>Baldir kemikleri. Prefab uretiminde baglaniyor.</summary>
        public Transform[] Knees;

        /// <summary>
        /// Bacak kemiklerinin BAGLANMA acilari, figurun kokune gore.
        ///
        /// NEDEN CALISMA ANINDA OKUNMUYOR: ilk yazim bunlari ilk
        /// kullanimda okuyordu ve okudugu sey baglanma acisi DEGILDI -
        /// klip o ana kadar pozu coktan degistirmis oluyordu. Sonuc:
        /// baldir "asagi" diye yazilan yere gidiyor ama o yer artik
        /// uylugun yonu. Prefab uretiminde model kesinlikle baglanma
        /// durusunda; dogru an orasi.
        /// </summary>
        public Quaternion[] LegRest;

        /// <summary>Baldirlarin baglanma acilari. Bkz. LegRest.</summary>
        public Quaternion[] KneeRest;

        // =====================================================================
        // OTURUSTA DIZ BUKULUYOR.
        //
        // Paketin iskeletinde diz YOKTU; bacak basina tek kemik vardi ve
        // oturan figur ya minderin icinden geciyor ya da bacaklarini one
        // uzatip yere oturmus gibi duruyordu. Diz kemigi artik uretimde
        // ekleniyor (Editor/ArtPrefabs.AddKnees) ve hicbir klip onu
        // oynatmiyor - yani butun eski duruşlar aynen duruyor, yalnizca
        // burasi mudahale ediyor.
        //
        // Kural iki cumle: UYLUK ONE, BALDIR ASAGI. Ikisi de figurun
        // kendi uzayinda, baglanma acisindan olculerek.
        //
        // Neden LateUpdate: Animator pozu Update'ten SONRA yaziyor.

        /// <summary>
        /// Uylugun baglanma yonunden (asagi) donusu, derece.
        /// -90 tam yatay; -78 ucu hafif asagi egik, oturmus bir insan gibi.
        ///
        /// STATIC ALAN, SABIT DEGIL: olcum araci bu iki aciyi supurup
        /// tek karede yan yana koyabilsin diye. Oyun degistirmiyor.
        /// </summary>
        public static float ThighAngle = -78f;

        /// <summary>Baldirin dikeyden one egimi, derece. Eksi = one.</summary>
        public static float ShinTilt = -6f;

        private void LateUpdate() { BendKnees(); }

        /// <summary>
        /// Oturuşta uylugu one, baldiri asagi cevirir. Editor onizlemesi
        /// de cagiriyor - iki yerde iki ayri hesap olmasin diye.
        /// </summary>
        public void BendKnees()
        {
            if (_pose != Pose.Sit || Knees == null || Knees.Length == 0) return;
            if (LegRest == null || KneeRest == null) return;

            // LEGS ICIN DE KORUMA.
            //
            // Uc alan icin vardi, Legs icin yoktu; asagidaki dongu
            // `Legs.Length` okuyor. Prefab ureteci Knees+KneeRest
            // baglayip Legs'i baglamazsa (AddKnees'te ayri adimlar)
            // salon doldugu anda kare basina ONLARCA
            // NullReferenceException - LateUpdate her oturan figurde
            // her karede calisiyor.
            if (Legs == null) return;

            Quaternion kok = transform.rotation;

            Vector3 uylukYon = kok * (Quaternion.Euler(ThighAngle, 0f, 0f) * Vector3.down);
            for (int i = 0; i < Legs.Length && i < LegRest.Length; i++)
                Aim(Legs[i], LegRest[i], uylukYon);

            // BALDIR: uyluk nereye giderse gitsin dunyada asagi.
            Vector3 baldirYon = kok * (Quaternion.Euler(ShinTilt, 0f, 0f) * Vector3.down);
            for (int i = 0; i < Knees.Length && i < KneeRest.Length; i++)
                Aim(Knees[i], KneeRest[i], baldirYon);
        }

        /// <summary>
        /// Kemigi, BAGLANMA durusunda asagi bakan ekseni verilen dunya
        /// yonune bakacak sekilde cevirir.
        ///
        /// NEDEN NISAN, NEDEN ACI DEGIL: aciyla kurmak ("kokun uzayinda
        /// su kadar dondur") kemigin kendi eksenlerinin nereye baktigini
        /// bilmeyi gerektiriyor ve bu pakette uyluk ile baldirinki AYNI
        /// DEGIL - uyluk beklendigi gibi donerken baldir bambaska bir
        /// yere gidiyordu. Nisan alma o bilgiyi hic sormuyor: kemigin
        /// baglanma durusundaki "asagi" ekseni bulunuyor ve nereye
        /// bakmasi isteniyorsa oraya cevriliyor.
        /// </summary>
        private static void Aim(Transform bone, Quaternion rest, Vector3 worldDir)
        {
            if (bone == null || worldDir.sqrMagnitude < 0.0001f) return;

            // Baglanma durusunda kemik asagi bakiyordu; o yonun kemigin
            // KENDI uzayindaki karsiligi.
            Vector3 local = Quaternion.Inverse(rest) * Vector3.down;
            Vector3 simdi = bone.rotation * local;
            bone.rotation = Quaternion.FromToRotation(simdi, worldDir) * bone.rotation;
        }

        /// <summary>Olcum araci icin: diz kemigi bulundu mu.</summary>
        public int KneeCount { get { return Knees == null ? 0 : Knees.Length; } }

        /// <summary>
        /// Gecisin bitmesine kalan sure. Sifirin altina inince Animator
        /// KAPANIYOR.
        ///
        /// Oturan musteri sonsuza kadar ayni klibi oynatiyordu ve dolu
        /// bir salonda 67 figurun 67 Animator'i vardi. Animator basina
        /// sabit maliyet (durum makinesi degerlendirmesi + is
        /// zamanlamasi) kucuk iskelette bile ~45 mikrosaniye, yani
        /// karede ~3 ms - ve bunun karsiligi SIFIR, cunku oturan
        /// musteri kipirdamiyor.
        ///
        /// Gecis bittiginde poz zaten son karede kaldigi yerde duruyor;
        /// Animator'i kapatmak gorunumu degistirmiyor. Duruş degisince
        /// Set() onu geri aciyor.
        /// </summary>
        private float _settleLeft;

        private const float CrossFade = 0.18f;

        /// <summary>
        /// Yuruyus klibinin KENDI hizi (m/sn) - yani ayaklarin kaymadigi
        /// yer hizi.
        ///
        /// Klip yerinde sayiyor (kok hareketi yok), o yuzden bu sayi
        /// hicbir yerde yazmiyor ve OLCULDU: Editor/PlacementAudit
        /// "YURUYUS" satiri klibi yirmi dort noktada ornekleyip iki
        /// ayagin en uzak acilmasini (adim boyu) buluyor; bir cevrim iki
        /// adim, yani dogal hiz = 2 x adim / klip suresi.
        ///
        /// Neden gerekli: Anim.speed hicbir yerde ayarlanmiyordu. Figur
        /// x4 oyun hizinda dort kat hizli gidiyor ama bacaklar ayni
        /// tempoda oynuyordu - ayaklar yerde kayiyordu.
        ///
        /// Olculen: klip 0,67 sn, adim 0,426 m -> 2 x 0,426 / 0,67 =
        /// 1,28 m/sn. Walker.Speed ise 1,15 - yani x1'de bile %11
        /// uyumsuzluk vardi, x4'te 3,6 kat.
        ///
        /// Bu sayi degisirse denetim "SAPMA VAR" diye yaziyor.
        /// </summary>
        public const float WalkClipSpeed = 1.28f;

        /// <summary>
        /// Klibin oynatma hizini YER HIZINA baglar.
        ///
        /// Yuruyen figur icin: bacaklar, govdenin gercekte kat ettigi
        /// mesafeye gore donuyor. Duran figur icin 1 - oturma, dograma,
        /// yikama kliplerinin hizi yer hiziyla ilgili degil.
        ///
        /// TAVANLI: cok yuksek bir carpan bacaklari titreme haline
        /// getiriyor ve zaten Walker x4,5 ustunde isinlaniyor.
        /// </summary>
        public void SetGroundSpeed(float metersPerSecond)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return;

            float k = metersPerSecond > 0.01f
                ? metersPerSecond / WalkClipSpeed
                : 1f;
            if (k < 0.35f) k = 0.35f;
            if (k > 5f) k = 5f;
            if (!Mathf.Approximately(Anim.speed, k)) Anim.speed = k;
        }

        /// <summary>Oynatma hizini normale dondurur.</summary>
        public void ResetPlaybackSpeed()
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return;
            if (!Mathf.Approximately(Anim.speed, 1f)) Anim.speed = 1f;
        }

        public Pose Current { get { return _pose; } }

        /// <summary>
        /// Su anki klibin ilerlemesi (dongu sayisi dahil). Turun
        /// sorabilmesi icin.
        ///
        /// Neden gerekli: "ayak kaymasi" olcusu klip HIZINI yer hiziyla
        /// karsilastiriyordu ve DONMUS bir klibi goremiyordu - klip hic
        /// ilerlemese bile oran dogru cikiyor. Kullanicinin gordugu sey
        /// ("adim atmiyorlar, kayiyorlar") tam olarak buydu ve sebebi
        /// kliplerin dongusuz ice aktarilmasiydi.
        ///
        /// Bu sayi ILERLIYOR mu diye bakmak, animasyonun gercekten
        /// oynadigini soyleyen tek olcu.
        /// </summary>
        public float ClipProgress
        {
            get
            {
                if (Anim == null || !Anim.enabled
                    || Anim.runtimeAnimatorController == null) return -1f;
                return Anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }
        }

        public void Set(Pose p)
        {
            if (_started && p == _pose) return;
            _started = true;
            _pose = p;

            if (Anim == null || Anim.runtimeAnimatorController == null) return;

            // Gecis suresi 0,18 sn: oturma ile ayaga kalkma arasinda gozle
            // gorulur ama beklenmeyen bir duraklama yaratmayan bir gecis.
            if (!Anim.enabled) Anim.enabled = true;
            Anim.CrossFadeInFixedTime(StateNames[(int)p], CrossFade, 0);

            // Gecis + bir kliplik pay: dongusel kliplerde ilk tur
            // tamamlansin, yoksa yurume ortasinda donuyor.
            _settleLeft = CrossFade + 0.9f;
        }

        /// <summary>
        /// Gecis bitince Animator'i kapatir.
        ///
        /// Update yalnizca gecis SURERKEN is yapiyor; kapandiktan sonra
        /// tek yaptigi bir float karsilastirmasi, ve o da Animator'in
        /// kendi maliyetinin yaninda olculemeyecek kadar kucuk.
        /// </summary>
        private void Update()
        {
            if (_settleLeft <= 0f) return;

            _settleLeft -= Time.deltaTime;
            if (_settleLeft > 0f) return;

            if (Anim != null && Anim.enabled) Anim.enabled = false;
        }

        /// <summary>
        /// Animator'i acik TUTAR.
        ///
        /// Set() gecisten 0,9 saniye sonra Animator'i kapatiyor ve bu
        /// dogru: oturan bir musteri kipirdamiyor, degerlendirilmesi
        /// bosuna. Ama YURUYEN bir figur icin yanlis - yuruyus dokuz
        /// saniye surebilir ve figur yolun ortasinda donup kalirdi.
        ///
        /// Walker her karede bunu cagiriyor; yuruyus bitince cagirmayi
        /// birakiyor ve normal kapanma isliyor. Yani "acik kalsin"
        /// bilgisi bir bayrak degil, bir KALP ATISI - unutulursa
        /// kendiliginden sonlanan turden.
        /// </summary>
        public void HoldAwake()
        {
            if (Anim == null) return;
            if (!Anim.enabled) Anim.enabled = true;
            if (_settleLeft < 0.25f) _settleLeft = 0.25f;
        }

        /// <summary>
        /// Havuza donen figur. Bir sonraki kullanimda Set() yeniden
        /// poz vermeli, yoksa eski duruşta kaliyor.
        /// </summary>
        public void Release()
        {
            _started = false;
            _settleLeft = 0f;
        }

        /// <summary>
        /// Durusu tek karede uygular. Editor onizlemesi icin: editor
        /// kipinde Animator islemiyor ve butun figurler baglanma
        /// durusunda - yani kollari yana acik - kaliyordu.
        /// </summary>
        public void Sample(Pose p, float time)
        {
            _pose = p;
            _started = true;
            int i = (int)p;
            if (Clips == null || i >= Clips.Length || Clips[i] == null) return;

            Clips[i].SampleAnimation(gameObject, time);
            BendKnees();
        }
    }
}
