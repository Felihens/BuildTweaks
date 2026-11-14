using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Snapping. The game snaps a piece you place by its snap points: children of the piece tagged "snappoint"
    // (Piece.GetSnapPoints). The ghost's point closest to a point of a piece nearby (within half a metre) is moved onto
    // it. Chests, furniture, the workbench and many more have no snap points, so they never snap.
    //
    // Here every hammer piece without snap points gets some when the game loads: the 8 corners of its box (from its
    // colliders) and the middle of its bottom and of its top. So a chest snaps beside another, on top of it, on the
    // corner of a floor, or centred on a beam. The game's own points stay as they are.
    // And the distance at which a point catches is SnapDistance instead of the game's half metre.
    // (The game already lets you pick which point of the ghost snaps: Q and E while building.)
    [HarmonyPatch]
    internal static class Snapping
    {
        private const string PointName = "BuildTweaks_snap";
        private static bool _done;

        [HarmonyPatch(typeof(ZNetScene), "Awake"), HarmonyPostfix]
        private static void AddPoints(ZNetScene __instance)
        {
            if (_done || !Plugin.SnapEverything.Value || __instance == null) return;
            _done = true;
            HashSet<string> skip = new HashSet<string>();
            foreach (string s in (Plugin.NoSnapPieces.Value ?? "").Split(','))
                if (s.Trim().Length > 0) skip.Add(s.Trim().ToLowerInvariant());
            int made = 0;
            foreach (GameObject prefab in __instance.m_prefabs)
            {
                if (prefab == null || skip.Contains(prefab.name.ToLowerInvariant())) continue;
                Piece piece = prefab.GetComponent<Piece>();
                if (piece == null || !Fits(prefab, piece)) continue;
                if (HasPoints(prefab.transform)) continue;
                Bounds b;
                if (!LocalBox(prefab.transform, out b)) continue;
                if (Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) < 0.25f) continue;   // too small to be worth it
                Vector3 mn = b.min, mx = b.max;
                for (int i = 0; i < 8; i++)
                    Add(prefab.transform, new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z));
                Add(prefab.transform, new Vector3(b.center.x, mn.y, b.center.z));
                Add(prefab.transform, new Vector3(b.center.x, mx.y, b.center.z));
                made++;
                Plugin.Debug("snap points added to " + prefab.name + " (box " + b.size.x.ToString("0.00") + " x " + b.size.y.ToString("0.00") + " x " + b.size.z.ToString("0.00") + ")");
            }
            Plugin.Log.LogInfo("snap points added to " + made + " pieces that had none");
        }

        // only building pieces placed with the hammer: no terrain tools, plants, boats or carts
        private static bool Fits(GameObject prefab, Piece piece)
        {
            if (piece.m_cultivatedGroundOnly || piece.m_waterPiece) return false;
            if (prefab.GetComponent<TerrainModifier>() != null || prefab.GetComponent<TerrainOp>() != null) return false;
            if (prefab.GetComponent<Plant>() != null || prefab.GetComponent<Pickable>() != null) return false;
            if (prefab.GetComponent<Ship>() != null || prefab.GetComponent<Vagon>() != null) return false;
            if (prefab.GetComponent<Character>() != null || prefab.GetComponent<ItemDrop>() != null) return false;
            return true;
        }

        private static bool HasPoints(Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
                if (t.GetChild(i).CompareTag("snappoint")) return true;
            return false;
        }

        private static void Add(Transform root, Vector3 local)
        {
            GameObject g = new GameObject(PointName);
            g.tag = "snappoint";   // an empty object: only its place counts
            g.transform.SetParent(root, false);
            g.transform.localPosition = local;
        }

        // the box of the piece's solid colliders, in the piece's own frame
        private static bool LocalBox(Transform root, out Bounds box)
        {
            box = new Bounds();
            bool any = false;
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.isTrigger) continue;
                Vector3 center, size;
                BoxCollider bc = c as BoxCollider;
                MeshCollider mc = c as MeshCollider;
                CapsuleCollider cc = c as CapsuleCollider;
                SphereCollider sc = c as SphereCollider;
                if (bc != null) { center = bc.center; size = bc.size; }
                else if (mc != null && mc.sharedMesh != null) { center = mc.sharedMesh.bounds.center; size = mc.sharedMesh.bounds.size; }
                else if (cc != null)
                {
                    center = cc.center;
                    size = Vector3.one * cc.radius * 2f;
                    size[cc.direction] = Mathf.Max(cc.height, cc.radius * 2f);
                }
                else if (sc != null) { center = sc.center; size = Vector3.one * sc.radius * 2f; }
                else continue;
                Matrix4x4 m = toRoot * c.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = center + Vector3.Scale(size * 0.5f, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) { box = new Bounds(p, Vector3.zero); any = true; }
                    else box.Encapsulate(p);
                }
            }
            return any;
        }

        // how far a snap point catches
        [HarmonyPatch(typeof(Player), "FindClosestSnapPoints"), HarmonyPrefix]
        private static void Reach(ref float maxSnapDistance)
        {
            if (Plugin.SnapDistance.Value > 0f) maxSnapDistance = Plugin.SnapDistance.Value;
        }
    }
}
