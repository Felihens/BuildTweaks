using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Support preview (user, 2026-11-24): the game only shows how well a piece is held up (the hammer's colour view)
    // after you build it. While placing, the ghost now takes that same colour: light blue when it stands on the
    // ground or on rock, then green, yellow and red as the support gets weaker, and dark wine when it has too little and will fall. It is the game's own sum done on the
    // ghost (WearNTear.UpdateSupport: support from each piece it touches, minus a loss for distance and angle, the
    // best pair of supports on opposite sides, the material's max and min) and the game's own colours
    // (WearNTear.Highlight). When the placement isn't allowed the game's red stays. Pieces that never need support
    // keep their normal look.
    [HarmonyPatch]
    internal static class Support
    {
        private const float RecheckEvery = 0.25f;   // seconds, when the ghost doesn't move

        private static readonly FieldInfo Status = AccessTools.Field(typeof(Player), "m_placementStatus");
        private static readonly MethodInfo MaterialProps = AccessTools.Method(typeof(WearNTear), "GetMaterialProperties");
        private static readonly MethodInfo GetCom = AccessTools.Method(typeof(WearNTear), "GetCOM");
        private static readonly MethodInfo GetSupport = AccessTools.Method(typeof(WearNTear), "GetSupport");
        private static readonly FieldInfo ForceCom = AccessTools.Field(typeof(WearNTear), "m_forceCorrectCOMCalculation");
        private static readonly Collider[] Hits = new Collider[128];
        private static readonly List<Vector3> Points = new List<Vector3>();
        private static readonly List<float> Values = new List<float>();
        private static readonly HashSet<Collider> Own = new HashSet<Collider>();
        private static readonly Vector3[] Corners = new Vector3[8];

        private static int _mask, _terrain = -2;
        private static GameObject _ghost;
        private static Vector3 _pos;
        private static Quaternion _rot;
        private static float _next, _logAt;
        private static Color _color;
        private static string _logged;

        // after Nudge has moved the ghost (lower priority = later)
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost"), HarmonyPostfix, HarmonyPriority(Priority.Low)]
        private static void AfterGhost(Player __instance, GameObject ___m_placementGhost)
        {
            if (__instance != Player.m_localPlayer || !Plugin.SupportPreview.Value) return;
            GameObject g = ___m_placementGhost;
            if (g == null || !g.activeSelf || Status == null || MaterialProps == null || GetCom == null || GetSupport == null) return;
            if (System.Convert.ToInt32(Status.GetValue(__instance)) != 0) return;   // not allowed here: the game's red stays
            Piece selected = __instance.GetSelectedPiece();
            WearNTear wnt = selected != null ? selected.GetComponent<WearNTear>() : null;
            if (wnt == null || !wnt.m_noSupportWear) return;   // a piece that never needs support

            Transform t = g.transform;
            if (g != _ghost || t.position != _pos || t.rotation != _rot || Time.time >= _next)
            {
                if (g != _ghost) { _ghost = g; Own.Clear(); foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) Own.Add(c); }
                _pos = t.position;
                _rot = t.rotation;
                _next = Time.time + RecheckEvery;
                float max, min;
                float support = Compute(g, wnt, out max, out min);
                _color = ColorFor(support, max, min);
                if (Plugin.DetailedLog.Value && Time.time >= _logAt)
                {
                    string line = selected.name + " " + support.ToString("0") + "/" + max.ToString("0") + " (breaks under " + min.ToString("0") + ")";
                    if (line != _logged) { _logged = line; _logAt = Time.time + 1f; Plugin.Debug("[support] " + line); }
                }
            }
            MaterialMan.instance.SetValue(g, ShaderProps._Color, _color, false);
            MaterialMan.instance.SetValue(g, ShaderProps._EmissionColor, _color * 0.4f, false);
        }

        // WearNTear.UpdateSupport, done for the ghost where it is now
        private static float Compute(GameObject g, WearNTear wnt, out float max, out float min)
        {
            object[] args = { 0f, 0f, 0f, 0f };
            MaterialProps.Invoke(wnt, args);
            max = (float)args[0];
            min = (float)args[1];
            float hLoss = (float)args[2], vLoss = (float)args[3];
            if (_terrain == -2)
            {
                _terrain = LayerMask.NameToLayer("terrain");
                _mask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
            }
            bool forceCom = ForceCom != null && (bool)ForceCom.GetValue(wnt);
            Vector3 com = g.transform.position + g.transform.rotation * wnt.m_comOffset;
            float support = 0f;
            Points.Clear();
            Values.Clear();

            foreach (Collider own in Own)
            {
                if (own == null || own.isTrigger) continue;
                Vector3 centre, half;
                Quaternion rot;
                if (!Bound(own, out centre, out half, out rot)) continue;
                int n = Physics.OverlapBoxNonAlloc(centre, half, Hits, rot, _mask);
                for (int i = 0; i < n; i++)
                {
                    Collider c = Hits[i];
                    if (c == null || c.attachedRigidbody != null || c.isTrigger || Own.Contains(c)) continue;
                    if (c.gameObject.layer == _terrain) return max;   // on the ground
                    WearNTear o = c.GetComponentInParent<WearNTear>();
                    if (o == null) return max;                         // on rock or another solid thing that isn't a piece
                    if (!o.m_supports) continue;
                    float d = Vector3.Distance(com, Com(o)) + 0.1f;
                    float d2 = Vector3.Distance(com, o.transform.position) + 0.1f;
                    if (d2 < d && !forceCom) d = d2;
                    float s = (float)GetSupport.Invoke(o, null);
                    support = Mathf.Max(support, s - hLoss * d * s);
                    Vector3 sp = SupportPoint(com, o, c);
                    if (sp.y < com.y + 0.05f)
                    {
                        Vector3 dir = (sp - com).normalized;
                        if (dir.y < 0f)
                        {
                            float k = Mathf.Acos(1f - Mathf.Abs(dir.y)) / 1.5707964f;
                            float loss = Mathf.Lerp(hLoss, vLoss, k);
                            support = Mathf.Max(support, s - loss * d * s);
                        }
                        Points.Add(sp);
                        Values.Add(s - vLoss * d * s);
                    }
                }
            }
            // two supports on opposite sides hold it better than either one
            for (int i = 0; i < Points.Count - 1; i++)
            {
                Vector3 a = Points[i] - com; a.y = 0f;
                for (int j = i + 1; j < Points.Count; j++)
                {
                    float avg = (Values[i] + Values[j]) * 0.5f;
                    if (avg <= support) continue;
                    Vector3 b = Points[j] - com; b.y = 0f;
                    if (Vector3.Angle(a, b) >= 100f) support = avg;
                }
            }
            return Mathf.Min(support, max);
        }

        // the box the game uses for each collider (WearNTear.SetupColliders), 0.3 m bigger, as half sizes
        private static bool Bound(Collider c, out Vector3 centre, out Vector3 half, out Quaternion rot)
        {
            Transform t = c.transform;
            Vector3 size;
            BoxCollider box = c as BoxCollider;
            if (box != null)
            {
                rot = t.rotation;
                centre = t.position + t.TransformVector(box.center);
                Vector3 s = t.lossyScale;
                size = new Vector3(s.x * box.size.x, s.y * box.size.y, s.z * box.size.z);
            }
            else
            {
                rot = Quaternion.identity;
                Bounds b;
                if (c.enabled) b = c.bounds;
                else
                {
                    // ghost colliders off the placing layers are switched off and have no bounds: use the mesh's
                    MeshCollider mc = c as MeshCollider;
                    if (mc == null || mc.sharedMesh == null) { centre = half = Vector3.zero; return false; }
                    Bounds local = mc.sharedMesh.bounds;
                    Vector3 lo = local.min, hi = local.max;
                    for (int i = 0; i < 8; i++)
                        Corners[i] = t.TransformPoint(new Vector3((i & 1) == 0 ? lo.x : hi.x, (i & 2) == 0 ? lo.y : hi.y, (i & 4) == 0 ? lo.z : hi.z));
                    b = new Bounds(Corners[0], Vector3.zero);
                    for (int i = 1; i < 8; i++) b.Encapsulate(Corners[i]);
                }
                centre = b.center;
                size = b.size;
            }
            half = (size + new Vector3(0.3f, 0.3f, 0.3f)) * 0.5f;
            return true;
        }

        private static Vector3 Com(WearNTear w) { return (Vector3)GetCom.Invoke(w, null); }

        // WearNTear.FindSupportPoint
        private static Vector3 SupportPoint(Vector3 com, WearNTear other, Collider c)
        {
            MeshCollider mc = c as MeshCollider;
            if (mc != null && !mc.convex)
            {
                RaycastHit hit;
                if (mc.Raycast(new Ray(com, Vector3.down), out hit, 10f)) return hit.point;
                return (com + Com(other)) * 0.5f;
            }
            return c.ClosestPoint(com);
        }

        private static readonly Color Breaks = new Color(0.45f, 0f, 0.22f);

        // WearNTear.GetSupportColorValue + Highlight
        private static Color ColorFor(float support, float max, float min)
        {
            if (support >= max) return new Color(0.6f, 0.8f, 1f);
            // under the material's minimum it collapses right after you build it: dark wine, apart from the
            // game's bright red for "can't place here" (user, 2026-11-24)
            if (support < min) return Breaks;
            float v = Mathf.Clamp01((support - min) / (max * 0.5f - min));
            Color c = Color.Lerp(new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), v);
            float h, s, val;
            Color.RGBToHSV(c, out h, out s, out val);
            return Color.HSVToRGB(h, Mathf.Lerp(1f, 0.5f, v), Mathf.Lerp(1.2f, 0.9f, v));
        }
    }
}
