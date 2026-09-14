using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Bir odaya dokunuldugunu kameraya bildiren isaret.
    ///
    /// Ayri dosyada, cunku Unity bir MonoBehaviour'un dosya adiyla ayni
    /// adi tasimasini bekliyor; ayni dosyaya iki davranis koymak, ileride
    /// sahneye elle eklenmek istendiginde "sinif bulunamadi" diye doner.
    /// </summary>
    public sealed class RoomTouch : MonoBehaviour
    {
        public int RoomIndex;
    }
}
