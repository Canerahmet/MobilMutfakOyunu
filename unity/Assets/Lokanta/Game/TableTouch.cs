using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Bir masaya dokunuldugunu bildiren isaret ve dokunma hedefi.
    ///
    /// Ayri dosyada, [[RoomTouch]] ile ayni sebepten: Unity bir
    /// MonoBehaviour'un dosya adiyla ayni adi tasimasini bekliyor.
    ///
    /// NEDEN VAR: mudahaleler bugune kadar hedefi KENDILERI seciyordu -
    /// "Cay ikram" her zaman MostImpatientParty'ye gidiyordu. Yani
    /// oyuncunun servis sirasindaki tek karari "simdi mi, sonra mi"
    /// idi; KIME sorusunu oyun cevapliyordu. Patron olmanin butun
    /// mekanigi tek bir zamanlama dugmesine inmisti.
    ///
    /// Ikinci kamera kademesinin (odaya yaklasma) da boylece bir isi
    /// oluyor: yakinlasmak SUSLEME degil, masa secebilmek demek.
    /// Yaklasmadan da oynanabiliyor - secim yapilmazsa eski davranis,
    /// yani sabri en az kalan masa.
    /// </summary>
    public sealed class TableTouch : MonoBehaviour
    {
        public int TableIndex;

        /// <summary>
        /// Dokunma hedefinin YARIÇAPI (m). Masanin capi 0,88 m ve
        /// yalnizca o kadarlik bir carpisan, odaya yaklasildiginda
        /// yaklasik 67 dp'ye dusuyor - dokunulabilir ama dar.
        /// Sandalyeleri de iceren 1,30 m, masa TAKIMINI hedef yapiyor
        /// ve oyuncunun zaten bir butun olarak gordugu sey o.
        /// </summary>
        private const float Radius = 0.65f;
        private const float Height = 1.10f;

        public static TableTouch Attach(Transform table, int index)
        {
            // Dokunma hacmi AYRI bir cocukta: masa on tanimli parcasinin
            // kendi carpisani var ve onu buyutmek, gorsel olcegi de
            // buyuturdu.
            GameObject go = new GameObject("Dokunma");
            go.transform.SetParent(table, false);
            go.transform.localPosition = new Vector3(0f, Height * 0.5f, 0f);

            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            c.radius = Radius;
            c.height = Height;
            c.isTrigger = true;

            TableTouch t = go.AddComponent<TableTouch>();
            t.TableIndex = index;
            return t;
        }
    }
}
