using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Masanin uzerinde duran DURUM ROZETI: bir sabir cubugu ve rengi.
    ///
    /// Bu olmadan servis asamasi izlenemiyordu. Sekiz dakika boyunca
    /// ekranda hicbir sey kipirdamiyor, ust seritteki iki sayi disinda
    /// hicbir bilgi akmiyordu; oyuncu hizi artirip bakmayi birakiyordu.
    /// Oysa cekirdek her masanin asamasini ve kalan sabrini zaten
    /// biliyordu - yalnizca cizilmiyordu.
    ///
    /// Cubuk RENK DEGIL UZUNLUK tasiyor: sabir azaldikca kisaliyor. Renk
    /// asamayi soyluyor ve ikisi birbirinin yedegi - renk koru bir
    /// oyuncu uzunlugu, kucuk ekranda uzunlugu secemeyen renkleri
    /// okuyor.
    ///
    /// Iki dortgen: koyu bir zemin ve uzerinde dolan bir dilim.
    ///
    /// MALZEME ISIKSIZ (URP/Unlit) OLMAK ZORUNDA. Burada bir zamanlar
    /// "emisyon kapali oldugu icin renkler isiktan bagimsiz okunuyor"
    /// yaziyordu ve bu, dogrunun tam tersiydi: emisyon kapaliysa renk
    /// tamamen isiga bagli. Olculdu - zeminle ayni Lit malzemeyi
    /// paylasan yesil rozet ekranda 1,47:1 kontrastla cikiyordu,
    /// yazili rengi ayni zeminde 7,57:1 verir. Anlamli bir grafik icin
    /// esik 3:1.
    /// </summary>
    public sealed class TableBadge : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Masa merkezinden yukseklik.
        ///
        /// Olculdu: 1,75 m denendi ve rozetler oturan figurlerin
        /// baslarinin arkasinda kaldi. Figur 1,45 m ve oturarak 0,35 m
        /// yukseltiliyor, ustelik kafalari govdenin ucte biri kadar - yani
        /// bas ustu 2,1 m civarinda. 2,45 m onlarin uzerinde duruyor.
        /// </summary>
        private const float Height = 2.45f;

        /// <summary>
        /// Genislik 0,88 m: masanin capiyla ayni. Rozet masaya AIT
        /// gorunmeli, havada duran ayri bir nesne gibi degil.
        /// </summary>
        private const float Width = 0.88f;
        // 0,11 -> 0,18. Genel gorunumde rozet 9 dp yuksekligindeydi;
        // bir telefonda o, ince bir cizgi. Masa araligi 1,70 m, yani
        // 0,18 komsu masanin rozetine degmiyor.
        private const float Thickness = 0.18f;

        private Transform _fill;
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _block;
        private Transform _camera;

        private int _shownBp = -1;
        private CustomerStage _shownStage = (CustomerStage)(-1);

        /// <summary>
        /// Secim isareti: rozetin arkasinda duran biraz daha genis,
        /// parlak bir cerceve.
        ///
        /// Secim GORUNMEK ZORUNDA. Dokunup da isaret gormeyen oyuncu
        /// dokunusun isleyip islemedigini bilemez ve tekrar dokunur -
        /// ki ikinci dokunus secimi birakiyor, yani tam tersini yapar.
        /// </summary>
        private Transform _mark;
        private bool _shownSelected;

        // =====================================================================
        public static TableBadge Create(Transform parent, Material material)
        {
            GameObject root = new GameObject("Rozet");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, Height, 0f);

            TableBadge badge = root.AddComponent<TableBadge>();
            badge.Build(material);
            root.SetActive(false);
            return badge;
        }

        private void Build(Material material)
        {
            GameObject back = Quad("Zemin", material, new Color(0.08f, 0.09f, 0.11f));
            back.transform.localScale = new Vector3(Width, Thickness, Thickness);
            back.transform.localPosition = Vector3.zero;

            GameObject fill = Quad("Dolgu", material, Color.white);
            fill.transform.localScale = new Vector3(Width, Thickness, Thickness);
            // Dilim SOLDAN doluyor: olcek merkezden buyudugu icin kendi
            // ebeveyni uzerinden kaydiriliyor.
            GameObject pivot = new GameObject("Pivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(-Width * 0.5f, 0f, -0.004f);
            fill.transform.SetParent(pivot.transform, false);
            fill.transform.localPosition = new Vector3(Width * 0.5f, 0f, 0f);

            // Secim cercevesi rozetten biraz BUYUK ve ARKADA: cubugu
            // ortmuyor, cevresinde ince bir kenar birakiyor.
            GameObject mark = Quad("Secim", material, new Color(1f, 0.93f, 0.72f));
            mark.transform.localScale =
                new Vector3(Width + 0.10f, Thickness + 0.06f, Thickness);
            mark.transform.localPosition = new Vector3(0f, 0f, 0.006f);
            mark.SetActive(false);

            _fill = pivot.transform;
            _mark = mark.transform;
            _fillRenderer = fill.GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private GameObject Quad(string name, Material material, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);

            // Carpisan kutu YOK: rozet dokunma hedefi degil, ve odaya
            // atilan isin onune gecmemeli.
            // EDITOR KIPINDE Destroy ERTELENIYOR ve carpisan kutu
            // sahnede KALIYOR. Goruntu araci Rebuild'i editor kipinde
            // kosuyor; kalan kutu, odaya dokunma isinini onunde
            // kesiyordu.
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            MaterialPropertyBlock b = new MaterialPropertyBlock();
            b.SetColor(BaseColorId, color);
            r.SetPropertyBlock(b);
            return go;
        }

        // =====================================================================
        /// <summary>Masanin durumunu yansitir. Bos masada rozet gizleniyor.</summary>
        public void Show(CustomerStage stage, int patienceBp, bool selected = false)
        {
            bool visible = stage != CustomerStage.None
                           && stage != CustomerStage.Done
                           && stage != CustomerStage.LeftAngry;

            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) return;

            if (selected != _shownSelected)
            {
                _shownSelected = selected;
                if (_mark != null) _mark.gameObject.SetActive(selected);
            }

            // Yalnizca DEGISINCE yaziliyor: her karede property block
            // yazmak, on dort masada kare basina on dort gereksiz cizim
            // grubu demek.
            int step = patienceBp / 200;              // %2'lik adimlar
            if (step == _shownBp && stage == _shownStage) return;
            _shownBp = step;
            _shownStage = stage;

            float k = Mathf.Clamp01(patienceBp / 10000f);
            _fill.localScale = new Vector3(k, 1f, 1f);

            _fillRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, ColorFor(stage, k));
            _fillRenderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Asama rengi, sabir azalinca kirmiziya kayiyor.
        ///
        /// Yemek YIYEN masa sakin yesil: orada yapilacak bir sey yok ve
        /// oyuncunun gozu bekleyen masalara gitmeli.
        /// </summary>
        private static Color ColorFor(CustomerStage stage, float patience)
        {
            if (stage == CustomerStage.Eating) return new Color(0.42f, 0.68f, 0.44f);
            if (stage == CustomerStage.WaitingToPay) return new Color(0.85f, 0.72f, 0.35f);

            // Bekleyen masa: sabir %30'un altina inince kirmizi.
            if (patience < 0.3f) return new Color(0.93f, 0.36f, 0.33f);
            if (patience < 0.6f) return new Color(0.93f, 0.70f, 0.33f);
            return new Color(0.55f, 0.72f, 0.90f);
        }

        // =====================================================================
        /// <summary>
        /// Rozet KAMERAYA doniyor. Kamera sabit acili olsa da iki kademe
        /// arasinda hareket ediyor; sabit bir donus, yaklasildiginda yandan
        /// gorunurdu.
        /// </summary>
        private void LateUpdate()
        {
            if (_camera == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                _camera = cam.transform;
            }
            transform.rotation = _camera.rotation;
        }
    }
}
