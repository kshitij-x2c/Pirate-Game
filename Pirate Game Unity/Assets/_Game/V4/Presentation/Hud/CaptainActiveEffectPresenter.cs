using RoguePirates.V4.Views;
using TMPro;
using UnityEngine;

namespace RoguePirates.V4.Presentation.Hud
{
    /// <summary>
    /// What a Captain's active did (M9 playtest: the actives were hard to notice): a banner under the timer with the
    /// active's name and what it does, fading out after about two seconds, and a ring on the water around the ship in the
    /// Captain's colour for as long as the active's effect lasts (a short flash for an instant one like All Guns!).
    /// Reads only the Captain and ship views.
    /// </summary>
    public sealed class CaptainActiveEffectPresenter : MonoBehaviour
    {
        const float BannerSeconds = 2.2f, FadeSeconds = 0.5f;
        const float RingRadius = 3.6f, RingHeight = 0.25f;
        const int RingPoints = 48;

        IRunViews _views;
        RectTransform _banner;
        TextMeshProUGUI _title, _summary;
        LineRenderer _ring;
        int _seenUses;
        float _bannerTimer;

        /// <summary>For tests and the playable check.</summary>
        public bool BannerShown => _banner != null && _banner.gameObject.activeSelf;
        public string BannerTitle => _title != null ? _title.text : string.Empty;
        public bool RingShown => _ring != null && _ring.enabled;

        public static CaptainActiveEffectPresenter Create(Transform hudCanvas)
        {
            var existing = hudCanvas.GetComponentInChildren<CaptainActiveEffectPresenter>(true);
            if (existing != null) return existing;
            var go = new GameObject("V4CaptainActiveEffect", typeof(RectTransform));
            go.transform.SetParent(hudCanvas, false);
            var presenter = go.AddComponent<CaptainActiveEffectPresenter>();
            presenter.Build((RectTransform)go.transform);
            return presenter;
        }

        void Build(RectTransform root)
        {
            // The presenter stays active; the banner is a child it shows and hides.
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var rt = (RectTransform)new GameObject("Banner", typeof(RectTransform)).transform;
            rt.SetParent(root, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -120f);
            rt.sizeDelta = new Vector2(760f, 96f);
            _banner = rt;
            _title = MakeText(rt, "Title", 44f, new Vector2(0f, 0f), FontStyles.Bold);
            _summary = MakeText(rt, "Summary", 20f, new Vector2(0f, -54f), FontStyles.Normal);
            rt.gameObject.SetActive(false);

            var ringGo = new GameObject("V4CaptainActiveRing");
            _ring = ringGo.AddComponent<LineRenderer>();
            ConfigureRing(_ring);
            _ring.material = new Material(Shader.Find("Sprites/Default"));
            _ownsRingMaterial = true;
            _ring.enabled = false;
        }

        /// <summary>The ring line's settings as the code makes it (the visual prefab builder saves it per Captain, with a
        /// Sprites/Default material asset, as Captains/&lt;captain&gt;/&lt;captain&gt;_ActiveRing.prefab).</summary>
        public static void ConfigureRing(LineRenderer ring)
        {
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingPoints;
            ring.widthMultiplier = 0.35f;
        }

        // V5 (visual prefabs): the Captain's own ring prefab (its width and material; its tuning sets the size, height,
        // pulse and can set the colour) replaces the code-built ring once the Captain is known.
        GameObject _ringPrefab;
        bool _ownsRingMaterial;

        /// <summary>The Captain's key from its legacy id (set by the presentation root; the ring prefab is per Captain).</summary>
        public System.Func<int, string> CaptainKeyOf { get; set; }

