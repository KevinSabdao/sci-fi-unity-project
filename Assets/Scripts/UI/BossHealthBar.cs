using UnityEngine;

// boss name and health bar at the top of the screen, in percent
// shows once the fight starts and fades out a few seconds after the boss dies
[RequireComponent(typeof(EnemyHealth))]
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] string bossName = "The Evil Resident";

    [SerializeField] int fontSize = 32;

    // the percentage beside the bar
    [SerializeField] int percentFontSize = 28;

    // distance from the top of the screen, in pixels
    [SerializeField] float topMargin = 24f;

    [SerializeField] Vector2 barSize = new Vector2(720f, 14f);

    // space between the bar and the edge of its background, in pixels
    [SerializeField] float padding = 4f;

    // space between the bar and the percentage, in pixels
    [SerializeField] float percentGap = 10f;

    [Header("Colours")]
    [SerializeField] Color textColor = Color.white;
    [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] Color fillColor = new Color(0.7f, 0.08f, 0.08f, 1f);

    // the part just lost, shrinks after a moment so big hits read clearly
    [SerializeField] Color recentDamageColor = new Color(1f, 0.85f, 0.6f, 0.9f);

    // the part just lost after a critical hit. the percentage flashes it too
    [SerializeField] Color criticalColor = new Color(1f, 0.85f, 0.1f, 1f);

    [Header("Timing")]
    [SerializeField] float fadeTime = 0.5f;

    // seconds the bar stays after the boss dies
    [SerializeField] float hideAfterDeath = 3f;

    // seconds before the recent damage starts to shrink
    [SerializeField] float recentDamageDelay = 0.5f;

    // share of the bar it shrinks per second
    [SerializeField] float recentDamageSpeed = 0.5f;

    // seconds the percentage stays in the critical colour after a critical hit
    [SerializeField] float criticalFlashTime = 0.4f;

    EnemyHealth health;

    // optional, without it the bar shows once the boss is first hurt
    BossAI ai;

    GUIStyle nameStyle;
    GUIStyle percentStyle;

    float alpha;
    float recentFraction = 1f;
    float lastHitTime;
    float lastCriticalTime = float.NegativeInfinity;
    float deathTime = float.NegativeInfinity;

    // a weak point hit is inside the recent damage part, colours it until it has shrunk away
    bool recentHasCritical;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        ai = GetComponent<BossAI>();
    }

    void OnEnable()
    {
        health.OnDamaged.AddListener(HandleDamaged);
        health.OnDied.AddListener(HandleDied);
    }

    void OnDisable()
    {
        health.OnDamaged.RemoveListener(HandleDamaged);
        health.OnDied.RemoveListener(HandleDied);
    }

    void HandleDamaged()
    {
        lastHitTime = Time.time;

        if (health.LastHitCritical)
        {
            lastCriticalTime = Time.time;
            recentHasCritical = true;
        }
    }

    void HandleDied() => deathTime = Time.time;

    float Fraction => health.Max > 0f ? health.Current / health.Max : 0f;

    bool Visible
    {
        get
        {
            if (!health.IsAlive)
                return Time.time < deathTime + hideAfterDeath;

            return ai != null ? ai.IsFighting : health.Current < health.Max;
        }
    }

    void Update()
    {
        alpha = Mathf.MoveTowards(alpha, Visible ? 1f : 0f, Time.deltaTime / Mathf.Max(fadeTime, 0.01f));

        // the recent damage part never sits below the real health
        if (recentFraction < Fraction)
            recentFraction = Fraction;
        else if (Time.time > lastHitTime + recentDamageDelay)
            recentFraction = Mathf.MoveTowards(recentFraction, Fraction, recentDamageSpeed * Time.deltaTime);

        if (recentFraction <= Fraction)
            recentHasCritical = false;
    }

    void OnGUI()
    {
        if (alpha <= 0f)
            return;

        EnsureStyles();

        float lineHeight = fontSize * 1.2f;

        var bar = new Rect((Screen.width - barSize.x) * 0.5f, topMargin + lineHeight + padding,
                           barSize.x, barSize.y);
        var background = new Rect(bar.x - padding, bar.y - padding,
                                  bar.width + padding * 2f, bar.height + padding * 2f);

        Color previous = GUI.color;

        Draw(background, backgroundColor);
        Draw(new Rect(bar.x, bar.y, bar.width * recentFraction, bar.height),
             recentHasCritical ? criticalColor : recentDamageColor);
        Draw(new Rect(bar.x, bar.y, bar.width * Fraction, bar.height), fillColor);

        // name centred above the bar
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(bar.x, topMargin, bar.width, lineHeight), bossName, nameStyle);

        // rounded up so it reads 1% until the last hit, not 0% while alive
        int percent = Mathf.CeilToInt(Fraction * 100f);

        // right of the bar, level with it
        Color percentColor = Time.time < lastCriticalTime + criticalFlashTime ? criticalColor : textColor;
        percentStyle.normal.textColor = percentColor;

        var percentArea = new Rect(background.xMax + percentGap, background.y,
                                   percentFontSize * 4f, background.height);
        GUI.Label(percentArea, $"{percent}%", percentStyle);

        GUI.color = previous;
    }

    void Draw(Rect rect, Color color)
    {
        color.a *= alpha;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }

    void EnsureStyles()
    {
        if (nameStyle != null && nameStyle.fontSize == fontSize && percentStyle.fontSize == percentFontSize)
            return;

        nameStyle = new GUIStyle
        {
            fontSize = fontSize,
            alignment = TextAnchor.LowerCenter,
        };
        nameStyle.normal.textColor = textColor;

        percentStyle = new GUIStyle
        {
            fontSize = percentFontSize,
            alignment = TextAnchor.MiddleLeft,
        };
        percentStyle.normal.textColor = textColor;
    }
}
