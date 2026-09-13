using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Debug only. Draws a live readout of what the enemy is doing, so you can
    // tell an animation that is actually moving the body from one that is
    // playing on the spot. Delete or disable before submitting.
    public class EnemyDebugReadout : MonoBehaviour
    {
        [Header("Display")]
        // pixels from the top left, clears the other debug panels
        [SerializeField] Vector2 screenPosition = new Vector2(10f, 120f);

        [SerializeField] int fontSize = 24;

        [SerializeField] float boxWidth = 620f;

        // below this the body counts as standing still
        [SerializeField] float movingThreshold = 0.05f;

        NavMeshAgent agent;
        Animator animator;
        EnemyAttackState attackState;
        Transform player;

        // measured from the transform, a stopped agent can still be sliding
        Vector3 lastPosition;
        float measuredSpeed;

        GUIStyle style;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            attackState = GetComponent<EnemyAttackState>();

            lastPosition = transform.position;
        }

        void Start()
        {
            // OnGUI runs several times per frame, too often to search in
            GameObject found = GameObject.FindGameObjectWithTag("Player");

            if (found != null)
                player = found.transform;
        }

        void Update()
        {
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;

            measuredSpeed = moved.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
        }

        void OnGUI()
        {
            if (style == null || style.fontSize != fontSize)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = fontSize;
                style.normal.textColor = Color.white;
                style.richText = true;
            }

            bool moving = measuredSpeed > movingThreshold;

            // the parameter the blend tree reads to pick a clip
            float animSpeed = animator != null ? animator.GetFloat("Speed") : -1f;

            // which clip is actually on screen right now
            AnimatorStateInfo info = animator != null
                ? animator.GetCurrentAnimatorStateInfo(0)
                : default;

            string clipName = "none";

            if (animator != null)
            {
                AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);

                if (clips.Length > 0)
                    clipName = clips[0].clip.name;
            }

            // same measurement EnemyAI tests against loseSightRange
            float distance = player != null
                ? Vector3.Distance(transform.position, player.position)
                : -1f;

            // red when the body is still under a locomotion clip
            string bodyColor = moving ? "#7CFF7C" : "#FF7C7C";

            string text =
                $"BODY: <color={bodyColor}>{(moving ? "MOVING" : "STILL")}</color>" +
                $"  ({measuredSpeed:F2} m/s)\n" +
                $"Speed param: {animSpeed:F2}\n" +
                $"Clip: {clipName}  ({info.normalizedTime % 1f:F2})\n" +
                $"Agent stopped: {(agent != null && agent.isStopped ? "yes" : "no")}\n" +
                $"Agent velocity: {(agent != null ? agent.velocity.magnitude : 0f):F2}\n" +
                $"Attacking: {(attackState != null && attackState.IsAttacking ? "yes" : "no")}\n" +
                $"Distance: {distance:F1} m";

            float lineHeight = fontSize * 1.35f;
            float boxHeight = lineHeight * 7f + 20f;

            Rect box = new Rect(screenPosition.x, screenPosition.y, boxWidth, boxHeight);

            // dark backing keeps the text readable over the scene
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, box.height - 16f),
                      text, style);
        }
    }
}
