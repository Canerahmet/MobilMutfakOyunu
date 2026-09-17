using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// The touch target of a room's badge, and the table it stands for.
    ///
    /// In its own file for the same reason as [[RoomTouch]] and
    /// [[TableTouch]]: Unity expects a MonoBehaviour to carry the same name
    /// as its file.
    ///
    /// WHY THE TABLE INDEX LIVES HERE. The badge shows the WORST table in its
    /// room, and which table that is changes from second to second. Rather
    /// than have CameraRig work it out again at the moment of the touch - a
    /// second implementation of "worst", which would drift from the first -
    /// RestaurantView writes the answer here in the same pass that draws the
    /// badge. What the player touches is then exactly what they were looking
    /// at, and not a table that became the worst one between the frame and
    /// the finger.
    ///
    /// -1 means the room has nothing that needs attention; the touch then
    /// falls through to zooming into the room, which is what a touch on the
    /// room itself does.
    /// </summary>
    public sealed class RoomBadgeTouch : MonoBehaviour
    {
        public int RoomIndex = -1;
        public int TableIndex = -1;
    }
}
