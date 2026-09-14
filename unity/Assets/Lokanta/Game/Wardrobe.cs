using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// PERSONELIN KIYAFETI: uc rol, uc siluet.
    ///
    /// Kullanicinin istegi acikti: "ascinin basinda sef sapkasi ve sef
    /// kiyafeti olsun, bulasikci onluk ve eldiven taksin, garson takim
    /// elbise giysin".
    ///
    /// Bizde personel ile musteri AYNI figurlerdi (BuildGameScene
    /// personeli erkek a/b/c'den seciyor), yani salonda kimin garson
    /// oldugunu oyuncu ancak HAREKETINDEN anlayabiliyordu. docs/25'in
    /// kurali: kimlik ORTAM, ISIK, SILUET ve KIYAFETLE kurulur.
    ///
    /// KEMIGE TAKILIYOR, KOKE DEGIL. Figur yururken govde ve bas
    /// sallaniyor; koke takilan bir kep havada kalir ve bu, animasyonun
    /// KENDISINI bozuk gosterir. Paketin iskeletinde "head", "torso",
    /// "arm-left" ve "arm-right" kemikleri var (FBX'te dogrulandi).
    ///
    /// OLCU FIGURDEN OKUNUYOR, SABIT SAYIDAN DEGIL. Ilk yazim kepi
    /// kemige gore sabit sayilarla koydu ve ekranda hicbir sey
    /// gorunmedi: kep basin ICINDE kaldi (kemik zincirinin kendi olcegi
    /// 1,19 ve bas kemigi boyunda duruyor). Butun parcalar artik
    /// figurun boyundan ve derisinin dunya kutusundan turetiliyor.
    /// </summary>
    public static class Wardrobe
    {
        /// <summary>Personelin rolu. Kiyafet buna gore kuruluyor.</summary>
        public enum Role { Cook, Dishwasher, Waiter }

        /// <summary>
        /// Kac giydirme DENENDI ve kaci TAMAMLANDI.
        ///
        /// Dress'in dort ayri erken cikisi var (kemik yok, cizici yok,
        /// olcu sacma, kol yok). Hepsi artik uyari basiyor ama uyari
        /// gunlukte kaybolabiliyor; bu iki sayi turun "giydirilen
        /// personel N/M" satirini besliyor, yani kiyafet kaybolursa
        /// otomatik olcum KIRMIZI yaniyor.
        /// </summary>
        public static int Attempted, Dressed;

        /// <summary>Olcumu sifirlar. Kadro yeniden kurulurken cagriliyor.</summary>
        public static void ResetCounters() { Attempted = 0; Dressed = 0; }


        private const string HatName = "Kep";
        private const string BodyName = "Kiyafet";
        private const string GloveName = "Eldiven";

        /// <summary>Sef beyazi: kep ve ceket.</summary>
        private static readonly Color Chef = new Color(0.949f, 0.945f, 0.929f);

        /// <summary>Sef ceketinin dugmeleri ve yaka cizgisi.</summary>
        private static readonly Color ChefTrim = new Color(0.427f, 0.451f, 0.494f);

        /// <summary>Bulasikcinin mesin onlugu: koyu ve mat.</summary>
        private static readonly Color Rubber = new Color(0.278f, 0.318f, 0.365f);

        /// <summary>Lastik eldiven. Sari: uzaktan okunan tek renk.</summary>
        private static readonly Color Glove = new Color(0.937f, 0.761f, 0.216f);

        /// <summary>Garsonun takimi, gomlegi ve papyonu.</summary>
        private static readonly Color Suit = new Color(0.137f, 0.153f, 0.192f);
        private static readonly Color Shirt = new Color(0.929f, 0.933f, 0.945f);
        private static readonly Color Bow = new Color(0.545f, 0.125f, 0.133f);

        /// <summary>Onlugun kemeri.</summary>
        private static readonly Color Belt = new Color(0.106f, 0.122f, 0.149f);

        /// <summary>
        /// Figuru giydirir. Ayni figur iki kez cagrilirsa eskisini
        /// temizler - kadro degisince ayni GameObject baska bir role
        /// gecebiliyor (asci cikip garson aliniyor, garson bulasiga
        /// geciyor) ve ustunde iki kiyafet birden kalmamali.
        /// </summary>
        public static void Dress(GameObject figure, Role role, Material mat,
                                 MaterialPropertyBlock block)
        {
            Attempted++;
            if (figure == null || mat == null) return;

            Transform head = Find(figure.transform, "head");
            Transform torso = Find(figure.transform, "torso");
            if (head == null || torso == null)
            {
                // SESSIZ ERKEN CIKIS YOK.
                //
                // Bu dosyanin kendi yorumu hatanin BIR KEZ yasandigini
                // anlatiyor ("SESSIZCE hicbir sey yapmadi") ama koruma
                // eklenmemis, yalnizca olcu kaynagi duzeltilmisti.
                // Kiyafet, uc rolu ayirt eden TEK kanal: kayboldugunda
                // mekanik gorunmez oluyor ve tek bir uyari basilmiyordu.
                Debug.LogWarning("Wardrobe: iskelette head/torso yok - "
                                 + figure.name + " giydirilemedi");
                return;
            }

            Transform solKol = Find(figure.transform, "arm-left");
            Transform sagKol = Find(figure.transform, "arm-right");

            Strip(head, HatName);
            Strip(torso, BodyName);
            Strip(solKol, GloveName);
            Strip(sagKol, GloveName);

            // DERI TEK BIR SkinnedMeshRenderer DEGIL.
            //
            // Ilk yazim oyle varsaydi ve SESSIZCE hicbir sey yapmadi:
            // paketin karakteri "body-mesh" ve "head-mesh" diye iki ayri
            // parcadan kuruluyor. Butun cizicilerin kutusu birlestiriliyor;
            // kiyafetin kendisi haric (ikinci cagride kendi kutusunu
            // olcmesin).
            Bounds b = new Bounds();
            bool ilk = true;
            foreach (Renderer rr in figure.GetComponentsInChildren<Renderer>())
            {
                Transform p = rr.transform.parent;
                if (p != null && (p.name == HatName || p.name == BodyName
                                  || p.name == GloveName)) continue;
                if (ilk) { b = rr.bounds; ilk = false; }
                else b.Encapsulate(rr.bounds);
            }
            if (ilk)
            {
                Debug.LogWarning("Wardrobe: figurde hic cizici yok - "
                                 + figure.name + " giydirilemedi");
                return;
            }

            float boy = b.size.y;
            if (boy < 0.05f)
            {
                Debug.LogWarning("Wardrobe: figur olculemedi (boy "
                                 + boy.ToString("0.000") + " m) - "
                                 + figure.name + " giydirilemedi");
                return;
            }

            // KIYAFET GOVDEYI SARIYOR, ONUNE YAPISMIYOR.
            //
            // Ilk yazim yalnizca ON yuze levha koyuyordu ve oyunun
            // kamerasi personelin cogu zaman SIRTINI goruyor: asci
            // ceketi de bulasikci onlugu de yarisi zaman gorunmuyordu.
            //
            // Kabuk derinligi figurun OLCULEN govde derinliginden
            // geliyor; detaylar (dugme, papyon) yalnizca on yuze.
            // OLCEK OLCUMDEN: 0,40 -> 0,80.
            //
            // Ilk deger goz karariydi ve kiyafet BEDENIN ICINDE kaldi:
            // olculdu (TANI satiri), figurun kutusu 1,14 x 1,00 x 0,51 -
            // genislikteki 1,14 KOLLAR, govde ise ~0,5 m. Yerel 0,70
            // genisligindeki ceket 0,40 olcekte 0,28 m ediyordu, yani
            // govdenin yarisi.
            //
            // Artik yerel 1,0 = boyun %80'i: ceket 0,56 m genisliginde
            // ve govdeyi sariyor. Kalinlik da OLCULEN derinlikten.
            float olcek = boy * 0.80f;
            float derinlik = b.size.z;
            float kalinlik = Mathf.Clamp(derinlik * 0.80f
                                         / Mathf.Max(0.0001f, olcek), 0.30f, 0.75f);

            switch (role)
            {
                case Role.Cook:
                    Hat(head, boy, b.max.y, mat, block);
                    ChefJacket(figure.transform, torso, olcek, kalinlik, mat, block);
                    break;

                case Role.Dishwasher:
                    RubberApron(figure.transform, torso, olcek, kalinlik, mat, block);
                    Gloves(solKol, sagKol, boy, mat, block);
                    break;

                default:
                    SuitJacket(figure.transform, torso, olcek, kalinlik, mat, block);
                    break;
            }

            Dressed++;
        }

        // =====================================================================
        /// <summary>
        /// Kep: bant ve uzerinde sisik govde. BIRIM orgu kuruluyor,
        /// dunya olcegine sonra cekiliyor.
        /// </summary>
        private static void Hat(Transform head, float boy, float tepe,
                                Material mat, MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            m.Prism(10, 0.40f, 0.42f, 0.22f, Vector3.zero,
                    Quaternion.identity, Chef);                       // bant
            m.Prism(10, 0.38f, 0.52f, 0.52f, new Vector3(0f, 0.20f, 0f),
                    Quaternion.identity, Chef);                       // sisik govde

            GameObject go = m.Build(head, HatName, mat, block);

            // Kep boyun %31'i kadar genis: gercek bir asci kepi de
            // kabaca basin capi kadardir ve bu olcekte siluet veriyor.
            Scale(go.transform, head, boy * 0.31f);
            go.transform.rotation = head.rotation;
            go.transform.position = new Vector3(head.position.x,
                                                tepe - boy * 0.045f,
                                                head.position.z);
        }

        /// <summary>
        /// SEF CEKETI: cift sira dugmeli beyaz ceket ve beyaz onluk.
        ///
        /// Onceki hali yalnizca beyaz bir onluktu ve asci ile garson
        /// ayni siluetti. Ceket govdenin ONUNU ve OMUZLARINI kapliyor;
        /// cift sira dugme, sef ceketini sef ceketi yapan tek detay.
        /// </summary>
        private static void ChefJacket(Transform kok, Transform torso, float olcek,
                                       float kal, Material mat,
                                       MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float on = kal * 0.5f + 0.02f;

            // Ceket govdesi: govdeyi saran kabuk.
            // GENISLIK GOVDEYE GORE: 0,70 -> 0,56.
            //
            // Ilk kabuk govdeden GENISTI ve ekranda iki yana tasan
            // beyaz kanatlar gibi duruyordu. Figurun govdesi ~0,45 m;
            // kabuk ondan biraz genis olmali, iki katı degil.
            m.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.46f, kal), Chef);
            m.Box(new Vector3(0f, 0.24f, 0f), new Vector3(0.60f, 0.12f, kal + 0.03f), Chef);

            // Cift sira dugme: yalnizca ON yuzde.
            for (int i = 0; i < 3; i++)
            {
                float y = 0.16f - i * 0.13f;
                m.Box(new Vector3(-0.10f, y, on),
                      new Vector3(0.06f, 0.06f, 0.04f), ChefTrim);
                m.Box(new Vector3(0.10f, y, on),
                      new Vector3(0.06f, 0.06f, 0.04f), ChefTrim);
            }

            // Belden asagi onluk ve kemer (kemer cepecevre).
            m.Box(new Vector3(0f, -0.32f, 0f), new Vector3(0.52f, 0.34f, kal), Chef);
            m.Box(new Vector3(0f, -0.17f, 0f), new Vector3(0.58f, 0.07f, kal + 0.03f), Belt);

            Place(m, kok, torso, olcek, mat, block);
        }

        /// <summary>
        /// BULASIKCININ ONLUGU: govdeyi bastan asagi kapatan mesin onluk
        /// ve boyun askisi.
        /// </summary>
        private static void RubberApron(Transform kok, Transform torso, float olcek,
                                        float kal, Material mat,
                                        MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float on = kal * 0.5f + 0.02f;

            // ONLUK ONDE, SIRT ACIK: gercek bir mesin onluk da oyle.
            // Ama sirtta askilar goruunuyor - figur sirtini dondugunde
            // oyuncu yine "bu bulasikci" diyebilmeli.
            m.Box(new Vector3(0f, -0.14f, on), new Vector3(0.52f, 0.66f, 0.06f), Rubber);
            m.Box(new Vector3(0f, 0.22f, on), new Vector3(0.28f, 0.20f, 0.06f), Rubber);

            // Boyun ve sirt askilari: omuzdan gecip sirtta caprazlaniyor.
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -0.13f : 0.13f;
                m.Box(new Vector3(x, 0.28f, 0f),
                      new Vector3(0.06f, 0.14f, kal + 0.03f), Rubber);
                m.Box(new Vector3(x, 0.02f, -on),
                      new Vector3(0.06f, 0.44f, 0.05f), Rubber);
            }

            // Kemer cepecevre.
            m.Box(new Vector3(0f, -0.16f, 0f), new Vector3(0.58f, 0.07f, kal + 0.03f), Belt);

            Place(m, kok, torso, olcek, mat, block);
        }

        /// <summary>
        /// GARSONUN TAKIMI: koyu ceket, beyaz gomlek seridi ve papyon.
        ///
        /// Yaka acikligini gomlek dolduruyor; papyon kucuk ama uzaktan
        /// "burasi servis" diyen tek leke.
        /// </summary>
        private static void SuitJacket(Transform kok, Transform torso, float olcek,
                                       float kal, Material mat,
                                       MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float on = kal * 0.5f + 0.02f;

            // Ceket: govdeyi saran koyu kabuk.
            m.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.50f, kal), Suit);
            m.Box(new Vector3(0f, 0.26f, 0f), new Vector3(0.60f, 0.12f, kal + 0.03f), Suit);
            m.Box(new Vector3(0f, -0.30f, 0f), new Vector3(0.52f, 0.20f, kal), Suit);

            // Gomlek ve papyon: yalnizca ON yuzde, yakanin arasinda.
            m.Box(new Vector3(0f, 0.02f, on), new Vector3(0.18f, 0.46f, 0.05f), Shirt);
            m.Box(new Vector3(0f, 0.21f, on + 0.02f),
                  new Vector3(0.15f, 0.07f, 0.05f), Bow);

            Place(m, kok, torso, olcek, mat, block);
        }

        /// <summary>
        /// ELDIVEN: iki kolun ucunda birer kaf.
        ///
        /// Kol kemiginin KENDI ekseni pakete gore degisebiliyor; bu
        /// yuzden eldiven kemigin ucuna degil, kemigin DUNYA konumundan
        /// asagi dogru (figurun boyuna oranli) bir noktaya konuyor.
        /// Kol asagi sarkik durdugunda el orada.
        /// </summary>
        private static void Gloves(Transform sol, Transform sag, float boy,
                                   Material mat, MaterialPropertyBlock block)
        {
            Glove1(sol, boy, mat, block);
            Glove1(sag, boy, mat, block);
        }

        private static void Glove1(Transform kol, float boy, Material mat,
                                   MaterialPropertyBlock block)
        {
            if (kol == null)
            {
                Debug.LogWarning("Wardrobe: iskelette arm-left/arm-right yok - "
                                 + "bulasikci eldiveni takilamadi");
                return;
            }

            Modeler m = new Modeler();
            m.Prism(8, 0.50f, 0.46f, 0.62f, Vector3.zero, Quaternion.identity, Glove);
            m.Prism(8, 0.54f, 0.54f, 0.16f, new Vector3(0f, 0.60f, 0f),
                    Quaternion.identity, Glove);        // kaf agzi

            GameObject go = m.Build(kol, GloveName, mat, block);
            Scale(go.transform, kol, boy * 0.13f);
            go.transform.rotation = kol.rotation;
            go.transform.position = kol.position - Vector3.up * (boy * 0.20f);
        }

        // =====================================================================
        /// <summary>Govde kiyafetini yerine koyar: olcek, yon, konum.</summary>
        private static void Place(Modeler m, Transform kok, Transform torso,
                                  float olcek, Material mat,
                                  MaterialPropertyBlock block)
        {
            GameObject go = m.Build(torso, BodyName, mat, block);
            Scale(go.transform, torso, olcek);

            // KIYAFET GOVDENIN MERKEZINDE.
            //
            // Once one dogru kaydiriliyordu (figurun onune yapisan bir
            // levha); kabuk olunca merkez dogru yer. Yon yine KOKTEN:
            // kemigin kendi ekseni pakete gore degisebiliyor.
            go.transform.rotation = kok.rotation;
            go.transform.position = torso.position;
        }

        /// <summary>Dunya olcegi k olacak sekilde yerel olcek yazar.</summary>
        private static void Scale(Transform t, Transform parent, float k)
        {
            Vector3 ls = parent.lossyScale;
            t.localScale = new Vector3(
                k / Mathf.Max(0.0001f, ls.x),
                k / Mathf.Max(0.0001f, ls.y),
                k / Mathf.Max(0.0001f, ls.z));
        }

        /// <summary>Bir kemigi adiyla bulur (buyuk-kucuk harf onemsiz).</summary>
        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == name || n == name + "_skel" || n.StartsWith(name + "."))
                    return t;
            }
            return null;
        }

        private static void Strip(Transform parent, string name)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform c = parent.GetChild(i);
                if (c.name != name) continue;
                if (Application.isPlaying) Object.Destroy(c.gameObject);
                else Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
