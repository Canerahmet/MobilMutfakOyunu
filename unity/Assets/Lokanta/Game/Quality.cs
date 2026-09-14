using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lokanta.Game
{
    /// <summary>
    /// YAKINLASTIRINCA COZUNURLUK YUKSELIYOR.
    ///
    /// Oyun 0,8 render olceginde ciziliyor: dusuk seviye bir Adreno'da
    /// piksel sayisini %36 dusuruyor ve varsayilan cerceveden
    /// bakildiginda fark edilmiyor - restoran ekranin yarisi kadar ve
    /// her sey duz renkli.
    ///
    /// Oyuncu iki parmakla yaklastirinca ayni yumusaklik GORUNUR
    /// oluyor: 2,2 kat buyutmede 0,8 olcek, kenarlarda basamaklanma
    /// demek. Ve yaklasmis bir kamerada ekranda cok daha az sey var,
    /// yani tam cozunurlugun bedelini odeyecek butce de var.
    ///
    /// Esik 0,85: kucuk bir kaydirmada acilip kapanmasin diye
    /// varsayilandan belirgin sekilde uzak.
    ///
    /// NEDEN BURADA: URP varligi projede TEK bir dosya (LokantaURP.asset)
    /// ve calisma zamaninda degistirilen deger DISKE YAZILMIYOR - yani
    /// oyun kapandiginda ayar geri geliyor. Yine de degeri baslangicta
    /// saklayip geri koyuyoruz: editorde oynanirsa varlik kirlenmesin.
    /// </summary>
    public static class Quality
    {
        private const float ZoomThreshold = 0.85f;
        private const float SharpScale = 1.0f;

        private static float _default = -1f;
        private static bool _sharp;

        /// <summary>Yakinlastirma oranina gore render olcegini ayarlar.</summary>
        public static void ApplyZoom(float zoom)
        {
            UniversalRenderPipelineAsset urp =
                GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;

            if (_default < 0f) _default = urp.renderScale;

            bool istenen = zoom < ZoomThreshold;
            if (istenen == _sharp) return;
            _sharp = istenen;

            urp.renderScale = istenen ? SharpScale : _default;
        }

        /// <summary>Varsayilana dondurur. Oyundan cikarken.</summary>
        public static void Restore()
        {
            if (_default < 0f) return;
            UniversalRenderPipelineAsset urp =
                GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.renderScale = _default;
            _sharp = false;
        }
    }
}
