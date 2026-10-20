using RoguePirates.V4.Views;
using UnityEngine;

namespace RoguePirates.V4.Presentation.World
{
    /// <summary>
    /// The storm's safe-region ring (M14.9b; port of V3's V3SaltwindStormRingVisual): a purple line on the storm's safe
    /// rectangle, shown from the storm's start until the next run, redrawn each frame from the world view (V3 refreshed it
    /// whenever the storm director ticked).
    /// </summary>
    public sealed class StormRingPresenter : MonoBehaviour
    {
        static readonly Color RingColor = new Color(0.35f, 0.08f, 0.55f, 0.72f);

        IWorldView _world;
        LineRenderer _ring;
        Material _material;

        public bool RingShown => _ring != null && _ring.gameObject.activeSelf;

        public void Bind(IWorldView world)
        {
            _world = world;
            if (_ring == null) _ring = CreateRing();
            _ring.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_ring == null) return;
            var visible = _world != null && _world.StormRingVisible;
            if (_ring.gameObject.activeSelf != visible) _ring.gameObject.SetActive(visible);
            if (!visible) return;
            _world.StormRingCorners(out var a, out var b, out var c, out var d);
            _ring.SetPosition(0, a);
            _ring.SetPosition(1, b);
            _ring.SetPosition(2, c);
            _ring.SetPosition(3, d);
        }

        LineRenderer CreateRing()
        {
            // V5 (visual prefabs): World/storm_ring/storm_ring_Ring (its colour, width and material are the prefab's; the
            // code sets only the four corners), else code-built as before.
            var prefab = RoguePirates.V4.Presentation.Visuals.VisualPick.Get(RoguePirates.V4.Presentation.Visuals.VisualIds.StormRing, RoguePirates.V4.Presentation.Visuals.Visual.Ring);
            if (prefab != null && prefab.GetComponent<LineRenderer>() != null)
            {
                var go = Instantiate(prefab, transform, false);
                go.name = "StormSafeRing";
                var own = go.GetComponent<LineRenderer>();
                own.useWorldSpace = true;
                own.loop = true;
                own.positionCount = 4;
                return own;
            }
            var child = new GameObject("StormSafeRing");
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            ConfigureLine(line);
            _material = new Material(Shader.Find("Sprites/Default"));
            line.material = _material;
            return line;
        }

        /// <summary>The ring line's settings as the code makes it (the visual prefab builder saves it, with a Sprites/Default
        /// material asset, as World/storm_ring/storm_ring_Ring.prefab).</summary>
        public static void ConfigureLine(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.5f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.startColor = RingColor;
            line.endColor = RingColor;
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
