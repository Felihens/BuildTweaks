using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Repair in an area (user, 2026-11-22, 3 m): holding AreaRepairKey (Left Shift) while repairing a piece with the
    // hammer also repairs every damaged piece within AreaRepairRadius of it. Each extra piece goes through the same
    // checks as repairing it by hand (crafting station nearby, wards) and costs the same (build stamina, hammer
    // durability); it stops when you run out of stamina. In repair mode Shift does nothing else in the game.
    [HarmonyPatch]
    internal static class AreaRepair
    {
        private static readonly List<Piece> Near = new List<Piece>();
        private static readonly MethodInfo CanRemove = AccessTools.Method(typeof(Player), "CheckCanRemovePiece");
        private static readonly MethodInfo BuildStamina = AccessTools.Method(typeof(Player), "GetBuildStamina");
        private static bool _quiet;

        [HarmonyPatch(typeof(Player), "Repair"), HarmonyPostfix]
        private static void AfterRepair(Player __instance, ItemDrop.ItemData toolItem)
        {
            if (__instance != Player.m_localPlayer || toolItem == null || !Plugin.Held(Plugin.AreaRepairKey.Value)) return;
            Piece centre = __instance.GetHoveringPiece();
            if (centre == null) return;
            Near.Clear();
            Piece.GetAllPiecesInRadius(centre.transform.position, Plugin.AreaRepairRadius.Value, Near);
            int fixedCount = 0;
            float stamina = BuildStamina != null ? (float)BuildStamina.Invoke(__instance, null) : 0f;
            foreach (Piece piece in Near)
            {
                if (piece == null || piece == centre) continue;
                WearNTear wnt = piece.GetComponent<WearNTear>();
                if (wnt == null || wnt.GetHealthPercentage() >= 0.999f) continue;
                if (stamina > 0f && !__instance.HaveStamina(stamina)) { Hud.instance.StaminaBarEmptyFlash(); break; }
                if (!Allowed(__instance, piece)) continue;
                if (!wnt.Repair()) continue;
                fixedCount++;
                piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, __instance.GetZDOID());
                __instance.UseStamina(stamina);
                if (toolItem.m_shared.m_useDurability)
                    toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate;
            }
            if (fixedCount > 0)
            {
                __instance.Message(MessageHud.MessageType.TopLeft, "Repaired " + fixedCount + " more piece" + (fixedCount == 1 ? "" : "s") + " around it");
                Plugin.Debug("[repair] area " + Plugin.AreaRepairRadius.Value + " m around " + centre.name + ": " + fixedCount + " repaired");
            }
        }

        // the game's own checks for repairing a piece, without their messages for every piece
        private static bool Allowed(Player p, Piece piece)
        {
            if (!PrivateArea.CheckAccess(piece.transform.position, 0f, false, false)) return false;
            if (CanRemove == null) return true;
            _quiet = true;
            try { return (bool)CanRemove.Invoke(p, new object[] { piece }); }
            finally { _quiet = false; }
        }

        [HarmonyPatch(typeof(Player), "Message"), HarmonyPrefix]
        private static bool Quiet() { return !_quiet; }
    }
}
