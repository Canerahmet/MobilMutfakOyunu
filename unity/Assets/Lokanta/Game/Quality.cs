using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lokanta.Game
{
    /// <summary>
    /// THE RESOLUTION RISES WHEN YOU ZOOM IN.
    ///
    /// The game is drawn at 0.8 render scale: on a low-end Adreno that
    /// cuts the pixel count by 36%, and from the default framing nobody
    /// notices - the restaurant is half the screen and everything is flat
    /// colour.
    ///
    /// When the player pinches in, that same softness becomes VISIBLE: at
    /// 2.2x magnification a 0.8 scale means stair-stepping along the
    /// edges. And a camera that has zoomed in has far less on screen, so
    /// there is also the budget to pay for full resolution.
    ///
    /// The threshold is 0.85: comfortably clear of the default so that it
    /// does not flick on and off on a small drag.
    ///
    /// WHY HERE: the URP asset is a SINGLE file in the project
    /// (LokantaURP.asset) and a value changed at runtime IS NOT WRITTEN TO
    /// DISK - so the setting comes back when the game closes. We still
    /// keep the starting value and put it back: so that playing in the
    /// editor does not dirty the asset.
    /// </summary>
    public static class Quality
    {
        private const float ZoomThreshold = 0.85f;
        private const float SharpScale = 1.0f;

        private static float _default = -1f;
        private static bool _sharp;

        /// <summary>Sets the render scale from the zoom ratio.</summary>
        public static void ApplyZoom(float zoom)
        {
            UniversalRenderPipelineAsset urp =
                GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;

            if (_default < 0f) _default = urp.renderScale;

            bool wanted = zoom < ZoomThreshold;
            if (wanted == _sharp) return;
            _sharp = wanted;

            urp.renderScale = wanted ? SharpScale : _default;
        }

        /// <summary>Puts the default back. On the way out of the game.</summary>
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