        void UseCaptainRing(int captainId)
        {
            var key = CaptainKeyOf?.Invoke(captainId);
            var prefab = key != null ? RoguePirates.V4.Presentation.Visuals.VisualPick.Get(key, RoguePirates.V4.Presentation.Visuals.Visual.ActiveRing) : null;
            if (prefab == null || prefab == _ringPrefab || prefab.GetComponent<LineRenderer>() == null) return;
            var old = _ring;
            var go = Instantiate(prefab);
            go.name = "V4CaptainActiveRing";
            _ring = go.GetComponent<LineRenderer>();
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.positionCount = RingPoints;
            _ring.enabled = false;
            _ringPrefab = prefab;
            if (old != null)
            {
                if (_ownsRingMaterial && old.sharedMaterial != null) Destroy(old.sharedMaterial);
                Destroy(old.gameObject);
            }
            _ownsRingMaterial = false;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, float size, Vector2 offset, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(0f, size * 1.3f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(10, 20, 30, 220);
            return text;
        }

        public void Bind(IRunViews views)
        {
            _views = views;
            _seenUses = views?.Captain != null ? views.Captain.ActiveUses : 0;
            if (views?.Captain != null) UseCaptainRing(views.Captain.CaptainId);
        }

        void LateUpdate()
        {
            var captain = _views?.Captain;
            if (captain == null) return;

            if (captain.ActiveUses != _seenUses)
            {
                _seenUses = captain.ActiveUses;
                ShowBanner(captain);
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
                var alpha = Mathf.Clamp01(_bannerTimer / FadeSeconds);
                _title.alpha = alpha;
                _summary.alpha = alpha;
                if (_bannerTimer <= 0f) _banner.gameObject.SetActive(false);
            }

            UpdateRing(captain);
        }

        void ShowBanner(ICaptainView captain)
        {
            _title.text = captain.ActiveName.ToUpperInvariant();
            _title.color = Bright(captain.ThemeColor);
            _summary.text = StripName(captain.ActiveSummary, captain.ActiveName);
            _summary.color = Color.white;
            _bannerTimer = BannerSeconds;
            _banner.gameObject.SetActive(true);
        }

        /// <summary>Several theme colours are dark (Rook's navy on a blue sea): lift them halfway to white so they read.</summary>
        static Color Bright(Color theme)
        {
            var c = Color.Lerp(theme, Color.white, 0.5f);
            c.a = 1f;
            return c;
        }

        /// <summary>The Captain summary starts with the active's name and cooldown ("Brace! (20s) — 60% DR…"); keep what it does.</summary>
        static string StripName(string summary, string name)
        {
            if (string.IsNullOrEmpty(summary)) return string.Empty;
            var dash = summary.IndexOf('—');
            return dash >= 0 && dash + 1 < summary.Length ? summary.Substring(dash + 1).Trim() : summary;
        }

        void UpdateRing(ICaptainView captain)
        {
            var ship = _views.Ship;
            var remaining = captain.ActiveEffectRemaining;
            var run = _views.Run;
            var show = remaining > 0f && ship != null && ship.Exists && (run == null || !run.IsRunEnded);
            if (_ring.enabled != show) _ring.enabled = show;
            if (!show) return;
            var duration = Mathf.Max(0.01f, captain.ActiveEffectDuration);
            var p = _ringPrefab; // V5: its tuning (1 and 0 without one)
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f * RoguePirates.V4.Presentation.Visuals.VisualTuning.PulseSpeed(p));
            var color = RoguePirates.V4.Presentation.Visuals.VisualTuning.Colour(p, Bright(captain.ThemeColor));
            color.a *= Mathf.Lerp(0.55f, 1f, pulse) * Mathf.Clamp01(remaining / Mathf.Min(0.6f, duration) ); // Bright() is opaque
            _ring.startColor = _ring.endColor = color;
            var center = ship.Position;
            var radius = RingRadius * RoguePirates.V4.Presentation.Visuals.VisualTuning.Scale(p) * (1f + 0.05f * RoguePirates.V4.Presentation.Visuals.VisualTuning.Pulse(p) * pulse);
            var height = RingHeight + RoguePirates.V4.Presentation.Visuals.VisualTuning.Height(p);
            for (var i = 0; i < RingPoints; i++)
            {
                var a = i * Mathf.PI * 2f / RingPoints;
                _ring.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, height, center.z + Mathf.Sin(a) * radius));
            }
        }

        void OnDestroy()
        {
            if (_ring == null) return;
            if (_ownsRingMaterial && _ring.sharedMaterial != null) Destroy(_ring.sharedMaterial);
            Destroy(_ring.gameObject);
        }
    }
}
