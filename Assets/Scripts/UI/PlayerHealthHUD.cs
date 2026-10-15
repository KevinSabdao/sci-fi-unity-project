using System.Collections.Generic;
using UnityEngine;

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

    [Header("Messages")]
    // seconds a message stays, it fades out over the last half second
    [SerializeField] float messageDuration = 3f;

    // same cyan as the vaccine
    [SerializeField] Color curedColor = new Color(0.2f, 0.8f, 1f, 1f);

    const string InfectionText = "INFECTION";

    // widest the infection line gets, sizes the background
    const string InfectionWidestText = "INFECTION - 00s";

    const string CuredText = "INFECTION CURED";
    const float MessageFadeTime = 0.5f;

    // one line of text above the health
    struct Line
    {
        public string text;

        // widest the text gets, keeps the background from changing width every second
        public string widestText;

        public Color color;
    }

    class Message
    {
        public string text;
        public Color color;
        public float endTime;
    }

    PlayerHealth health;

    // optional, without it no effect lines or messages show
    PlayerStatusEffects status;

    GUIStyle style;

    // white text, each line tints it with its own color
    GUIStyle lineStyle;

    // rebuilt every frame, effects first, then messages
    readonly List<Line> lines = new List<Line>();
    readonly List<Message> messages = new List<Message>();

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        status = GetComponent<PlayerStatusEffects>();
    }

    void OnEnable()
    {
        if (status != null)
            status.Vaccinated += ShowCuredMessage;
    }

    void OnDisable()
    {
        if (status != null)
            status.Vaccinated -= ShowCuredMessage;
    }

    // short message on its own line, the same text again restarts its timer
    public void ShowMessage(string text, Color color)
    {
        float endTime = Time.time + messageDuration;

        foreach (Message m in messages)
        {
            if (m.text != text)
                continue;

            m.color = color;
            m.endTime = endTime;
            return;
        }

        messages.Add(new Message { text = text, color = color, endTime = endTime });
    }

    void ShowCuredMessage() => ShowMessage(CuredText, curedColor);

    void OnGUI()
    {
        if (health == null)
            return;

        EnsureStyle();
        BuildLines();

        string text = $"{health.Current:0}/{health.Max:0}";

        float lineHeight = fontSize * 1.2f;
        int lineCount = 1 + lines.Count;

        // sized for full health, so the background keeps its width as health drops
        float textWidth = style.CalcSize(new GUIContent($"{health.BaseMax:0}/{health.BaseMax:0}")).x;

        foreach (Line line in lines)
            textWidth = Mathf.Max(textWidth, lineStyle.CalcSize(new GUIContent(line.widestText)).x);

        var background = new Rect(margin.x,
                                  Screen.height - margin.y - lineCount * lineHeight - padding * 2f,
                                  textWidth + padding * 2f,
                                  lineCount * lineHeight + padding * 2f);

        Color previous = GUI.color;
        GUI.color = backgroundColor;
        GUI.DrawTexture(background, Texture2D.whiteTexture);
        GUI.color = previous;

        var healthArea = new Rect(background.x + padding,
                                  background.yMax - padding - lineHeight,
                                  textWidth, lineHeight);

        GUI.Label(healthArea, text, style);

        // same font and alignment as the health, stacked upwards
        for (int i = 0; i < lines.Count; i++)
        {
            var area = new Rect(healthArea.x, healthArea.y - (i + 1) * lineHeight, textWidth, lineHeight);

            GUI.color = lines[i].color;
            GUI.Label(area, lines[i].text, lineStyle);
        }

        GUI.color = previous;
    }

    void BuildLines()
    {
        lines.Clear();

        if (status != null && status.IsInfected)
        {
            // counts down to the next max health loss
            lines.Add(new Line
            {
                text = $"{InfectionText} - {Mathf.CeilToInt(status.SecondsToNextLoss)}s",
                widestText = InfectionWidestText,
                color = infectedColor,
            });
        }

        messages.RemoveAll(m => Time.time >= m.endTime);

        foreach (Message m in messages)
        {
            Color color = m.color;
            color.a *= Mathf.Clamp01((m.endTime - Time.time) / MessageFadeTime);

            lines.Add(new Line { text = m.text, widestText = m.text, color = color });
        }
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

        lineStyle = new GUIStyle(style);
        lineStyle.normal.textColor = Color.white;
    }
}
