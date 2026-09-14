using UnityEngine;

namespace COMP602
{
    public class PlayerHealthHUD : MonoBehaviour
    {
        [SerializeField] int fontSize = 48;

        // Distance from the bottom-left corner, in pixels.
        [SerializeField] Vector2 margin = new Vector2(32f, 32f);

        [SerializeField] Color textColor = Color.white;

        PlayerHealth health;
        GUIStyle style;

        void Awake() => health = GetComponent<PlayerHealth>();

        void OnGUI()
        {
            if (health == null)
                return;

            EnsureStyle();

            string text = $"{health.Current:0}/{health.Max:0}";

            float lineHeight = fontSize * 1.2f;
            var area = new Rect(margin.x, Screen.height - lineHeight - margin.y,
                                fontSize * 6f, lineHeight);

            GUI.Label(area, text, style);
        }

        void EnsureStyle()
        {
            if (style != null && style.fontSize == fontSize)
                return;

            style = new GUIStyle
            {
                fontSize = fontSize,
                alignment = TextAnchor.LowerLeft,
            };
            style.normal.textColor = textColor;
        }
    }
}