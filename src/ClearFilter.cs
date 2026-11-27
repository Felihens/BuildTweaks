using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuildTweaks
{
    // A small ✕ inside the build menu's filter box that wipes what's written (the user, 2026-11-27: "I missed a
    // clear button to click and wipe what's written"). The filter box isn't the game's (another mod adds it), so
    // any text box in the build menu whose placeholder says filter or search gets one.
    internal static class ClearFilter
    {
        private const string Mark = "BT_ClearFilter";
        private static float _next;

        internal static void Update()
        {
            if (Time.time < _next || Hud.instance == null) return;
            _next = Time.time + 0.5f;
            if (!Hud.IsPieceSelectionVisible()) return;
            foreach (TMP_InputField f in Hud.instance.GetComponentsInChildren<TMP_InputField>(false))
                if (Wants(f.placeholder) && f.transform.Find(Mark) == null) Add(f.transform, f.textComponent, delegate { f.text = ""; });
            foreach (InputField f in Hud.instance.GetComponentsInChildren<InputField>(false))
                if (Wants(f.placeholder) && f.transform.Find(Mark) == null) Add(f.transform, null, delegate { f.text = ""; });
        }

        private static bool Wants(Graphic placeholder)
        {
            if (placeholder == null) return false;
            string t = placeholder is TMP_Text ? ((TMP_Text)placeholder).text : placeholder is Text ? ((Text)placeholder).text : "";
            t = (t ?? "").ToLowerInvariant();
            return t.Contains("filter") || t.Contains("search");
        }

        private static void Add(Transform field, TMP_Text like, UnityEngine.Events.UnityAction clear)
        {
            GameObject go = new GameObject(Mark, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(field, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-4f, 0f);
            float h = Mathf.Clamp(((RectTransform)field).rect.height - 8f, 14f, 26f);
            rt.sizeDelta = new Vector2(h, h);
            Image img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
            Button b = go.GetComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.8f, 0.4f, 1f);
            cb.pressedColor = new Color(1f, 0.7f, 0.3f, 1f);
            b.colors = cb;
            b.onClick.AddListener(clear);

            GameObject tx = new GameObject("x", typeof(RectTransform), typeof(TextMeshProUGUI));
            tx.transform.SetParent(go.transform, false);
            RectTransform trt = (RectTransform)tx.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            TextMeshProUGUI t = tx.GetComponent<TextMeshProUGUI>();
            if (like != null) t.font = like.font;
            t.text = "x";
            t.fontSize = h * 0.75f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.95f, 0.72f, 0.3f);
            t.raycastTarget = false;
            Plugin.Log.LogInfo("[filter] clear button added to the build menu's " + field.name);
        }
    }
}
