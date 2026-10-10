using UnityEngine;

namespace COMP602
{
    public class PlayerHealthHUD : MonoBehaviour
    {
        [SerializeField] int fontSize = 48;

        // Distance from the bottom-left corner, in pixels.
        [SerializeField] Vector2 margin = new Vector2(32f, 32f);

        [SerializeField] Color textColor = Color.white;

        [Header("Background")]
        [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);

        // space between the text and the edge of the background, in pixels
        [SerializeField] float padding = 12f;

        [Header("Infected")]
        [SerializeField] Color infectedColor = new Color(0.35f, 0.75f, 0f, 1f);

        const string InfectedText = "INFECTED";

        PlayerHealth health;

        // optional, without it the INFECTED label never shows
        PlayerStatusEffects status;

        GUIStyle style;
        GUIStyle infectedStyle;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            status = GetComponent<PlayerStatusEffects>();
        }

        void OnGUI()
        {
            if (health == null)
                return;

            EnsureStyle();

            bool infected = status != null && status.IsInfected;

            string text = $"{health.Current:0}/{health.Max:0}";

            float lineHeight = fontSize * 1.2f;
            int lines = infected ? 2 : 1;

            // sized for full health, so the background keeps its width as health drops
            float textWidth = style.CalcSize(new GUIContent($"{health.Max:0}/{health.Max:0}")).x;

            if (infected)
                textWidth = Mathf.Max(textWidth, infectedStyle.CalcSize(new GUIContent(InfectedText)).x);

            var background = new Rect(margin.x,
                                      Screen.height - margin.y - lines * lineHeight - padding * 2f,
                                      textWidth + padding * 2f,
                                      lines * lineHeight + padding * 2f);

            Color previous = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(background, Texture2D.whiteTexture);
            GUI.color = previous;

            var healthArea = new Rect(background.x + padding,
                                      background.yMax - padding - lineHeight,
                                      textWidth, lineHeight);

            GUI.Label(healthArea, text, style);

            if (!infected)
                return;

            // same font and alignment as the health, one line above
            var infectedArea = new Rect(healthArea.x, healthArea.y - lineHeight, textWidth, lineHeight);

            GUI.Label(infectedArea, InfectedText, infectedStyle);
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

            infectedStyle = new GUIStyle(style);
            infectedStyle.normal.textColor = infectedColor;
        }
    }
}