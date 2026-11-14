using HarmonyLib;
using UnityEngine;

namespace BuildTweaks
{
    // More rotation angles. The game turns the piece you place in steps of 22.5 degrees (16 angles): each turn of the
    // mouse wheel moves Player.m_placeRotation by one, and the piece is turned m_placeRotationDegrees times that.
    // Here each turn moves our own angle by RotationStep (FineRotationStep while FineRotationKey is held), and the
    // game is handed that angle as one step of that many degrees, so the game itself places the piece at it.
    // Anything the game does to the rotation on its own (copying a piece's rotation, a reset) is taken as it is.
    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    internal static class Rotation
    {
        private static float _angle;
        private static int _setRot = int.MinValue;
        private static float _setDeg = float.NaN;
        private static int _before;
        private static float _shownUntil;

        private static void Prefix(Player __instance, ref int ___m_placeRotation, ref float ___m_placeRotationDegrees)
        {
            if (__instance != Player.m_localPlayer || !Plugin.MoreAngles.Value) return;
            // the game changed it since our last frame (it copied a piece's rotation, or reset it): take that angle
            if (___m_placeRotation != _setRot || !Mathf.Approximately(___m_placeRotationDegrees, _setDeg))
                _angle = ___m_placeRotationDegrees * ___m_placeRotation;
            _before = ___m_placeRotation;
        }

        private static void Postfix(Player __instance, ref int ___m_placeRotation, ref float ___m_placeRotationDegrees)
        {
            if (__instance != Player.m_localPlayer || !Plugin.MoreAngles.Value) return;
            int turned = ___m_placeRotation - _before;
            if (turned != 0 && Mathf.Approximately(___m_placeRotationDegrees, _setDeg) && Mathf.Abs(turned) <= 2)
            {
                bool fine = Plugin.FineRotationKey.Value.MainKey != KeyCode.None && Input.GetKey(Plugin.FineRotationKey.Value.MainKey);
                float step = fine ? Plugin.FineRotationStep.Value : Plugin.RotationStep.Value;
                _angle += turned * Mathf.Clamp(step, 0.1f, 90f);
                _shownUntil = Time.time + 1.5f;
            }
            else if (turned != 0) _angle = ___m_placeRotationDegrees * ___m_placeRotation;   // the game's own change
            _angle = Mathf.Repeat(_angle, 360f);
            if (_angle > 359.95f) _angle = 0f;
            ___m_placeRotation = 1;
            ___m_placeRotationDegrees = _angle;
            _setRot = 1;
            _setDeg = _angle;
        }

        // the angle, under the crosshair for a moment after each turn
        internal static void OnGUI()
        {
            if (!Plugin.MoreAngles.Value || !Plugin.ShowAngle.Value || Time.time > _shownUntil) return;
            Player p = Player.m_localPlayer;
            if (p == null || !p.InPlaceMode() || Event.current == null || Event.current.type != EventType.Repaint) return;
            float s = Screen.height / 1080f;
            GUIStyle st = new GUIStyle(GUI.skin.label);
            st.alignment = TextAnchor.MiddleCenter;
            st.fontStyle = FontStyle.Bold;
            st.fontSize = Mathf.Max(10, Mathf.RoundToInt(20f * s));
            Rect r = new Rect(Screen.width / 2f - 100f * s, Screen.height / 2f + 40f * s, 200f * s, 30f * s);
            string txt = (_angle % 1f == 0f ? _angle.ToString("0") : _angle.ToString("0.##")) + "°";
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 1.5f * s, r.y + 1.5f * s, r.width, r.height), txt, st);
            GUI.color = new Color(1f, 0.92f, 0.7f, 1f);
            GUI.Label(r, txt, st);
            GUI.color = old;
        }
    }
}
