using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Undo (user, 2026-11-22; placing added 2026-11-24): with the hammer out, UndoKey (Backspace by default,
    // configurable because other mods may use it: the user's CDEV quick slot 1 is Z and fires even with Ctrl held,
    // Recycle_N_Reclaim uses Ctrl+Z) undoes your last build action:
    //  - a piece you removed comes back at the same spot and angle, paying its materials like placing it again;
    //  - a piece you placed is taken down the way the hammer removes it, so its materials come back.
    // Pressing again goes further back (UndoHistory actions). Only your own actions in this session. A placed piece
    // that is gone already (someone else took it down, it broke) is skipped: nothing is removed and nothing comes back.
    // It checks the same things the game does (materials, the crafting station nearby, wards). In Valheim Left Ctrl
    // also crouches: when the Ctrl of the shortcut toggled crouching, it is set back.
    [HarmonyPatch]
    internal static class Undo
    {
        private struct Action
        {
            public bool Placed;          // true: you placed it (undo takes it down); false: you removed it (undo puts it back)
            public string Prefab;
            public Vector3 Pos;
            public Quaternion Rot;
            public int Id;
            public ZDOID Zdo;            // the piece in the world (placed), or the one that was removed
        }

        private static readonly List<Action> History = new List<Action>();
        private static readonly MethodInfo PlacePiece = AccessTools.Method(typeof(Player), "PlacePiece",
            new[] { typeof(Piece), typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(bool) });
        private static readonly MethodInfo ConsumeResources = AccessTools.Method(typeof(Player), "ConsumeResources");
        private static readonly MethodInfo CheckCanRemove = AccessTools.Method(typeof(Player), "CheckCanRemovePiece");
        private static readonly MethodInfo SetCrouch = AccessTools.Method(typeof(Player), "SetCrouch");
        private static readonly FieldInfo CrouchToggled = AccessTools.Field(typeof(Player), "m_crouchToggled");
        private static readonly FieldInfo NoCost = AccessTools.Field(typeof(Player), "m_noPlacementCost");

        private static Player _owner;
        private static bool _crouchLastFrame, _crouchBeforeCtrl;
        private static bool _busy;          // our own undo is placing or removing: not a new action
        private static bool _placing;       // inside Player.PlacePiece for the local player
        private static ZDOID _restored;     // the piece an undo just put back

        // ---------- recording ----------

        // A piece you removed with the hammer (called from Removing).
        internal static void Record(Piece piece)
        {
            if (_busy || !Usable(piece)) return;
            int id = piece.GetInstanceID();
            if (History.Count > 0 && !History[History.Count - 1].Placed && History[History.Count - 1].Id == id) return;   // the same removal seen twice
            ZNetView nv = piece.GetComponent<ZNetView>();
            Add(false, piece, nv != null && nv.IsValid() ? nv.GetZDO().m_uid : ZDOID.None);
        }

        [HarmonyPatch(typeof(Player), "PlacePiece"), HarmonyPrefix]
        private static void PlaceStart(Player __instance)
        {
            _placing = __instance == Player.m_localPlayer;
        }

        [HarmonyPatch(typeof(Player), "PlacePiece"), HarmonyFinalizer]
        private static void PlaceEnd()
        {
            _placing = false;
        }

        // A piece you placed: PlacePiece calls OnPlaced on the new piece.
        [HarmonyPatch(typeof(WearNTear), "OnPlaced"), HarmonyPostfix]
        private static void Placed(WearNTear __instance)
        {
            if (!_placing) return;
            ZNetView nv = __instance.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) return;
            if (_busy) { _restored = nv.GetZDO().m_uid; return; }
            Piece piece = __instance.GetComponent<Piece>();
            if (!Usable(piece)) return;
            Add(true, piece, nv.GetZDO().m_uid);
        }

        private static bool Usable(Piece piece)
        {
            if (piece == null || PlacePiece == null) return false;
            Player p = Player.m_localPlayer;
            if (p == null) return false;
            if (_owner != p) { History.Clear(); _owner = p; }
            // things that aren't plain building pieces stay out
            if (piece.GetComponent<ItemDrop>() != null || piece.GetComponent<Feast>() != null || piece.GetComponent<TerrainModifier>() != null) return false;
            if (piece.m_resources == null || piece.m_resources.Length == 0) return false;
            return ZNetScene.instance != null && ZNetScene.instance.GetPrefab(Utils.GetPrefabName(piece.gameObject)) != null;
        }

        private static void Add(bool placed, Piece piece, ZDOID zdo)
        {
            Action a;
            a.Placed = placed;
            a.Prefab = Utils.GetPrefabName(piece.gameObject);
            a.Pos = piece.transform.position;
            a.Rot = piece.transform.rotation;
            a.Id = piece.GetInstanceID();
            a.Zdo = zdo;
            History.Add(a);
            int keep = Mathf.Max(1, Plugin.UndoHistory.Value);
            while (History.Count > keep) History.RemoveAt(0);
            Plugin.Debug("[undo] remembered " + (placed ? "placing " : "removing ") + a.Prefab + " at " + a.Pos.ToString("0.00") + " (" + History.Count + " in history)");
        }

        // ---------- undoing ----------

        internal static void Update()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return;
            KeyboardShortcut key = Plugin.UndoKey.Value;
            bool crouch = CrouchToggled != null && (bool)CrouchToggled.GetValue(p);
            // the crouch state from before Ctrl went down (Ctrl toggles crouching in Valheim)
            foreach (KeyCode m in key.Modifiers)
                if ((m == KeyCode.LeftControl || m == KeyCode.RightControl) && Input.GetKeyDown(m)) _crouchBeforeCtrl = _crouchLastFrame;
            _crouchLastFrame = crouch;

            if (!Plugin.Pressed(key) || Plugin.Busy() || !p.InPlaceMode() || p.IsDead()) return;
            if (HasCtrl(key) && SetCrouch != null && crouch != _crouchBeforeCtrl)
            {
                SetCrouch.Invoke(p, new object[] { _crouchBeforeCtrl });
                _crouchLastFrame = _crouchBeforeCtrl;
            }
            if (_owner != p) { History.Clear(); _owner = p; }
            if (History.Count == 0) { p.Message(MessageHud.MessageType.Center, "Nothing to undo"); return; }

            Action a = History[History.Count - 1];
            if (a.Placed) TakeDown(p, a);
            else PutBack(p, a);
        }

        // undo a removal: the piece comes back where it was
        private static void PutBack(Player p, Action a)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(a.Prefab) : null;
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece == null) { History.RemoveAt(History.Count - 1); return; }

            bool free = (NoCost != null && (bool)NoCost.GetValue(p)) ||
                        (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));
            if (!free && !p.HaveRequirements(piece, Player.RequirementMode.CanBuild))
            {
                p.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return;
            }
            if (Location.IsInsideNoBuildLocation(a.Pos)) { p.Message(MessageHud.MessageType.Center, "$msg_nobuildzone"); return; }
            if (!PrivateArea.CheckAccess(a.Pos, 0f, true, false)) { p.Message(MessageHud.MessageType.Center, "$msg_privatezone"); return; }

            _busy = true;
            _restored = ZDOID.None;
            try { PlacePiece.Invoke(p, new object[] { piece, a.Pos, a.Rot, true, false }); }
            finally { _busy = false; }
            if (!free && ConsumeResources != null) ConsumeResources.Invoke(p, new object[] { piece.m_resources, 0, -1, 1 });
            History.RemoveAt(History.Count - 1);
            // an earlier "you placed it" now means the piece that just came back, so undoing further takes it down
            if (a.Zdo != ZDOID.None && _restored != ZDOID.None)
                for (int i = 0; i < History.Count; i++)
                    if (History[i].Placed && History[i].Zdo == a.Zdo) { Action b = History[i]; b.Zdo = _restored; History[i] = b; }
            p.Message(MessageHud.MessageType.Center, "Put back: " + Localization.instance.Localize(piece.m_name));
            Plugin.Debug("[undo] put back " + a.Prefab + " at " + a.Pos.ToString("0.00") + (free ? " (free)" : "") + ", " + History.Count + " left");
        }

        // undo a placement: the piece is taken down like the hammer does it, and its materials come back
        private static void TakeDown(Player p, Action a)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(a.Zdo) : null;
            if (go == null)
            {
                if (ZDOMan.instance != null && ZDOMan.instance.GetZDO(a.Zdo) != null)
                {
                    p.Message(MessageHud.MessageType.Center, "Too far away to undo");   // still there, just not loaded
                    return;
                }
                Gone(p, a, "no longer exists");
                return;
            }
            Piece piece = go.GetComponent<Piece>();
            WearNTear wnt = go.GetComponent<WearNTear>();
            ZNetView nv = go.GetComponent<ZNetView>();
            if (piece == null || wnt == null || nv == null || !nv.IsValid() || Utils.GetPrefabName(go) != a.Prefab) { Gone(p, a, "is not the same piece"); return; }
            if (!piece.m_canBeRemoved) { Gone(p, a, "can't be removed"); return; }
            Vector3 pos = go.transform.position;
            if (Location.IsInsideNoBuildLocation(pos)) { p.Message(MessageHud.MessageType.Center, "$msg_nobuildzone"); return; }
            if (!PrivateArea.CheckAccess(pos, 0f, true, false)) { p.Message(MessageHud.MessageType.Center, "$msg_privatezone"); return; }
            if (CheckCanRemove != null && !(bool)CheckCanRemove.Invoke(p, new object[] { piece })) return;   // it says why (station missing)
            if (!piece.CanBeRemoved()) { p.Message(MessageHud.MessageType.Center, "$msg_cantremovenow"); return; }

            string name = Localization.instance.Localize(piece.m_name);
            Quaternion rot = go.transform.rotation;
            // as if you removed it with the hammer: Build Tweaks sends its materials to your inventory
            _busy = true;
            Removing._inRemove = true;
            try
            {
                IRemoved removed = go.GetComponent<IRemoved>();
                if (removed != null) removed.OnRemoved();
                wnt.Remove(false);
            }
            finally { Removing._inRemove = false; _busy = false; }
            piece.m_placeEffect.Create(pos, rot, null, 1f, -1, p.GetZDOID());
            p.m_removeEffects.Create(pos, Quaternion.identity, null, 1f, -1, p.GetZDOID());
            History.RemoveAt(History.Count - 1);
            p.Message(MessageHud.MessageType.Center, "Taken down: " + name);
            Plugin.Debug("[undo] took down " + a.Prefab + " at " + pos.ToString("0.00") + ", " + History.Count + " left");
        }

        private static void Gone(Player p, Action a, string why)
        {
            History.RemoveAt(History.Count - 1);
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(a.Prefab) : null;
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            string name = piece != null ? Localization.instance.Localize(piece.m_name) : a.Prefab;
            p.Message(MessageHud.MessageType.Center, name + " is already gone, skipped");
            Plugin.Debug("[undo] skipped " + a.Prefab + " at " + a.Pos.ToString("0.00") + ": " + why);
        }

        private static bool HasCtrl(KeyboardShortcut k)
        {
            foreach (KeyCode m in k.Modifiers) if (m == KeyCode.LeftControl || m == KeyCode.RightControl) return true;
            return false;
        }
    }
}
