using UnityEngine;

// "Press X to mend" below the centre of the screen while a deep wound is open
// a ring of dots fills up while the key is held. letting go keeps the progress for a moment
[RequireComponent(typeof(PlayerStatusEffects))]
public class PlayerMendPrompt : MonoBehaviour
{
    [SerializeField] int fontSize = 34;

    // distance below the screen centre, in pixels
    [SerializeField] float offsetBelowCentre = 110f;

    // radius of the ring of dots, in pixels
    [SerializeField] float ringRadius = 32f;

    [SerializeField] int ringDots = 32;

    // size of each dot, in pixels
    [SerializeField] float dotSize = 6f;

    [Header("Colours")]
    [SerializeField] Color textColor = Color.white;
    [SerializeField] Color filledColor = Color.white;
    [SerializeField] Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    // drawn behind the text so it reads on bright backgrounds
    [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.7f);

    PlayerStatusEffects status;
    GUIStyle style;

    void Awake()
    {
        status = GetComponent<PlayerStatusEffects>();
    }

    void OnGUI()
    {
        if (!status.IsDeepWounded || FirstPersonLook.UiHasCursor)
            return;

        EnsureStyle();

        Color previous = GUI.color;
        var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f + offsetBelowCentre);

        // clockwise from the top. the dots up to the progress are filled
        int filled = Mathf.FloorToInt(status.MendFraction * ringDots);

        for (int i = 0; i < ringDots; i++)
        {
            float angle = i / (float)ringDots * Mathf.PI * 2f;
            Vector2 dot = centre + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * ringRadius;

            GUI.color = i < filled ? filledColor : emptyColor;
            GUI.DrawTexture(new Rect(dot.x - dotSize * 0.5f, dot.y - dotSize * 0.5f, dotSize, dotSize),
                            Texture2D.whiteTexture);
        }

        string key = status.MendKey.ToString();

        // the key in the middle of the ring, the instruction under it
        DrawLabel(new Rect(centre.x - ringRadius, centre.y - ringRadius, ringRadius * 2f, ringRadius * 2f), key);
        DrawLabel(new Rect(centre.x - 200f, centre.y + ringRadius + 6f, 400f, fontSize * 1.3f), $"Press {key} to mend");

        GUI.color = previous;
    }

    void DrawLabel(Rect area, string text)
    {
        GUI.color = shadowColor;
        GUI.Label(new Rect(area.x + 2f, area.y + 2f, area.width, area.height), text, style);

        GUI.color = textColor;
        GUI.Label(area, text, style);
    }

    void EnsureStyle()
    {
        if (style != null && style.fontSize == fontSize)
            return;

        style = new GUIStyle
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
        };
        style.normal.textColor = Color.white;
    }
}
