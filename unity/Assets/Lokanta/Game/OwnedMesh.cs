using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// KENDI ORGUSUNU SAHIPLENEN NESNE.
    ///
    /// Modeler her cagrida `new Mesh()` uretiyor. Mesh bir
    /// UnityEngine.Object'tir ve GameObject yok edilince PESINDEN
    /// GITMEZ: MeshFilter.sharedMesh ile tutulan orgu yetim kalir ve
    /// sahne bosaltilana kadar bellekte durur.
    ///
    /// En kotu yol kiyafetler: kadro bilesimi her degistiginde TUM
    /// personel yeniden giydiriliyor ve kisi basi uc-dort orgu
    /// yaratiliyor. On bir personel ve altmis gunluk bir kampanyada
    /// yuzlerce ise alma / bulasik nobeti degisimi = binlerce yetim
    /// orgu. Ayni hata bir kat asagida (CookRoutine'in tava malzemesi)
    /// bir kez yasandi ve orada duzeltildi; Modeler gelince bir kat
    /// yukarida tekrarlandi.
    ///
    /// Cozum neden BURASI: sahiplik nesnenin kendisinde durursa,
    /// orguyu kimin yok edecegini her cagiranin ayri ayri hatirlamasi
    /// gerekmiyor. Clear(), Strip(), sahne degisimi, prefab silinmesi -
    /// hepsi ayni yoldan gecer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh Mesh;

        /// <summary>
        /// Uygulama KAPANIYOR mu.
        ///
        /// Kapanista Unity zaten butun varliklari bosaltiyor ve o
        /// sirada elle Destroy cagirmak isin en kotu halinde surecin
        /// cokmesine yol aciyor: turun ilk kosusu 0xC0000005 ile
        /// dondu ve yigin tamamen kapanis temizliginin icindeydi.
        ///
        /// Sahiplik OYUN SIRASINDA anlamli - sizinti orada oluyor.
        /// Kapanista yapacak bir sey yok.
        /// </summary>
        private static bool _kapaniyor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Izle()
        {
            _kapaniyor = false;
            Application.quitting += () => { _kapaniyor = true; };
        }

        private void OnDestroy()
        {
            if (_kapaniyor || Mesh == null) return;
            if (Application.isPlaying) Destroy(Mesh);
            else DestroyImmediate(Mesh);
            Mesh = null;
        }
    }
}
