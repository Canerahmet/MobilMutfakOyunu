using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// CALISAN BIR OCAK. Kapak aciliyor, icerisi yaniyor, gozler alev
    /// aliyor.
    ///
    /// Neden gerekli: mutfak, ekranin ucte birini kapliyor ve icinde
    /// hicbir sey olmuyordu. Simulasyon her an hangi istasyonda kac
    /// tabak pistigini biliyor; o bilgi hicbir yere cizilmiyordu. Bir
    /// yonetim oyununda "mutfak sikisti" en sik verilen karar ve
    /// oyuncunun onu gorecegi tek yer mutfagin kendisi.
    ///
    /// PARCACIK YOK. docs/19 dusuk seviye bir Adreno'yu hedefliyor ve
    /// parcacik sistemi o sinif cihazlarda belgelenmis bir doldurma
    /// darbogazi. Alev de lamba da BIRER KUTU: emissive renkli, golge
    /// atmayan, carpisani olmayan. Uc ocak icin alti kutu - cizim
    /// cagrisi olarak olculemez.
    ///
    /// CAM GERCEKTEN SAYDAM. Firin kapaginin onune ince bir saydam
    /// panel ve ARKASINA bir lamba paneli konuyor: lamba yaninca isik
    /// camin arkasindan goruunuyor, sonunce koyu cam kaliyor. Saydamlik
    /// SRP toplu cizimini bozuyor ama sahnede en fazla uc tane var.
    /// </summary>
    public sealed class Appliance : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Kapagin acilma acisi. Gercek bir firin kapagi asagi dusuyor.</summary>
        private const float OpenAngle = -72f;

        /// <summary>Acilip kapanma suresi. Kapak agir, hizli acilmamali.</summary>
        private const float DoorSeconds = 0.45f;

        private Transform _hinge;      // kapagin menteşesi
        private Renderer _lamp;        // firin ici lamba paneli
        private Renderer[] _flames;    // ocak gozleri
        private MaterialPropertyBlock _block;

        private bool _on;
        private float _t;              // 0 kapali, 1 acik

        private static readonly Color LampOff = new Color(0.05f, 0.05f, 0.06f);
        private static readonly Color LampOn = new Color(1.00f, 0.62f, 0.20f);
        private static readonly Color FlameOff = new Color(0.07f, 0.07f, 0.09f);
        private static readonly Color FlameOn = new Color(0.35f, 0.62f, 1.00f);

        // =====================================================================
        /// <summary>
        /// Ocagi hazirlar: kapaga menteşe, icine lamba, gozlere alev.
        /// </summary>
        /// <param name="mat">
        /// ISIKSIZ olmali (URP/Unlit). Lamba ve alev ANLAM tasiyor;
        /// isikli bir malzemede firinin icindeki lamba, oraya isik
        /// girmedigi icin karanlik bir panel olarak ciziliyor.
        /// </param>
        public static Appliance Attach(GameObject stove, Material mat, Material glass)
        {
            if (stove == null) return null;
            Appliance a = stove.AddComponent<Appliance>();
            a.Build(mat, glass);
            return a;
        }

        private void Build(Material mat, Material glass)
        {
            _block = new MaterialPropertyBlock();

            Transform door = FindChild(transform, "door");
            if (door != null)
            {
                // MENTESE KAPAGIN ALT KENARINDA.
                //
                // Modelin kendi pivotu kapagin ORTASINDA; oradan
                // dondurmek kapagi firinin icine gomuyor. Alt kenara bir
                // ara nesne konup kapak ona baglaniyor - gercek bir firin
                // kapagi da oradan doniyor.
                Bounds b = Bounds(door);
                float alt = b.min.y;

                GameObject pivot = new GameObject("Mentese");
                pivot.transform.SetParent(door.parent, false);
                pivot.transform.position = new Vector3(
                    door.position.x, alt, door.position.z);
                pivot.transform.rotation = door.rotation;

                door.SetParent(pivot.transform, true);
                _hinge = pivot.transform;

                // CAM KAPAKTA, LAMBA GOVDEDE.
                //
                // Ikisi de menteşeye baglanmisti ve kapak acilinca lamba
                // onunla birlikte doniyordu - gercek bir firinda lamba
                // GOVDENIN icinde durur, yalnizca cam kapakla gelir.
                // Kapali: isik camin arkasindan goruunuyor. Acik: dogrudan
                // goruunuyor.
                Vector3 boy = b.size;
                float en = Mathf.Max(0.12f, boy.x * 0.62f);
                float yuk = Mathf.Max(0.08f, boy.y * 0.55f);

                // MERKEZ, PIVOT DEGIL.
                //
                // Menteşe kapagin ALT KENARINA konuyor ve kapak modelinin
                // kendi pivotu ortalanmamis (yerel x'i 0,268). Panelleri
                // menteşeye gore (0, ...) koymak, ikisini de o kaymayla
                // birlikte YANA ittiriyordu - lamba firinin icinde degil
                // iki ocagin arasinda yaniyordu.
                //
                // Kapagin GORSEL merkezi menteşe uzayina cevriliyor;
                // model pivotunun nerede oldugu artik onemli degil.
                Vector3 orta = pivot.transform.InverseTransformPoint(b.center);

                // LAMBA CAM ILE KAPAK YUZEYININ ARASINDA.
                //
                // Iki deneme:
                //   1. Menteşeye bagli, kapagin onunde -> kapak acilinca
                //      lamba da doniyordu; gercek bir firinda lamba
                //      govdede durur.
                //   2. Govdeye bagli, firinin icinde -> DOGRU ama
                //      GORUNMUYOR: paketin kapak agi tamamen opak, yani
                //      icerideki hicbir sey camdan goruunmuyor.
                //
                // Gercekcilik burada okunabilirlige yeniliyor: lamba
                // kapagin YUZEYINDE, camin hemen arkasinda. Kapali
                // duruyorken camdan yanan bir panel goruunuyor; kapak
                // acilinca panel yukari bakiyor ve yine goruunuyor.
                // Ofsetler de menteşe-yerel uzayda olmali: dunya
                // metresini olcege bolerek.
                float pz = Mathf.Max(0.0001f, pivot.transform.lossyScale.z);
                _lamp = Panel(pivot.transform, "Lamba", mat,
                              orta + new Vector3(0f, 0f, -0.030f / pz),
                              new Vector3(en, yuk, 0.01f), LampOff);

                if (glass != null)
                    Panel(pivot.transform, "Cam", glass,
                          orta + new Vector3(0f, 0f, -0.042f / pz),
                          new Vector3(en + 0.02f, yuk + 0.02f, 0.012f),
                          new Color(0.14f, 0.16f, 0.18f, 0.45f));
            }

            // OCAK GOZLERI: DORT gozun dordune de alev, GOZUN UZERINDE.
            //
            // Konumlar TEPEDEN RENDER EDILIP OLCULDU
            // (Editor/FigureShot -> render/olcek_ocak_ustten.png):
            //
            //   x: gozler ocagin merkezine gore SIMETRIK, +-%20,5
            //   z: SIMETRIK DEGIL - arka sira +%20,4, on sira -%7,5
            //
            // Ilk yazimda ikisi de +-%21 idi ve x tutuyordu ama on
            // sira gozun yaklasik 9 cm onune dusuyordu: alev gozde
            // degil izgaranin bosluğunda yaniyordu. Kullanicinin
            // "alev biraz kaymis gibi" demesinin sayisal karsiligi bu.
            //
            // Neden simetrik degil: modelin on kenarinda dugme sirasi
            // var ve goz izgarasi ona yer birakmak icin arkaya kaymis.
            Bounds hep = Bounds(transform);
            float ust = hep.max.y - transform.position.y;
            const float ArkaZ = 0.204f;
            const float OnZ = -0.075f;
            _flames = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                float dx = ((i & 1) == 0 ? -1f : 1f) * hep.size.x * 0.205f;
                float dz = hep.size.z * ((i & 2) == 0 ? OnZ : ArkaZ);
                _flames[i] = Panel(transform, "Alev" + i, mat,
                                   new Vector3(dx, ust + 0.012f, dz),
                                   new Vector3(0.085f, 0.006f, 0.085f), FlameOff);
            }
        }

        // =====================================================================
        /// <summary>Istasyon calisiyor mu. Her karede degil, DEGISINCE.</summary>
        public void SetWorking(bool on)
        {
            if (on == _on) return;
            _on = on;
            Paint(_lamp, on ? LampOn : LampOff);
            for (int i = 0; _flames != null && i < _flames.Length; i++)
                Paint(_flames[i], on ? FlameOn : FlameOff);
        }

        private void Update()
        {
            if (_hinge == null) return;

            float hedef = _on ? 1f : 0f;
            if (Mathf.Approximately(_t, hedef)) return;

            _t = Mathf.MoveTowards(_t, hedef, Time.deltaTime / DoorSeconds);
            // Yavaslayarak: kapak agir, sona dogru oturuyor.
            float k = 1f - (1f - _t) * (1f - _t);
            _hinge.localRotation = Quaternion.Euler(OpenAngle * k, 0f, 0f);
        }

        // =====================================================================
        /// <summary>
        /// Panel. BOYUT DUNYA METRESI, yerel olcek degil.
        ///
        /// Menteşe, modelin kendi olceginin (0,204) altinda duruyor;
        /// dunya metresini dogrudan localScale'e yazmak panelleri bes
        /// kat kucultuyordu - cam, kapinin ortasinda kucuk bir kare
        /// olarak goruunuyordu. Ebeveynin olcegi burada telafi ediliyor,
        /// boylece cagiran taraf metre yazabiliyor.
        /// </summary>
        private Renderer Panel(Transform parent, string name, Material mat,
                               Vector3 localPos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.0001f, ps.x),
                size.y / Mathf.Max(0.0001f, ps.y),
                size.z / Mathf.Max(0.0001f, ps.z));
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Paint(r, c);
            return r;
        }

        private void Paint(Renderer r, Color c)
        {
            if (r == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_block);
        }

        private static Transform FindChild(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
        }

        private static Bounds Bounds(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.3f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
