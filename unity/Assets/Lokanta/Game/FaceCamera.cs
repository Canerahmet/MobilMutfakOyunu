using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Levhayi kameraya dondurur.
    ///
    /// NEDEN BIR BILESEN GEREKTI: fenerin halesi kurulusta
    /// `CameraFit.Rotation` ile SABIT bir aciya kuruluyordu ve
    /// yanindaki yorum "oyunun kamerasinin acisi sabit, yalnizca konumu
    /// degisiyor" diyordu. Bu artik dogru degil - CameraRig iki
    /// parmakla +-35 dereceye kadar `_yawOffset` uyguluyor
    /// (LookRotation = Euler(0, _yawOffset, 0) * CameraFit.Rotation).
    ///
    /// Sonucu sessiz: 35 derecede hale belirgin sekilde inceliyor,
    /// fener govdesi ortasini kapatiyor ve oyuncu yalnizca "gece biraz
    /// sonuk" goruyor. Hicbir hata basilmiyor - tam olarak halenin bir
    /// kez HIC cizilmedigi (arka yuz ayiklanmasi) hatanin yumusak hali.
    ///
    /// Unity Quad'inin yuzu -Z'ye bakiyor, o yuzden 180 derece
    /// cevriliyor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FaceCamera : MonoBehaviour
    {
        private Camera _kamera;

        private void LateUpdate()
        {
            if (_kamera == null)
            {
                _kamera = Camera.main;
                if (_kamera == null) return;
            }
            transform.rotation = _kamera.transform.rotation
                                 * Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
