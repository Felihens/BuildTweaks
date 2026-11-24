using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // Fine placement (user, 2026-11-22): while placing a piece, the arrow keys move it NudgeStep (5 cm) at a time
    // along its own sides (the side closest to where you look counts as "forward"), Page Up / Page Down move it up
    // and down. Held keys repeat. The piece still follows the cursor and snapping; the nudge is added on top, and goes
    // back to zero after you place the piece, pick another one or put the hammer away. Shift keeps its game job
    // (placing without snapping), so the nudge has its own keys, all configurable.
    [HarmonyPatch]
    internal static class Nudge
    {
        private const float MaxOffset = 3f;        // metres, in any direction
        private const float RepeatAfter = 0.35f;   // seconds a key is held before it repeats
        private const float RepeatEvery = 0.06f;

        private static Vector3 _offset;
        private static Piece _piece;
        private static float _repeatAt;
        private static KeyCode _held = KeyCode.None;
        private static int _readFrame = -1;

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost"), HarmonyPostfix]
        private static void AfterGhost(Player __instance, GameObject ___m_placementGhost)
        {
            if (__instance != Player.m_localPlayer) return;
            Piece selected = __instance.GetSelectedPiece();
            if (selected != _piece || !__instance.InPlaceMode()) { _piece = selected; _offset = Vector3.zero; }
            if (___m_placementGhost == null || !___m_placementGhost.activeSelf) return;
            // placing runs this a second time in the same frame: read the keys once
            if (_readFrame != Time.frameCount && !Plugin.Busy()) { _readFrame = Time.frameCount; ReadKeys(___m_placementGhost.transform); }
            if (_offset != Vector3.zero) ___m_placementGhost.transform.position += _offset;
        }

        [HarmonyPatch(typeof(Player), "PlacePiece"), HarmonyPostfix]
        private static void Placed(Player __instance)
        {
            if (__instance == Player.m_localPlayer && _offset != Vector3.zero)
            {
                Plugin.Debug("[nudge] placed with offset " + _offset.ToString("0.00"));
                _offset = Vector3.zero;
            }
        }

        private static void ReadKeys(Transform ghost)
        {
            if (GameCamera.instance == null) return;
            Transform cam = GameCamera.instance.transform;
            Vector3 fwd = Closest(ghost, Flat(cam.forward));
            Vector3 right = Closest(ghost, Flat(cam.right));
            float step = Plugin.NudgeStep.Value;
            Try(Plugin.NudgeForward.Value, fwd * step);
            Try(Plugin.NudgeBack.Value, -fwd * step);
            Try(Plugin.NudgeRight.Value, right * step);
            Try(Plugin.NudgeLeft.Value, -right * step);
            Try(Plugin.NudgeUp.Value, Vector3.up * step);
            Try(Plugin.NudgeDown.Value, Vector3.down * step);
        }

        private static void Try(KeyboardShortcut key, Vector3 move)
        {
            bool go = false;
            if (Plugin.Pressed(key)) { go = true; _held = key.MainKey; _repeatAt = Time.time + RepeatAfter; }
            else if (_held == key.MainKey && Plugin.Held(key) && Time.time >= _repeatAt) { go = true; _repeatAt = Time.time + RepeatEvery; }
            if (!go) return;
            _offset = Vector3.ClampMagnitude(_offset + move, MaxOffset);
            if (_offset.sqrMagnitude < 0.000001f) _offset = Vector3.zero;
            _shownUntil = Time.time + 1.5f;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        // the piece's own side (forward, back, right or left, flat) closest to a direction
        private static Vector3 Closest(Transform ghost, Vector3 dir)
        {
            Vector3 f = Flat(ghost.forward), r = Flat(ghost.right);
            Vector3 best = f;
            float bestDot = Vector3.Dot(f, dir);
            Vector3[] others = { -f, r, -r };
            foreach (Vector3 o in others)
            {
                float d = Vector3.Dot(o, dir);
                if (d > bestDot) { bestDot = d; best = o; }
            }
            return best;
        }

        private static float _shownUntil;

        // the nudge, under the crosshair while it isn't zero
        internal static void OnGUI()
        {
            Player p = Player.m_localPlayer;
            if (_offset == Vector3.zero || p == null || !p.InPlaceMode() || Event.current == null || Event.current.type != EventType.Repaint) return;
            float s = Screen.height / 1080f;
            GUIStyle st = new GUIStyle(GUI.skin.label);
            st.alignment = TextAnchor.MiddleCenter;
            st.fontStyle = FontStyle.Bold;
            st.fontSize = Mathf.Max(10, Mathf.RoundToInt(17f * s));
            Rect r = new Rect(Screen.width / 2f - 150f * s, Screen.height / 2f + 70f * s, 300f * s, 26f * s);
            string txt = "moved " + (_offset.magnitude * 100f).ToString("0") + " cm" + (Mathf.Abs(_offset.y) > 0.001f ? " (" + (_offset.y > 0 ? "+" : "") + (_offset.y * 100f).ToString("0") + " up)" : "");
            Color old = GUI.color;
            float a = Time.time < _shownUntil ? 1f : 0.6f;
            GUI.color = new Color(0f, 0f, 0f, 0.85f * a);
            GUI.Label(new Rect(r.x + 1.5f * s, r.y + 1.5f * s, r.width, r.height), txt, st);
            GUI.color = new Color(0.75f, 0.9f, 1f, a);
            GUI.Label(r, txt, st);
            GUI.color = old;
        }
    }
}
