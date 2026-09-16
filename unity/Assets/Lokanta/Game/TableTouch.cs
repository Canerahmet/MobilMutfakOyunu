using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A marker that a table has been touched, and the touch target.
    ///
    /// In its own file for the same reason as [[RoomTouch]]: Unity expects
    /// a MonoBehaviour to carry the same name as its file.
    ///
    /// WHY IT EXISTS: until now the interventions picked their target
    /// THEMSELVES - "offer tea" always went to MostImpatientParty. So the
    /// player's only decision during service was "now or later"; the game
    /// answered the WHO. The whole mechanic of being the owner had shrunk
    /// to a single timing button.
    ///
    /// It also gives the second camera step (zooming into a room) a job:
    /// zooming in is not DECORATION, it means being able to pick a table.
    /// It is still playable without zooming in - if no choice is made the
    /// old behaviour applies, that is, the table with the least patience
    /// left.
    /// </summary>
    public sealed class TableTouch : MonoBehaviour
    {
        public int TableIndex;

        /// <summary>
        /// The RADIUS of the touch target (m). The table is 0.88 m across
        /// and a collider only that big drops to about 67 dp when the room
        /// is zoomed into - touchable, but tight. 1.30 m, which takes the
        /// chairs in as well, makes the table SET the target, and that is
        /// what the player already sees as one thing.
        /// </summary>
        private const float Radius = 0.65f;
        private const float Height = 1.10f;

        public static TableTouch Attach(Transform table, int index)
        {
            // The touch volume is a SEPARATE child: the table's prefab part
            // has a collider of its own and enlarging that would have enlarged
            // the visual scale too.
            GameObject go = new GameObject("Touch");
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
