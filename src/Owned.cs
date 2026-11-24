using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // "(owned)" tag (user, 2026-11-19): while you hold a station upgrade (chopping block, tanning rack, forge
    // bellows...) in build mode, its name at the bottom of the screen gets "(owned)" when the nearest station of
    // its kind already has that upgrade, so you don't build the same one twice.
    [HarmonyPatch]
    internal static class Owned
    {
        private const string Tag = " <color=#9BE39B>(owned)</color>";
        private const float StationSearch = 30f;   // metres around you to find the station the upgrade is for
        private const float CheckEvery = 0.5f;

        private static readonly List<StationExtension> Found = new List<StationExtension>();
        private static Piece _lastPiece;
        private static bool _lastOwned;
        private static float _nextCheck;

        // what we added after the piece's name last time ("(owned)" and the build count, 2026-11-22)
        private static string _suffix = "";
        private static Piece _countPiece;
        private static float _nextCount;
        private static string _countTag = "";

        [HarmonyPatch(typeof(Hud), "UpdateBuild"), HarmonyPostfix]
        private static void AfterUpdateBuild(Hud __instance, Player player)
        {
            if (__instance.m_buildSelection == null) return;
            string text = __instance.m_buildSelection.text;
            if (text == null) return;
            string name = _suffix.Length > 0 && text.EndsWith(_suffix) ? text.Substring(0, text.Length - _suffix.Length) : text;
            bool placing = player != null && player.InPlaceMode();
            Piece piece = placing ? player.GetSelectedPiece() : null;
            string suffix = "";
            if (Plugin.OwnedTag.Value && placing && IsOwned(player, piece)) suffix += Tag;
            if (Plugin.BuildCount.Value && piece != null && !CraftyBoxes()) suffix += CountTag(player, piece);
            if (suffix == _suffix && name.Length + suffix.Length == text.Length) return;
            _suffix = suffix;
            __instance.m_buildSelection.text = name + suffix;
        }

        // AzuCraftyBoxes already puts its own count, inventory + chests, after the name (user, 2026-11-24): ours would repeat it
        private static int _crafty = -1;
        private static bool CraftyBoxes()
        {
            if (_crafty < 0) _crafty = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Azumatt.AzuCraftyBoxes") ? 1 : 0;
            return _crafty == 1;
        }

        // " x12": how many of this piece the materials in your inventory make (nothing when building is free)
        private static string CountTag(Player player, Piece piece)
        {
            if (piece == _countPiece && Time.time < _nextCount) return _countTag;
            _countPiece = piece;
            _nextCount = Time.time + 0.25f;
            _countTag = "";
            if (piece.m_resources == null || piece.m_resources.Length == 0 || player.NoCostCheat()) return _countTag;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())) return _countTag;
            Inventory inv = player.GetInventory();
            if (inv == null) return _countTag;
            int can = int.MaxValue, withChests = int.MaxValue;
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0) continue;
                string item = r.m_resItem.m_itemData.m_shared.m_name;
                int have = inv.CountItems(item, -1, true);
                can = Mathf.Min(can, have / r.m_amount);
                withChests = Mathf.Min(withChests, (have + Nearby.Count(item)) / r.m_amount);
            }
            if (can == int.MaxValue) return _countTag;
            _countTag = can > 0 ? " <color=#E8D9A8>x" + can + "</color>" : " <color=#E07A6A>x0</color>";
            // (42 with chests): only when the chests around you make more of it (user's pick, 2026-11-22)
            if (withChests > can) _countTag += " <color=#9A8F7A><size=85%>(" + withChests + " with chests)</size></color>";
            return _countTag;
        }

        private static bool IsOwned(Player player, Piece piece)
        {
            if (piece == null) return false;
            if (piece == _lastPiece && Time.time < _nextCheck) return _lastOwned;
            _lastPiece = piece;
            _nextCheck = Time.time + CheckEvery;
            _lastOwned = false;
            StationExtension ext = piece.GetComponent<StationExtension>();
            if (ext == null || ext.m_stack || ext.m_craftingStation == null) return false;
            CraftingStation station = CraftingStation.FindClosestStationInRange(ext.m_craftingStation.m_name, player.transform.position, StationSearch);
            if (station == null) return false;
            Found.Clear();
            StationExtension.FindExtensions(station, station.transform.position, Found);
            string want = piece.gameObject.name;
            foreach (StationExtension e in Found)
                if (e != null && Utils.GetPrefabName(e.gameObject) == want) { _lastOwned = true; break; }
            Plugin.Debug("[owned] " + want + " for " + station.m_name + " at " + station.transform.position.ToString("0") + ": " + (_lastOwned ? "already there" : "not there yet"));
            return _lastOwned;
        }
    }
}
