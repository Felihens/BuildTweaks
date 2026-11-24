using System.Collections.Generic;
using UnityEngine;

namespace BuildTweaks
{
    // What the chests around you hold, for the build count's "(42 with chests)" (user, 2026-11-22, same rules as
    // Recipe Book's "nearby"): only chests you may open (not someone else's private chest, not tombstones), within
    // BuildCountChestRange metres. Counted at most every 2 seconds, and only while you are placing a piece.
    internal static class Nearby
    {
        private static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private static float _next = -1f;

        internal static int Count(string itemName)
        {
            Refresh();
            int n;
            return _counts.TryGetValue(itemName, out n) ? n : 0;
        }

        private static void Refresh()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 2f;
            _counts.Clear();
            Player p = Player.m_localPlayer;
            float range = Plugin.BuildCountChestRange.Value;
            if (p == null || range <= 0f) return;
            Vector3 at = p.transform.position;
            float r2 = range * range;
            long me = p.GetPlayerID();
            foreach (Container c in Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
            {
                if (c == null || (c.transform.position - at).sqrMagnitude > r2) continue;
                Piece piece = c.GetComponent<Piece>();
                if (piece == null || c.GetComponent<TombStone>() != null) continue;
                if (c.m_privacy == Container.PrivacySetting.Private && piece.GetCreator() != me) continue;   // someone else's private chest
                Inventory inv = c.GetInventory();
                if (inv == null) continue;
                foreach (ItemDrop.ItemData it in inv.GetAllItems())
                {
                    string name = it.m_shared.m_name;
                    int n;
                    _counts.TryGetValue(name, out n);
                    _counts[name] = n + it.m_stack;
                }
            }
        }
    }
}
