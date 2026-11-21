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

        [HarmonyPatch(typeof(Hud), "UpdateBuild"), HarmonyPostfix]
        private static void AfterUpdateBuild(Hud __instance, Player player)
        {
            if (__instance.m_buildSelection == null) return;
            string text = __instance.m_buildSelection.text;
            if (text == null) return;
            bool tagged = text.EndsWith(Tag);
            bool owned = Plugin.OwnedTag.Value && player != null && player.InPlaceMode() && IsOwned(player, player.GetSelectedPiece());
            if (owned && !tagged) __instance.m_buildSelection.text = text + Tag;
            else if (!owned && tagged) __instance.m_buildSelection.text = text.Substring(0, text.Length - Tag.Length);
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
