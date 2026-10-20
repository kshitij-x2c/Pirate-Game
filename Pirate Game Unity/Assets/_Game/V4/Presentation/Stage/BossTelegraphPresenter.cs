using System;
using RoguePirates.V4.Config;
using RoguePirates.V4.Stage;
using UnityEngine;

namespace RoguePirates.V4.Presentation.Stage
{
    /// <summary>
    /// The boss's marked circle (M7): shown while a marked bombardment or shell slam is telegraphed, as V3's boss
    /// director drew it (a flat primitive cylinder without its collider, URP Lit else Standard, orange at alpha 0.55,
    /// scaled radius × 2 by the thickness, just above the marked point). V4: drawn from <see cref="IBossView"/> by this
    /// presenter instead of by the director; the colour, height and thickness come from <see cref="BossConfig"/>
    /// (V3's values by default) and follow live edits.
    /// </summary>
    public sealed class BossTelegraphPresenter : MonoBehaviour
    {
        static readonly Color V3Color = new Color(1f, 0.35f, 0.1f, 0.55f);
        const float V3Height = 0.08f;
        const float V3Thickness = 0.12f;

        IBossView _view;
        Func<BossConfig> _config;
        GameObject _circle;
        Material _material;

        public void Bind(IBossView view, Func<BossConfig> config)
        {
            _view = view;
            _config = config;
        }

        /// <summary>The circle object (null until first shown).</summary>
        public GameObject Circle => _circle;

        void LateUpdate() => Refresh();

        /// <summary>Shows, poses or hides the circle from the view (called every frame; public for tests).</summary>
        public void Refresh()
        {
            if (_view == null || !_view.MarkedTelegraphVisible)
            {
                if (_circle != null) _circle.SetActive(false);
                return;
            }

            var config = _config != null ? _config() : null;
            EnsureCircle();
            // V5 (visual prefabs): Bosses/boss/boss_MarkedTelegraph's tuning (colour, height, size, thickness as width).
            var color = Visuals.VisualTuning.Colour(_prefab, config != null ? config.telegraphColor : V3Color);
            if (_material.color != color) _material.color = color;
            var height = (config != null ? config.telegraphHeight : V3Height) + Visuals.VisualTuning.Height(_prefab);
            var thickness = (config != null ? config.telegraphThickness : V3Thickness) * Visuals.VisualTuning.Width(_prefab);
            var radius = _view.MarkedTelegraphRadius * Visuals.VisualTuning.Scale(_prefab);
            _circle.transform.position = _view.MarkedTelegraphCenter + Vector3.up * height;
            _circle.transform.localScale = new Vector3(radius * 2f, thickness, radius * 2f);
            _circle.SetActive(true);
        }

        GameObject _prefab;

        void EnsureCircle()
        {
            if (_circle != null) return;
            _prefab = Visuals.VisualPick.Get(Visuals.VisualIds.Boss, Visuals.Visual.MarkedTelegraph);
            var config = _config != null ? _config() : null;
            if (_prefab != null)
            {
                // V5 (visual prefabs): an instance of the prefab, with its own copy of the prefab's material (the code
                // recolours it from the boss config).
                _circle = Instantiate(_prefab);
                _circle.name = "BarnacleKingMarkedTelegraph";
                var renderer = _circle.GetComponent<Renderer>();
                _material = new Material(renderer.sharedMaterial);
                renderer.sharedMaterial = _material;
            }
            else _circle = BuildCircle(out _material);
            _material.color = Visuals.VisualTuning.Colour(_prefab, config != null ? config.telegraphColor : V3Color);
        }

        /// <summary>V3's marked circle: a collider-less cylinder with its own URP Lit (else Standard) material in V3's orange.
        /// The visual prefab builder saves it, its material an asset, as Bosses/boss/boss_MarkedTelegraph.prefab.</summary>
        public static GameObject BuildCircle(out Material material)
        {
            var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.name = "BarnacleKingMarkedTelegraph";
            var collider = circle.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = V3Color };
            circle.GetComponent<Renderer>().sharedMaterial = material;
            return circle;
        }

        void OnDestroy()
        {
            if (_circle != null) DestroyOwned(_circle);
            if (_material != null) DestroyOwned(_material);
            _circle = null;
            _material = null;
        }

        static void DestroyOwned(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
