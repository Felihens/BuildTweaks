using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "felih.BuildTweaks";
        public const string NAME = "Build Tweaks";
        public const string VERSION = "1.4.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> FreeRemove;
        internal static ConfigEntry<bool> RemovedToInventory;
        internal static ConfigEntry<bool> DetailedLog, SnapEverything, MoreAngles, ShowAngle;
        internal static ConfigEntry<string> NoSnapPieces;
        internal static ConfigEntry<float> SnapDistance, RotationStep, FineRotationStep;
        internal static ConfigEntry<KeyboardShortcut> FineRotationKey;
        internal static ConfigEntry<bool> AutoLight, FiresWarm, TorchWarms;
        internal static ConfigEntry<float> AutoLightRange;
        internal static ConfigEntry<bool> OwnedTag, BuildCount, SupportPreview;
        internal static ConfigEntry<KeyboardShortcut> AreaRepairKey, UndoKey;
        internal static ConfigEntry<float> AreaRepairRadius, NudgeStep, BuildCountChestRange;
        internal static ConfigEntry<int> UndoHistory;
        internal static ConfigEntry<KeyboardShortcut> NudgeLeft, NudgeRight, NudgeForward, NudgeBack, NudgeUp, NudgeDown;

        private void Awake()
        {
            Log = Logger;
            FreeRemove = Config.Bind("1 - Removing", "NoStaminaCost", true,
                "Removing a building piece with the hammer costs no stamina.");
            RemovedToInventory = Config.Bind("1 - Removing", "MaterialsToInventory", true,
                "The materials of a piece you remove go straight into your inventory. What doesn't fit drops on the ground as usual.");
            SnapEverything = Config.Bind("2 - Snapping", "SnapEverything", true,
                "Pieces without snap points (chests, furniture, the workbench and more) get some: the corners of their box and the " +
                "middle of their bottom and top. So they snap beside, on top of and onto other pieces. Needs a game restart.");
            NoSnapPieces = Config.Bind("2 - Snapping", "NoSnapPieces", "",
                "Pieces (prefab names, comma separated) that should stay without snap points, e.g. piece_chest_wood,piece_bed02.");
            SnapDistance = Config.Bind("2 - Snapping", "SnapDistance", 0.8f,
                new ConfigDescription("How close (metres) a snap point of the piece you place must come to one nearby to snap. The game: 0.5. " +
                "0 = the game's own.", new AcceptableValueRange<float>(0f, 3f)));
            MoreAngles = Config.Bind("3 - Rotation", "MoreAngles", true,
                "Turn the piece you place in RotationStep degrees for each turn of the mouse wheel, instead of the game's 22.5.");
            RotationStep = Config.Bind("3 - Rotation", "RotationStep", 11.25f,
                new ConfigDescription("Degrees per turn of the wheel (11.25 = 32 angles, every angle of the game included).", new AcceptableValueRange<float>(0.5f, 90f)));
            FineRotationKey = Config.Bind("3 - Rotation", "FineRotationKey", new KeyboardShortcut(KeyCode.LeftAlt),
                "Hold this key while turning the wheel for FineRotationStep instead. None = off.");
            FineRotationStep = Config.Bind("3 - Rotation", "FineRotationStep", 1f,
                new ConfigDescription("Degrees per turn of the wheel while FineRotationKey is held.", new AcceptableValueRange<float>(0.1f, 45f)));
            ShowAngle = Config.Bind("3 - Rotation", "ShowAngle", true, "Show the angle under the crosshair for a moment after each turn.");
            AutoLight = Config.Bind("4 - Fires", "AutoLight", true,
                "Built fires (torches, sconces, braziers, campfires...) near you that burn out are filled up again for free. Fires turned off on purpose stay off.");
            AutoLightRange = Config.Bind("4 - Fires", "AutoLightRange", 50f,
                new ConfigDescription("How far from you (metres) fires are kept lit.", new AcceptableValueRange<float>(5f, 100f)));
            FiresWarm = Config.Bind("4 - Fires", "LightsWarmYou", true,
                "Every lit built fire warms you like a campfire (same distance). Only in your game. Needs a restart to change.");
            TorchWarms = Config.Bind("4 - Fires", "TorchInHandWarmsYou", true, "The torch in your hand warms you like a campfire.");
            OwnedTag = Config.Bind("5 - Build menu", "OwnedTag", true,
                "While placing a station upgrade (chopping block, tanning rack, forge bellows...), its name at the bottom of the screen shows \"(owned)\" when the nearest station of its kind already has it.");
            BuildCount = Config.Bind("5 - Build menu", "BuildCount", true,
                "While placing a piece, its name at the bottom of the screen shows how many of it you can build with what is in your inventory (x12). " +
                "Hidden when AzuCraftyBoxes is installed, since it already shows its own count.");
            BuildCountChestRange = Config.Bind("5 - Build menu", "BuildCountChestRange", 20f,
                new ConfigDescription("The build count also says how many you could build with the chests around you, e.g. x12 (42 with chests), " +
                "within this many metres (chests you may open only). It shows only when the chests add something. 0 = inventory only.",
                new AcceptableValueRange<float>(0f, 50f)));
            SupportPreview = Config.Bind("5 - Build menu", "SupportPreview", true,
                "While placing a piece, it takes the colour it will have in the hammer's support view (blue on the ground, green to yellow to red as support gets weaker, dark wine when it will fall right after you build it), " +
                "so you know how solid it will be before you build it. Red when the placement itself is not allowed, as usual.");
            AreaRepairKey = Config.Bind("6 - Repair", "AreaRepairKey", new KeyboardShortcut(KeyCode.LeftShift),
                "Hold this key while repairing a piece with the hammer to also repair every damaged piece around it. " +
                "Each extra piece costs what repairing it by hand costs (stamina, hammer durability). None = off.");
            AreaRepairRadius = Config.Bind("6 - Repair", "AreaRepairRadius", 7f,
                new ConfigDescription("How far (metres) from the piece you repair the other pieces are repaired too.", new AcceptableValueRange<float>(0.5f, 10f)));
            NudgeStep = Config.Bind("7 - Nudge", "NudgeStep", 0.05f,
                new ConfigDescription("Metres the piece you place moves for each press of a nudge key. It goes back to normal after you place it " +
                "or pick another piece.", new AcceptableValueRange<float>(0.01f, 0.5f)));
            NudgeForward = Config.Bind("7 - Nudge", "NudgeForward", new KeyboardShortcut(KeyCode.UpArrow),
                "Moves the piece you place away from you (along its own side closest to where you look). None = off.");
            NudgeBack = Config.Bind("7 - Nudge", "NudgeBack", new KeyboardShortcut(KeyCode.DownArrow), "Moves the piece you place towards you. None = off.");
            NudgeLeft = Config.Bind("7 - Nudge", "NudgeLeft", new KeyboardShortcut(KeyCode.LeftArrow), "Moves the piece you place to the left. None = off.");
            NudgeRight = Config.Bind("7 - Nudge", "NudgeRight", new KeyboardShortcut(KeyCode.RightArrow), "Moves the piece you place to the right. None = off.");
            NudgeUp = Config.Bind("7 - Nudge", "NudgeUp", new KeyboardShortcut(KeyCode.PageUp), "Moves the piece you place up. None = off.");
            NudgeDown = Config.Bind("7 - Nudge", "NudgeDown", new KeyboardShortcut(KeyCode.PageDown), "Moves the piece you place down. None = off.");
            UndoKey = Config.Bind("8 - Undo", "UndoKey", new KeyboardShortcut(KeyCode.Backspace),
                "With the hammer out, undoes your last build action: a piece you removed comes back at the same spot and angle (using its materials from your inventory), " +
                "a piece you placed is taken down and its materials come back. Press again for the one before. A piece that someone else already took down is skipped. Backspace by default: Ctrl+Z also crouches in Valheim and Z is the quick slot key of several " +
                "equipment-slot mods. Any key works, e.g. Z + LeftControl. None = off.");
            UndoHistory = Config.Bind("8 - Undo", "UndoHistory", 20,
                new ConfigDescription("How many build actions (pieces placed or removed) are remembered for undo.", new AcceptableValueRange<int>(1, 100)));
            DetailedLog = Config.Bind("9 - Debug", "DetailedLog", false, "Writes details to the BepInEx log.");
            new Harmony(GUID).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo(NAME + " " + VERSION + " loaded");
        }

        private void Update()
        {
            Removing.Update();
            Fires.Update();
            Undo.Update();
        }

        private void OnGUI()
        {
            Rotation.OnGUI();
            Nudge.OnGUI();
        }

        // A shortcut whose keys are all held now (main key and modifiers). None = never.
        internal static bool Held(KeyboardShortcut k)
        {
            if (k.MainKey == KeyCode.None || !Input.GetKey(k.MainKey)) return false;
            foreach (KeyCode m in k.Modifiers) if (!Input.GetKey(m)) return false;
            return true;
        }

        // A shortcut that was just pressed: its main key went down this frame with its modifiers held.
        internal static bool Pressed(KeyboardShortcut k)
        {
            if (k.MainKey == KeyCode.None || !Input.GetKeyDown(k.MainKey)) return false;
            foreach (KeyCode m in k.Modifiers) if (!Input.GetKey(m)) return false;
            return true;
        }

        // typing in the chat or the console, or a menu is open: our keys do nothing
        internal static bool Busy()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            if (global::Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible() || InventoryGui.IsVisible()) return true;
            if (Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible()) return true;
            return false;
        }

        internal static void Debug(string msg)
        {
            if (DetailedLog.Value) Log.LogInfo(msg);
        }
    }

    [HarmonyPatch]
    internal static class Removing
    {
        private const float Window = 3f;    // seconds after removing during which drops are collected
        private const float Radius = 4f;    // metres around the removed piece

        private struct Spot { public Vector3 Pos; public float Time; }
        private static readonly List<Spot> Spots = new List<Spot>();
        private static readonly List<ItemDrop> Pending = new List<ItemDrop>();
        private static readonly MethodInfo RequestOwn = AccessTools.Method(typeof(ItemDrop), "RequestOwn");
        internal static bool _inRemove;
        private static bool _skipStamina;

        // Player.RemovePiece runs when you remove a piece with the hammer.
        [HarmonyPatch(typeof(Player), "RemovePiece")]
        [HarmonyPrefix]
        private static void RemoveStart(Player __instance)
        {
            _inRemove = __instance == Player.m_localPlayer;
        }

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        [HarmonyPostfix]
        private static void RemoveDone(bool __result)
        {
            if (_inRemove && __result && Plugin.FreeRemove.Value) _skipStamina = true;
        }

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        [HarmonyFinalizer]
        private static void RemoveEnd()
        {
            _inRemove = false;
        }

        // The stamina for removing is charged right after RemovePiece returns.
        [HarmonyPatch(typeof(Player), "GetBuildStamina")]
        [HarmonyPrefix]
        private static bool NoStamina(ref float __result)
        {
            if (!_skipStamina) return true;
            _skipStamina = false;
            __result = 0f;
            return false;
        }

        // Normal building pieces: the piece's owner destroys it and drops the materials.
        [HarmonyPatch(typeof(WearNTear), "Remove")]
        [HarmonyPrefix]
        private static void WntRemoved(WearNTear __instance)
        {
            if (_inRemove) { Remember(__instance.transform.position, __instance.name); Undo.Record(__instance.GetComponent<Piece>()); }
        }

        // The piece's owner runs RPC_Remove. When that is your own game and you asked for the removal,
        // the materials can go straight into your inventory without ever dropping.
        private static bool _myRemove;

        [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
        [HarmonyPrefix]
        private static void RpcRemoveStart(long sender)
        {
            _myRemove = Plugin.RemovedToInventory.Value && Player.m_localPlayer != null && sender == ZNet.GetUID();
        }

        [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
        [HarmonyFinalizer]
        private static void RpcRemoveEnd()
        {
            _myRemove = false;
        }

        [HarmonyPatch(typeof(Piece), "DropResources")]
        [HarmonyPrefix]
        private static bool PieceDropped(Piece __instance, HitData hitData)
        {
            if (_inRemove) { Remember(__instance.transform.position, __instance.name); Undo.Record(__instance); }
            if (!(_myRemove || _inRemove) || !Plugin.RemovedToInventory.Value) return true;
            // Leave unusual cases (feasts, loot-chest pieces, free-build worlds) to the game.
            if (__instance.m_destroyedLootPrefab != null || __instance.GetComponent<Feast>() != null) return true;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(__instance.FreeBuildKey())) return true;

            Player p = Player.m_localPlayer;
            Inventory inv = p != null ? p.GetInventory() : null;
            if (inv == null) return true;

            List<GameObject> prefabs = new List<GameObject>();
            List<int> amounts = new List<int>();
            foreach (Piece.Requirement r in __instance.m_resources)
            {
                if (r.m_resItem == null || !r.m_recover) continue;
                GameObject prefab = ObjectDB.instance.GetItemPrefab(Utils.GetPrefabName(r.m_resItem.name));
                int amount = r.m_amount;
                if (amount <= 0) continue;
                prefab = Game.instance.CheckDropConversion(hitData, r.m_resItem, prefab, ref amount);
                if (prefab == null || amount <= 0) continue;
                if (!__instance.IsPlacedByPlayer()) amount = Mathf.Max(1, amount / 3);
                prefabs.Add(prefab);
                amounts.Add(amount);
            }
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (!inv.CanAddItem(prefabs[i], amounts[i]))
                {
                    Plugin.Debug("[remove] inventory full, dropping materials of " + __instance.name);
                    return true;
                }
            }
            for (int i = 0; i < prefabs.Count; i++)
            {
                inv.AddItem(prefabs[i], amounts[i]);
                ItemDrop id = prefabs[i].GetComponent<ItemDrop>();
                if (id != null)
                    p.Message(MessageHud.MessageType.TopLeft, "$msg_added " + id.m_itemData.m_shared.m_name, amounts[i], id.m_itemData.GetIcon());
                Plugin.Debug("[remove] to inventory (direct): " + prefabs[i].name + " x" + amounts[i]);
            }
            return false;
        }

        private static void Remember(Vector3 pos, string name)
        {
            if (!Plugin.RemovedToInventory.Value) return;
            Spot s; s.Pos = pos; s.Time = Time.time;
            Spots.Add(s);
            Plugin.Debug("[remove] " + name);
        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        [HarmonyPostfix]
        private static void Spawned(ItemDrop __instance)
        {
            if (Spots.Count == 0) return;
            Vector3 p = __instance.transform.position;
            foreach (Spot s in Spots)
            {
                if (Time.time - s.Time <= Window && Vector3.Distance(p, s.Pos) <= Radius)
                {
                    Pending.Add(__instance);
                    return;
                }
            }
        }

        internal static void Update()
        {
            for (int i = Spots.Count - 1; i >= 0; i--)
                if (Time.time - Spots[i].Time > Window) Spots.RemoveAt(i);
            if (Pending.Count == 0) return;

            Player p = Player.m_localPlayer;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                ItemDrop d = Pending[i];
                if (d == null || p == null || Spots.Count == 0) { Pending.RemoveAt(i); continue; }
                ZNetView nv = d.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                if (!nv.IsOwner())
                {
                    RequestOwn.Invoke(d, null);   // dropped by another player's client: ask for it, try next frame
                    continue;
                }
                string name = d.m_itemData != null ? d.m_itemData.m_shared.m_name + " x" + d.m_itemData.m_stack : d.name;
                if (p.Pickup(d.gameObject, false, false))
                {
                    Plugin.Debug("[remove] to inventory: " + name);
                    Pending.RemoveAt(i);
                }
                // If it didn't fit, keep trying until the window ends; then it stays on the ground.
            }
        }
    }
}
