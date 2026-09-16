using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// AN OBJECT THAT OWNS ITS OWN MESH.
    ///
    /// Modeler produces a `new Mesh()` on every call. A Mesh is a
    /// UnityEngine.Object and it DOES NOT FOLLOW the GameObject when that
    /// is destroyed: a mesh held through MeshFilter.sharedMesh is orphaned
    /// and stays in memory until the scene is unloaded.
    ///
    /// The worst offender is the clothing: every time the crew's make-up
    /// changes ALL the staff are dressed again and three or four meshes
    /// are created per person. Eleven staff and a sixty-day campaign =
    /// hundreds of hires / changes of dishwashing duty = thousands of
    /// orphaned meshes. The same mistake was made one floor below
    /// (CookRoutine's pan material) and fixed there; when Modeler arrived
    /// it repeated itself one floor up.
    ///
    /// Why the fix belongs HERE: if ownership sits on the object itself,
    /// every caller does not have to remember separately who is going to
    /// destroy the mesh. Clear(), Strip(), a scene change, a prefab being
    /// deleted - they all go down the same path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh Mesh;

        /// <summary>
        /// Is the application QUITTING?
        ///
        /// On quit Unity is unloading every asset anyway, and calling Destroy
        /// by hand at that moment makes the process crash in the worst case:
        /// the tour's first run came back with 0xC0000005 and the stack was
        /// entirely inside the shutdown cleanup.
        ///
        /// Ownership is meaningful DURING PLAY - that is where the leak
        /// happens. At shutdown there is nothing to do.
        /// </summary>
        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Watch()
        {
            _quitting = false;
            Application.quitting += () => { _quitting = true; };
        }

        private void OnDestroy()
        {
            if (_quitting || Mesh == null) return;
            if (Application.isPlaying) Destroy(Mesh);
            else DestroyImmediate(Mesh);
            Mesh = null;
        }
    }
}
