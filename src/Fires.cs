using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Fires and lights (user, 2026-11-16):
    //  - Every built fire (torches, sconces, braziers, campfires, hearths...) within AutoLightRange of you that has
    //    burnt out, or is about to, is filled up again for free, so it stays lit. Fires that never run out, can't be
    //    refilled, or were turned off on purpose are left alone.
    //  - Every lit built fire warms you like a campfire, and so does the torch in your hand. The warmth is the game's
    //    own (the campfire's warm area, same size), made only in your game: only you feel it.
    [HarmonyPatch]
    internal static class Fires
    {
        private const float ScanEvery = 7f;
        private const float RefillBelow = 1f;   // fuel left (in fuel items) when it is filled up again (one item lasts far longer than ScanEvery)
        private const string HeatName = "BuildTweaks_Heat";

        private static readonly List<Fireplace> All = new List<Fireplace>();
        private static float _nextScan;
        private static float _heatRadius = -1f;
        private static int _heatLayer = -1;
        private static GameObject _handHeat;
        private static readonly System.Reflection.FieldInfo RightItem = AccessTools.Field(typeof(Humanoid), "m_rightItem"), LeftItem = AccessTools.Field(typeof(Humanoid), "m_leftItem");

        [HarmonyPatch(typeof(Fireplace), "Awake"), HarmonyPostfix]
        private static void Seen(Fireplace __instance)
        {
            All.Add(__instance);
            if (Plugin.FiresWarm.Value) AddHeat(__instance);
        }

        internal static void Update()
        {
            Player p = Player.m_localPlayer;
            if (p == null) { if (_handHeat != null) Object.Destroy(_handHeat); return; }
            HandTorch(p);
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + ScanEvery;
            if (!Plugin.AutoLight.Value) return;
            float range = Plugin.AutoLightRange.Value;
            Vector3 me = p.transform.position;
            int lit = 0;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                Fireplace f = All[i];
                if (f == null) { All.RemoveAt(i); continue; }
                if (f.m_infiniteFuel || !f.m_canRefill || f.m_maxFuel <= 0f) continue;
                if (f.GetComponent<Piece>() == null) continue;   // only built ones
                if ((f.transform.position - me).sqrMagnitude > range * range) continue;
                ZNetView nv = f.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                ZDO zdo = nv.GetZDO();
                if (f.m_canTurnOff && zdo.GetInt(ZDOVars.s_state, 1) == 0) continue;   // turned off on purpose
                if (zdo.GetFloat(ZDOVars.s_fuel, 0f) >= RefillBelow) continue;
                nv.InvokeRPC("RPC_SetFuelAmount", new object[] { f.m_maxFuel });
                lit++;
            }
            if (lit > 0) Plugin.Debug("[fires] lit " + lit + " fire(s) within " + range.ToString("0") + " m");
        }

        // the campfire's own warm area: its size and layer
        private static bool HeatShape()
        {
            if (_heatRadius > 0f) return true;
            GameObject pit = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("fire_pit") : null;
            if (pit == null) return false;
            foreach (EffectArea a in pit.GetComponentsInChildren<EffectArea>(true))
            {
                if ((a.m_type & EffectArea.Type.Heat) == 0) continue;
                Collider col = a.GetComponent<Collider>();
                SphereCollider sc = col as SphereCollider;
                float scale = Mathf.Max(a.transform.lossyScale.x, Mathf.Max(a.transform.lossyScale.y, a.transform.lossyScale.z));
                _heatRadius = sc != null ? sc.radius * scale : col != null ? col.bounds.extents.magnitude : 3f;
                _heatLayer = a.gameObject.layer;
                Plugin.Debug("[fires] campfire warmth: radius " + _heatRadius.ToString("0.0") + " m, layer " + LayerMask.LayerToName(_heatLayer));
                return true;
            }
            _heatRadius = 3f;
            _heatLayer = LayerMask.NameToLayer("character_trigger");
            Plugin.Debug("[fires] campfire warmth not found, using 3 m");
            return true;
        }

        private static GameObject NewHeat(Transform parent, Vector3 localPos)
        {
            GameObject g = new GameObject(HeatName);
            g.SetActive(false);
            g.layer = _heatLayer;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            SphereCollider sc = g.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            Vector3 s = parent.lossyScale;
            float k = Mathf.Max(0.01f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
            sc.radius = _heatRadius / k;
            EffectArea a = g.AddComponent<EffectArea>();
            a.m_type = EffectArea.Type.Heat;
            a.m_playerOnly = true;
            g.SetActive(true);
            return g;
        }

        // a built fire that doesn't warm yet: the warm area goes on the part that shows only while it burns
        private static void AddHeat(Fireplace f)
        {
            if (f == null || f.GetComponent<Piece>() == null || f.m_enabledObject == null) return;
            foreach (EffectArea a in f.GetComponentsInChildren<EffectArea>(true))
                if ((a.m_type & EffectArea.Type.Heat) != 0) return;   // it warms already (campfire, hearth)
            if (!HeatShape()) return;
            Transform on = f.m_enabledObject.transform;
            NewHeat(on, on.InverseTransformPoint(f.transform.position + Vector3.up * 0.5f));
        }

        // the torch in your hand warms you while it is out
        private static void HandTorch(Player p)
        {
            bool want = Plugin.TorchWarms.Value && HoldsTorch(p) && HeatShape();
            if (want && _handHeat == null) { _handHeat = NewHeat(p.transform, Vector3.up * 1f); Plugin.Debug("[fires] torch in hand: warm"); }
            else if (!want && _handHeat != null) { Object.Destroy(_handHeat); _handHeat = null; }
        }

        private static bool HoldsTorch(Player p)
        {
            ItemDrop.ItemData r = RightItem != null ? RightItem.GetValue(p) as ItemDrop.ItemData : null, l = LeftItem != null ? LeftItem.GetValue(p) as ItemDrop.ItemData : null;
            return IsTorch(r) || IsTorch(l);
        }

        private static bool IsTorch(ItemDrop.ItemData it)
        {
            if (it == null || it.m_shared == null) return false;
            if (it.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) return true;
            return it.m_dropPrefab != null && it.m_dropPrefab.name == "Torch";
        }
    }
}
