using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// PROSEDUREL MODEL KURUCUSU: kutu, prizma, levha.
    ///
    /// Neden var: sahnenin esyalari bugune kadar ya hazir prefablardan
    /// ya da tek tek GameObject'lerden kuruluyordu. Ikisi de ayni
    /// bedeli odetiyor - her parca ayri bir cizim cagrisi. Sokak
    /// lambasi bunu bir kez cozdu (onbes kutu -> iki orgu); bu sinif
    /// ayni cozumu butun esyalara aciyor.
    ///
    /// RENGE GORE GRUPLANIYOR. URP/Lit kose rengi okumuyor, yani tek
    /// orguye birden fazla renk koymanin yolu yok. Kurucu parcalari
    /// RENKLERINE gore ayri orgulere topluyor: uc renkli bir esya uc
    /// cizim, onbes parcali bir esya degil.
    ///
    /// Butun olculer YEREL: kurucunun kokune gore. Yerlestirme cagiran
    /// tarafin isi.
    /// </summary>
    public sealed class Modeler
    {
        private sealed class Parca
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<int> T = new List<int>();
        }

        private readonly Dictionary<Color32, Parca> _renkler =
            new Dictionary<Color32, Parca>();

        private Parca Al(Color c)
        {
            Color32 k = c;
            Parca p;
            if (!_renkler.TryGetValue(k, out p))
            {
                p = new Parca();
                _renkler[k] = p;
            }
            return p;
        }

        /// <summary>
        /// PAH (chamfer) YARICAPI, metre. 0 = kapali, kutular keskin.
        ///
        /// Kullanicinin cumlesi: "modeller cok keskin duruyor, biraz
        /// daha yuvarlaksi koseleri olsa daha yumusak bir goruntu".
        ///
        /// Bu kamera mesafesinde pahin asil isi SILUETI degistirmek
        /// degil - 3 cm'lik bir pah ekranda bir iki piksel. Isi, kenar
        /// boyunca ISIGI KIRMAK: pah yuzeyi komsu iki yuzden farkli bir
        /// aciyla duruyor, yani her kenarda ince bir acik (ya da koyu)
        /// serit beliriyor. Duz golgelemeli bir sahnede yumusakligi
        /// veren sey o serit.
        ///
        /// Modeler ornegi basina ayarlanabiliyor: bir cagiran kendi
        /// esyasini keskin isterse 0 yaziyor.
        /// </summary>
        public float Pah = 0.055f;

        /// <summary>
        /// Pah, kutunun HER ekseninde o eksenin uzunluguna gore
        /// kisitlaniyor: en ince eksende bile kutuyu yiyemesin.
        /// 0,32 = eksenin en fazla %32'si (iki uctan %64).
        /// </summary>
        private const float PahOran = 0.32f;

        /// <summary>
        /// Pah icin bir eksenin "buyuk" sayildigi esik.
        ///
        /// KAPI EN INCE KENARA BAKMIYOR, KAC KENARIN BUYUK OLDUGUNA
        /// BAKIYOR. Ilk yazisimda kosul "en ince kenar >= 5 cm" idi ve
        /// oyunun EN GORUNUR parcasini eliyordu: masa tablasi
        /// 0,80 x 0,04 x 0,80, yani en ince kenari 4 cm. Tabla, tezgah
        /// ustu, sandalye oturagi, raf - hepsi yassi levha ve hepsi
        /// eleniyordu. Olcum de bunu soyledi: butun sahnede ucgen
        /// sayisi yalnizca %12 artti, cunku pah asil mobilyaya hic
        /// deymemisti.
        ///
        /// Ince ekseni korumak zaten kapinin isi degil: pah HER eksende
        /// o eksenin %22'siyle kisitli, yani 4 cm'lik bir tablada dikey
        /// pah kendiliginden 0,9 cm'ye iniyor. Yatayda 3 cm, dikeyde
        /// 0,9 cm - yassi bir levhanin dogru pahi tam olarak bu.
        ///
        /// Iki buyuk kenar sarti, cubuk seklindeki parcalari (korkuluk
        /// citasi, masa ayagi, direk) disarida birakiyor: tek uzun
        /// ekseni var, pah gorunmez kalir ama 44 ucgene mal olurdu.
        /// </summary>
        private const float PahBuyukKenar = 0.10f;

        /// <summary>Merkezi verilen eksen hizali kutu.</summary>
        public Modeler Box(Vector3 center, Vector3 size, Color c)
        {
            return BoxAt(center, size, Quaternion.identity, c);
        }

        /// <summary>Donmus kutu. Yaslanan levhalar ve egik parcalar icin.</summary>
        public Modeler BoxAt(Vector3 center, Vector3 size, Quaternion rot, Color c)
        {
            Parca p = Al(c);
            Vector3 h = size * 0.5f;
            Matrix4x4 m = Matrix4x4.TRS(center, rot, Vector3.one);

            int buyukKenar = (size.x >= PahBuyukKenar ? 1 : 0)
                           + (size.y >= PahBuyukKenar ? 1 : 0)
                           + (size.z >= PahBuyukKenar ? 1 : 0);
            if (Pah > 0.0001f && buyukKenar >= 2)
            {
                PahliKutu(p, m, h, new Vector3(
                    Mathf.Min(Pah, size.x * PahOran),
                    Mathf.Min(Pah, size.y * PahOran),
                    Mathf.Min(Pah, size.z * PahOran)));
                return this;
            }

            // Alti yuz, her biri KENDI koseleriyle: duz golgeleme, keskin
            // kenar. Paylasilan kose yumusak bir kutu verirdi ve oyunun
            // butun modelleri az yuzeyli.
            Yuz(p, m, new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z),
                new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z));      // +Z
            Yuz(p, m, new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z),
                new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z));    // -Z
            Yuz(p, m, new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z));      // +X
            Yuz(p, m, new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z),
                new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z));    // -X
            Yuz(p, m, new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z),
                new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z));    // +Y
            Yuz(p, m, new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z));    // -Y
            return this;
        }

        /// <summary>
        /// PAHLI KUTU: 6 icerlek yuz + 12 kenar seridi + 8 kose ucgeni.
        ///
        /// 44 ucgen (keskin kutu 12). Bedel gercek, o yuzden kisit
        /// cagiran tarafta degil BURADA: PahEnAzKalinlik.
        ///
        /// Her kosede uc nokta var - kutunun uc yuzune ait olanlar:
        ///
        ///     Nokta(..., eksen, ...) = o eksende KENARDA (h), oteki iki
        ///     eksende ICERIDE (h - r) duran nokta.
        ///
        /// Yuzler o noktalarin dordunden, kenar seritleri iki komsu
        /// yuzun ikiser noktasindan, kose ucgenleri bir kosenin uc
        /// noktasindan kuruluyor.
        ///
        /// ON IKI KENAR TAM BIR KEZ: (eksen, s) yuzu yalnizca `c`
        /// ekseninin iki yuzuyle eslestiriliyor. Uc eksen donunce
        /// 3 x 2 x 2 = 12 kenarin hepsi bir kez geciyor ve hicbiri iki
        /// kez gecmiyor.
        ///
        /// SARIM YONU ELLE YAZILMIYOR, HESAPLANIYOR. Yirmi alti yuzeyin
        /// sarimini elle dogru yazmak, bir tanesinin ters olup iceri
        /// bakan bir yuzey uretmesi demekti - ve ters yuzey hicbir hata
        /// vermeden GORUNMEZ oluyor. Sekil digbukey ve yerelde merkezi
        /// baslangicta oldugu icin olcut basit: yuzeyin normali kendi
        /// merkezinden DISARI bakmali; bakmiyorsa sira ters ceviriliyor.
        /// </summary>
        private static void PahliKutu(Parca p, Matrix4x4 m, Vector3 h, Vector3 r)
        {
            Vector3 ic = new Vector3(h.x - r.x, h.y - r.y, h.z - r.z);

            for (int eksen = 0; eksen < 3; eksen++)
            {
                int b = (eksen + 1) % 3;
                int c = (eksen + 2) % 3;

                for (int s = -1; s <= 1; s += 2)
                {
                    DisariYuz(p, m,
                        Nokta(h, ic, eksen, s, b, -1, c, -1),
                        Nokta(h, ic, eksen, s, b, 1, c, -1),
                        Nokta(h, ic, eksen, s, b, 1, c, 1),
                        Nokta(h, ic, eksen, s, b, -1, c, 1));

                    for (int sc = -1; sc <= 1; sc += 2)
                        DisariYuz(p, m,
                            Nokta(h, ic, eksen, s, b, -1, c, sc),
                            Nokta(h, ic, eksen, s, b, 1, c, sc),
                            Nokta(h, ic, c, sc, eksen, s, b, 1),
                            Nokta(h, ic, c, sc, eksen, s, b, -1));
                }
            }

            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        DisariUcgen(p, m,
                            new Vector3(sx * h.x, sy * ic.y, sz * ic.z),
                            new Vector3(sx * ic.x, sy * h.y, sz * ic.z),
                            new Vector3(sx * ic.x, sy * ic.y, sz * h.z));
        }

        /// <summary>
        /// Pahli kutunun kose noktasi: <paramref name="eksen"/> ekseninde
        /// kenarda (h), oteki iki eksende iceride (h - r).
        /// </summary>
        private static Vector3 Nokta(Vector3 h, Vector3 ic,
                                     int eksen, int sEksen,
                                     int b, int sb, int c, int sc)
        {
            Vector3 v = Vector3.zero;
            v[eksen] = sEksen * h[eksen];
            v[b] = sb * ic[b];
            v[c] = sc * ic[c];
            return v;
        }

        /// <summary>Sarimi DISARI bakacak sekilde duzelterek dortgen ekler.</summary>
        private static void DisariYuz(Parca p, Matrix4x4 m,
                                      Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            Vector3 merkez = (a + b + c + d) * 0.25f;
            if (Vector3.Dot(n, merkez) < 0f) Yuz(p, m, d, c, b, a);
            else Yuz(p, m, a, b, c, d);
        }

        /// <summary>Sarimi DISARI bakacak sekilde duzelterek ucgen ekler.</summary>
        private static void DisariUcgen(Parca p, Matrix4x4 m,
                                        Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f) < 0f) Ucgen(p, m, c, b, a);
            else Ucgen(p, m, a, b, c);
        }

        private static void Ucgen(Parca p, Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = p.V.Count;
            Vector3 pa = m.MultiplyPoint3x4(a);
            Vector3 pb = m.MultiplyPoint3x4(b);
            Vector3 pc = m.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa).normalized;
            p.V.Add(pa); p.V.Add(pb); p.V.Add(pc);
            p.N.Add(n); p.N.Add(n); p.N.Add(n);
            p.T.Add(i); p.T.Add(i + 2); p.T.Add(i + 1);
        }

        private static void Yuz(Parca p, Matrix4x4 m,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = p.V.Count;
            Vector3 pa = m.MultiplyPoint3x4(a);
            Vector3 pb = m.MultiplyPoint3x4(b);
            Vector3 pc = m.MultiplyPoint3x4(c);
            Vector3 pd = m.MultiplyPoint3x4(d);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa).normalized;
            p.V.Add(pa); p.V.Add(pb); p.V.Add(pc); p.V.Add(pd);
            p.N.Add(n); p.N.Add(n); p.N.Add(n); p.N.Add(n);
            p.T.Add(i); p.T.Add(i + 2); p.T.Add(i + 1);
            p.T.Add(i); p.T.Add(i + 3); p.T.Add(i + 2);
        }

        /// <summary>
        /// Cok kenarli prizma; tabani verilen noktada, +Y yonunde.
        ///
        /// Sekiz kenar sutun ve abajur icin, alti kenar saksi icin,
        /// dort kenar ise dondurulmus bir kutu demek.
        /// </summary>
        public Modeler Prism(int sides, float rBottom, float rTop, float height,
                             Vector3 at, Quaternion rot, Color c, bool caps = true)
        {
            Parca p = Al(c);
            Matrix4x4 m = Matrix4x4.TRS(at, rot, Vector3.one);

            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 p0 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rBottom, 0f, Mathf.Sin(a0) * rBottom));
                Vector3 p1 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rBottom, 0f, Mathf.Sin(a1) * rBottom));
                Vector3 p2 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rTop, height, Mathf.Sin(a0) * rTop));
                Vector3 p3 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rTop, height, Mathf.Sin(a1) * rTop));

                int b = p.V.Count;
                p.V.Add(p0); p.V.Add(p1); p.V.Add(p2); p.V.Add(p3);
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                p.N.Add(n); p.N.Add(n); p.N.Add(n); p.N.Add(n);
                p.T.Add(b); p.T.Add(b + 2); p.T.Add(b + 1);
                p.T.Add(b + 1); p.T.Add(b + 2); p.T.Add(b + 3);
            }

            if (!caps) return this;
            Kapak(p, m, sides, rTop, height, true);
            Kapak(p, m, sides, rBottom, 0f, false);
            return this;
        }

        private static void Kapak(Parca p, Matrix4x4 m, int sides, float r,
                                  float y, bool ust)
        {
            if (r <= 0.0001f) return;
            int b = p.V.Count;
            Vector3 n = m.MultiplyVector(ust ? Vector3.up : Vector3.down);
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides;
                p.V.Add(m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)));
                p.N.Add(n);
            }
            for (int i = 1; i < sides - 1; i++)
            {
                if (ust) { p.T.Add(b); p.T.Add(b + i); p.T.Add(b + i + 1); }
                else { p.T.Add(b); p.T.Add(b + i + 1); p.T.Add(b + i); }
            }
        }

        /// <summary>
        /// Orguleri kuruyor ve sahneye asiyor. Renk basina bir cizim.
        ///
        /// ISIKLI PARCA DALI KALDIRILDI. `glowMat`/`glowColor`
        /// parametreleri vardi ama hicbir cagri dort argumandan
        /// fazlasini vermiyordu: isikli parcalar (tabela, neon) ayri
        /// bir Modeler ornegiyle ve ayri malzemeyle kuruluyor
        /// (_decorGlow). Olu dal yaniltiyordu - "isikli parca ayri
        /// malzeme alir" diye okunuyor, gercekte hic calismiyordu.
        /// </summary>
        public GameObject Build(Transform parent, string name, Material mat,
                                MaterialPropertyBlock block)
        {
            GameObject kok = new GameObject(name);
            kok.transform.SetParent(parent, false);

            foreach (KeyValuePair<Color32, Parca> kv in _renkler)
            {
                Parca p = kv.Value;
                if (p.V.Count == 0) continue;

                Mesh mesh = new Mesh();
                mesh.name = name + "_" + kv.Key.r + "_" + kv.Key.g + "_" + kv.Key.b;
                mesh.SetVertices(p.V);
                mesh.SetNormals(p.N);
                mesh.SetTriangles(p.T, 0);
                mesh.RecalculateBounds();

                GameObject go = new GameObject(mesh.name);
                go.transform.SetParent(kok.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;

                // ORGU SAHIPLENILIYOR. Mesh bir UnityEngine.Object ve
                // GameObject yok edilince PESINDEN GITMEZ; OwnedMesh
                // onu nesnenin omruyle baglar. Ayrintili gerekce
                // OwnedMesh.cs'te.
                go.AddComponent<OwnedMesh>().Mesh = mesh;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                r.sharedMaterial = mat;
                r.GetPropertyBlock(block);
                block.SetColor(Shader.PropertyToID("_BaseColor"), (Color)kv.Key);
                r.SetPropertyBlock(block);
            }
            return kok;
        }
    }
}
