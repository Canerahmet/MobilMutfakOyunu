using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Unity'nin GELISMIS METIN URETICISINI (ATG) acar.
    ///
    /// NEDEN GEREKLI: Arapca harfleri BIRLESIR. "مرحبا" yazan bir metin,
    /// olcunlu ureticide harflerin ayri ayri ve soldan saga dizilmis
    /// halini gosterir - okunmaz. Birlestirme (init/medi/fina), iki yonlu
    /// siralama ve satir sonu yalnizca ATG'de var. Oyunda bes dil var ve
    /// biri Arapca; bu ayar olmadan o dil ekranda cop.
    ///
    /// NEDEN BURADA: ayar bir ONAY KUTUSU (Edit > Project Settings >
    /// UI Toolkit > Enable Advanced Text Generator) ve
    /// ProjectSettings/UIToolkitProjectSettings.asset dosyasinda duruyor.
    /// Elle acilan bir kutu, depoyu yeni klonlayan bir makinede kapali
    /// olur ve Arapca SESSIZCE bozulur - yapinin kendisi aciyor.
    ///
    /// NEDEN YANSIMA: UIToolkitProjectSettings sinifi Unity'nin ic
    /// (internal) sinifi, genel bir API'si yok. Yansima kirilgan, o
    /// yuzden BULAMAZSA SESSIZ KALMIYOR - uyari basiyor ve yapi kutugune
    /// dusuyor.
    /// </summary>
    public static class AdvancedText
    {
        private const string TypeName =
            "UnityEditor.UIElements.UIToolkitProjectSettings, UnityEditor.UIElementsModule";

        [MenuItem("Lokanta/Gelismis metin ureticisini ac")]
        public static void Enable()
        {
            if (Set(true)) Debug.Log("Gelismis metin ureticisi ACIK.");
        }

        /// <summary>
        /// Ayari kurar. Basarisizsa uyari basar ve false doner.
        /// </summary>
        public static bool Set(bool value)
        {
            System.Type t = System.Type.GetType(TypeName);
            if (t == null)
            {
                Debug.LogWarning(
                    "UIToolkitProjectSettings bulunamadi - gelismis metin " +
                    "ureticisi ayarlanamadi. Arapca yanlis cizilebilir.");
                return false;
            }

            PropertyInfo p = t.GetProperty(
                "enableAdvancedText",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (p == null || !p.CanWrite)
            {
                Debug.LogWarning(
                    "UIToolkitProjectSettings.enableAdvancedText yazilamadi - " +
                    "Arapca yanlis cizilebilir.");
                return false;
            }

            p.SetValue(null, value);

            // OKUYARAK DOGRULA. Yansimayla yazmak sessizce hicbir sey
            // yapmis olabilir; yazdigini geri okumayan bir ayar, hic
            // ayarlanmamis olanla ekranda ayni gorunur.
            object back = p.GetValue(null);
            if (!(back is bool) || (bool)back != value)
            {
                Debug.LogWarning(
                    "Gelismis metin ureticisi ayari geri okundugunda tutmadi.");
                return false;
            }
            return true;
        }
    }
}
