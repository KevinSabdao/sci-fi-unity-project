using UnityEngine;

// stamina bar centred near the bottom of the screen
// shows while the sprint key is held and fades out shortly after it is released
public class PlayerStaminaBar : MonoBehaviour
{
    [SerializeField] Vector2 size = new Vector2(260f, 10f);

    // distance from the bottom of the screen, in pixels
    [SerializeField] float bottomOffset = 140f;

    // space between the bar and the edge of its background, in pixels
    [SerializeField] float padding = 4f;

    [Header("Colours")]
    [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] Color fillColor = new Color(1f, 1f, 1f, 0.9f);

    // while out of stamina, until sprint comes back
    [SerializeField] Color exhaustedColor = new Color(0.9f, 0.2f, 0.15f, 0.9f);

    [Header("Fade")]
    // seconds it stays after the sprint key is released
    [SerializeField] float hideDelay = 0.8f;

    [SerializeField] float fadeTime = 0.25f;

    PlayerStamina stamina;
    FirstPersonSprintJump sprint;

    float lastHeldTime = float.NegativeInfinity;
    float alpha;

    void Awake()
    {
        stamina = GetComponent<PlayerStamina>();
        sprint = GetComponent<FirstPersonSprintJump>();
    }

    void Update()
    {
        if (stamina == null || sprint == null || !stamina.UseStamina)
        {
            alpha = 0f;
            return;
        }

        if (sprint.SprintHeld)
            lastHeldTime = Time.unscaledTime;

        bool visible = Time.unscaledTime < lastHeldTime + hideDelay;
        alpha = Mathf.MoveTowards(alpha, visible ? 1f : 0f,
                                  Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f));
    }

    void OnGUI()
    {
        if (alpha <= 0f)
            return;

        var background = new Rect((Screen.width - size.x) * 0.5f - padding,
                                  Screen.height - bottomOffset - size.y - padding,
                                  size.x + padding * 2f,
                                  size.y + padding * 2f);

        var bar = new Rect(background.x + padding, background.y + padding, size.x, size.y);

        Color previous = GUI.color;

        Draw(background, backgroundColor);

        Color fill = stamina.IsExhausted ? exhaustedColor : fillColor;
        Draw(new Rect(bar.x, bar.y, bar.width * stamina.Fraction, bar.height), fill);

        // where sprint comes back, only while exhausted
        if (stamina.IsExhausted)
        {
            float x = bar.x + bar.width * stamina.RecoverFraction;
            Draw(new Rect(x - 1f, bar.y - 2f, 2f, bar.height + 4f), fillColor);
        }

        GUI.color = previous;
    }

    void Draw(Rect rect, Color color)
    {
        color.a *= alpha;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
}
