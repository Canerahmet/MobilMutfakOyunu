using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A marker that tells the camera a room has been touched.
    ///
    /// In its own file, because Unity expects a MonoBehaviour to carry the
    /// same name as its file; putting two behaviours in one file comes
    /// back as "class not found" the day someone wants to add it to the
    /// scene by hand.
    /// </summary>
    public sealed class RoomTouch : MonoBehaviour
    {
        public int RoomIndex;
    }
}
