using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Turns the board to face the camera.
    ///
    /// WHY A COMPONENT WAS NEEDED: the lantern's halo was built at a FIXED
    /// angle at setup time with `CameraFit.Rotation`, and the comment next
    /// to it said "the game's camera angle is fixed, only its position
    /// changes". That is not true any more - CameraRig applies a
    /// `_yawOffset` of up to +-35 degrees with two fingers
    /// (LookRotation = Euler(0, _yawOffset, 0) * CameraFit.Rotation).
    ///
    /// The consequence is silent: at 35 degrees the halo visibly narrows,
    /// the lantern body covers its middle, and the player only sees "the
    /// night is a bit dull". Nothing is logged - precisely the soft form
    /// of the bug where the halo was once NOT drawn AT ALL (back-face
    /// culling).
    ///
    /// Unity's Quad faces -Z, which is why it is turned by 180 degrees.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FaceCamera : MonoBehaviour
    {
        private Camera _camera;

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }
            transform.rotation = _camera.transform.rotation
                                 * Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
