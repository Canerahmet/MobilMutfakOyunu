using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// ACILIP KAPANAN BIR KAPI.
    ///
    /// Neden var: duvarlar odalari ayirdi ama figurler onlarin icinden
    /// geciyordu - bir duvarin ne ise yaradigi ancak bir kapisi varken
    /// belli oluyor. Kapi ayrica salonun en cok tekrar eden hareketi:
    /// garson gun boyunca onlarca kez ayni esikten geciyor.
    ///
    /// MENTESE KANADIN KENARINDA. Modelin kendi pivotundan dondurmek
    /// kanadi duvarin icine gomerdi (firin kapaginda tam bu oldu, bkz.
    /// Appliance): ayri bir mentese nesnesi konuyor ve kanat ona
    /// baglaniyor. Gercek bir kapi da oradan doner.
    ///
    /// CARPISAN YOK. Dokunma hedefi oda zemini (docs/31 olcumu); kapiya
    /// carpan bir isin oda secimini bozardi. Figurler de kapiya
    /// carpmiyor - yol koridordan geciyor, kapi yalnizca goruntu.
    /// </summary>
    public sealed class Door : MonoBehaviour
    {
        /// <summary>Acilma acisi. Iceriye dogru aciliyor.</summary>
        private const float OpenAngle = 88f;

        /// <summary>Acilip kapanma suresi (sn). Kapi hafif, hizli aciliyor.</summary>
        private const float OpenSeconds = 0.28f;

        /// <summary>
        /// Kapinin "birini gordugu" mesafe (m).
        ///
        /// 1,10: figur yurume hizi 1,15 m/sn, kapi 0,28 sn'de aciliyor -
        /// yani figur kapiya varmadan yaklasik 0,8 m once kanat tam
        /// acik oluyor. Daha kisa bir mesafede figur kapanmakta olan
        /// kanadin icinden gecerdi.
        /// </summary>
        public const float Sense = 1.10f;

        private Transform _hinge;
        private float _t;          // 0 kapali, 1 acik
        private bool _open;

        /// <summary>Kapinin dunya (yerel) konumu: mesafe buradan olculuyor.</summary>
        public Vector3 Spot { get; private set; }

        /// <summary>Su an acik mi. Turun sorabilmesi icin.</summary>
        public bool IsOpen { get { return _t > 0.5f; } }

        /// <summary>Acilma orani, 0-1. Turun sorabilmesi icin.</summary>
        public float Openness { get { return _t; } }

        // =====================================================================
        /// <summary>
        /// Kapiyi kurar. spot: kapinin yeri; yaw: duvarin yonu;
        /// width: bosluk genisligi; height: kanat yuksekligi.
        /// </summary>
        public static Door Create(Transform parent, Vector3 spot, float yaw,
                                  float width, float height, Material mat)
        {
            GameObject kok = new GameObject("Kapi");
            kok.transform.SetParent(parent, false);
            kok.transform.localPosition = spot;
            kok.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // MENTESE bosugun bir KENARINDA, ortasinda degil.
            GameObject mentese = new GameObject("Mentese");
            mentese.transform.SetParent(kok.transform, false);
            mentese.transform.localPosition = new Vector3(0f, 0f, -width * 0.5f);

            GameObject kanat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kanat.name = "Kanat";
            kanat.transform.SetParent(mentese.transform, false);
            // Kanat mentesenin ONUNDE: donunce ucu disari savruluyor.
            kanat.transform.localPosition = new Vector3(0f, height * 0.5f, width * 0.5f);
            kanat.transform.localScale = new Vector3(0.05f, height, width * 0.94f);

            Collider col = kanat.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = kanat.GetComponent<Renderer>();
            ren.sharedMaterial = mat;
            // Saydam bir kanadin golgesi OPAK duser: URP golge gecisi
            // alfayi okumuyor (duvarlarda da ayni sebeple kapali).
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;

            Door d = kok.AddComponent<Door>();
            d._hinge = mentese.transform;
            d.Spot = spot;
            return d;
        }

        /// <summary>Birileri yaklasti mi. Her karede degil, DEGISINCE.</summary>
        public void SetOpen(bool on)
        {
            _open = on;
        }

        private void Update()
        {
            if (_hinge == null) return;

            float hedef = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, hedef)) return;

            _t = Mathf.MoveTowards(_t, hedef, Time.deltaTime / OpenSeconds);
            // Yavaslayarak: kanat sona dogru oturuyor.
            float k = 1f - (1f - _t) * (1f - _t);
            _hinge.localRotation = Quaternion.Euler(0f, -OpenAngle * k, 0f);
        }
    }
}
