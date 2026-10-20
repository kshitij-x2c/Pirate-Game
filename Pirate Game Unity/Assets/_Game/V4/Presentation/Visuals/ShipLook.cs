using UnityEngine;

namespace RoguePirates.V4.Presentation.Visuals
{
    /// <summary>
    /// V5 (visual prefabs): a Captain's look on the player ship. Each Captain has a prefab variant of the ship model
    /// (Captains/&lt;captain&gt;/&lt;captain&gt;_ShipLook.prefab, with that Captain's sail and flag); at the run's start its
    /// renderers' materials are put on the ship's matching renderers (matched by place in the hierarchy), in place of the
    /// two-material swap the HUD settings did. Only materials are taken: the ship's model stays the scene's, because its
    /// hull carries the ship's collider (gameplay). The 1v1 opponent's copy takes the player's look the same way.
    /// </summary>
    public static class ShipLook
    {
        /// <summary>The model under <paramref name="root"/> that <paramref name="look"/> dresses: the first transform (breadth
        /// first) whose children have the look's children's names, in order. Null when there is none.</summary>
        public static Transform FindModel(Transform root, GameObject look)
        {
            if (root == null || look == null) return null;
            var queue = new System.Collections.Generic.Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (SameChildren(t, look.transform)) return t;
                for (var i = 0; i < t.childCount; i++) queue.Enqueue(t.GetChild(i));
            }
            return null;
        }

        static bool SameChildren(Transform a, Transform b)
        {
            if (a.childCount != b.childCount || a.childCount == 0) return false;
            for (var i = 0; i < a.childCount; i++)
                if (a.GetChild(i).name != b.GetChild(i).name) return false;
            return true;
        }

        /// <summary>Puts the look's materials on the ship's model under <paramref name="shipRoot"/>; the number of renderers
        /// changed, or -1 when the model is not found (the caller keeps its old way).</summary>
        public static int Apply(Transform shipRoot, GameObject look)
        {
            var model = FindModel(shipRoot, look);
            return model == null ? -1 : CopyMaterials(look.transform, model);
        }

        /// <summary>Every renderer's materials from <paramref name="from"/> onto the renderer at the same place under
        /// <paramref name="to"/> (same child index and name, same renderer type); the number changed.</summary>
        public static int CopyMaterials(Transform from, Transform to)
        {
            if (from == null || to == null) return 0;
            var changed = 0;
            var a = from.GetComponent<Renderer>();
            var b = to.GetComponent<Renderer>();
            if (a != null && b != null && a.GetType() == b.GetType() && !(a is ParticleSystemRenderer) && !Same(a.sharedMaterials, b.sharedMaterials))
            {
                b.sharedMaterials = a.sharedMaterials;
                changed++;
            }
            var n = Mathf.Min(from.childCount, to.childCount);
            for (var i = 0; i < n; i++)
            {
                var ca = from.GetChild(i);
                var cb = to.GetChild(i);
                if (ca.name == cb.name) changed += CopyMaterials(ca, cb);
            }
            return changed;
        }

        /// <summary>Shows or hides each part under <paramref name="to"/> as the part at the same place under
        /// <paramref name="from"/> is (the 1v1 opponent's copy takes the player's ship as it is: health bar and aim parts
        /// hidden, the wake shown once the ship has moved).</summary>
        public static void CopyShown(Transform from, Transform to)
        {
            if (from == null || to == null) return;
            var n = Mathf.Min(from.childCount, to.childCount);
            for (var i = 0; i < n; i++)
            {
                var ca = from.GetChild(i);
                var cb = to.GetChild(i);
                if (ca.name != cb.name) continue;
                if (cb.gameObject.activeSelf != ca.gameObject.activeSelf) cb.gameObject.SetActive(ca.gameObject.activeSelf);
                CopyShown(ca, cb);
            }
        }

        static bool Same(Material[] a, Material[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
